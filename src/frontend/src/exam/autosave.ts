import { ApiError } from '../api/client'

export type SaveStatus = 'saving' | 'saved' | 'offline'

export interface SaveItem {
  questionNumber: number
  value: string
}

export interface AutosaveOptions {
  /** Wait this long after the last change before saving, so typing is sent in one request. */
  delayMs: number
  /** Retry this often while the server cannot be reached. */
  retryMs: number
  onStatus: (status: SaveStatus) => void
  /** The server will not take more answers (time up, paused, signed in elsewhere). */
  onClosed: (reason: string) => void
}

/** Statuses after which saving again would never succeed. */
const CLOSING_STATUSES = new Set([401, 403, 409])

/**
 * Sends changed answers to the server. Only the latest value of each question is kept; answers are never
 * dropped on a network failure, and an answer changed while a save is in flight is sent again afterwards.
 */
export class AutosaveQueue {
  private readonly save: (items: SaveItem[]) => Promise<void>
  private readonly options: AutosaveOptions
  private readonly pending = new Map<number, string>()
  private timer: ReturnType<typeof setTimeout> | undefined
  private isSending = false
  private isClosed = false

  constructor(save: (items: SaveItem[]) => Promise<void>, options: AutosaveOptions) {
    this.save = save
    this.options = options
  }

  get hasPending(): boolean {
    return this.pending.size > 0
  }

  push(questionNumber: number, value: string): void {
    if (this.isClosed) {
      return
    }

    this.pending.set(questionNumber, value)
    this.schedule(this.options.delayMs)
  }

  dispose(): void {
    this.isClosed = true
    clearTimeout(this.timer)
  }

  private schedule(delayMs: number): void {
    clearTimeout(this.timer)
    this.timer = setTimeout(() => void this.flush(), delayMs)
  }

  private async flush(): Promise<void> {
    if (this.isClosed || this.isSending || this.pending.size === 0) {
      return
    }

    const batch = [...this.pending].map(([questionNumber, value]) => ({ questionNumber, value }))
    this.isSending = true
    this.options.onStatus('saving')
    try {
      await this.save(batch)
      // Forget only what was sent unchanged; newer edits stay pending.
      batch.filter((item) => this.pending.get(item.questionNumber) === item.value).forEach((item) => this.pending.delete(item.questionNumber))
      this.options.onStatus(this.pending.size === 0 ? 'saved' : 'saving')
    } catch (error) {
      this.handleFailure(error)
      return
    } finally {
      this.isSending = false
    }

    if (this.pending.size > 0) {
      this.schedule(this.options.delayMs)
    }
  }

  private handleFailure(error: unknown): void {
    if (error instanceof ApiError && CLOSING_STATUSES.has(error.status)) {
      this.dispose()
      this.options.onClosed(error.message)
      return
    }

    this.options.onStatus('offline')
    this.schedule(this.options.retryMs)
  }
}
