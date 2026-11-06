import { ApiError } from '../api/client'

export type SaveStatus = 'saving' | 'saved' | 'offline'

export interface SaveItem {
  questionNumber: number
  value: string
}

/** What the server accepts in one answer (`ResponseItem` is `[StringLength(20_000)]`); inputs stop here so a save is never refused for length. */
export const MAX_ANSWER_LENGTH = 20_000

/** The server takes at most this many answers in one request. */
const MAX_BATCH = 100

export interface AutosaveOptions {
  /** Wait this long after the last change before saving, so typing is sent in one request. */
  delayMs: number
  /** Never wait longer than this for the first unsent change, so continuous typing is still saved. Defaults to 3 s. */
  maxWaitMs?: number
  /** Retry this often while the server cannot be reached. */
  retryMs: number
  onStatus: (status: SaveStatus) => void
  /** The server will not take more answers (time up, signed in elsewhere). A pause does not close the queue. */
  onClosed: (reason: string) => void
  /** The server refused a save because the test is paused: the answers are kept; read the clock so the screen follows. */
  onPaused?: () => void
  /** Pending answers are copied here (localStorage) until the server has them, so a reload or crash does not lose them. */
  storageKey?: string
  /** Sends answers so that they get out even while the page is closing (fetch with keepalive). */
  saveOnPageHide?: (items: SaveItem[]) => void
}

/** Statuses after which saving again would never succeed. A 409 with code 'paused' or 'busy' is not one of them. */
const CLOSING_STATUSES = new Set([401, 403, 409])

/** Answers an earlier page left in the mirror; empty when there are none or the copy is damaged. */
export function loadStoredAnswers(key: string): SaveItem[] {
  try {
    const parsed: unknown = JSON.parse(localStorage.getItem(key) ?? '[]')
    return Array.isArray(parsed)
      ? parsed.filter((i): i is SaveItem => typeof i?.questionNumber === 'number' && typeof i?.value === 'string')
      : []
  } catch {
    return []
  }
}

export function clearStoredAnswers(key: string): void {
  try {
    localStorage.removeItem(key)
  } catch {
    // No storage (private window, blocked site data): saving to the server is still the main path.
  }
}

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
  /** When the oldest unsent change was made; the debounce never pushes a save past this plus maxWaitMs. */
  private waitingSince: number | undefined

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
    this.mirror()
    this.waitingSince ??= Date.now()
    const waited = Date.now() - this.waitingSince
    this.schedule(Math.min(this.options.delayMs, Math.max(0, (this.options.maxWaitMs ?? 3_000) - waited)))
  }

  /** Sends whatever is pending once more, then stops. For the moment the screen goes away or the section ends. */
  flushAndDispose(): void {
    if (!this.isClosed && !this.isSending) {
      for (const batch of this.batches()) {
        this.save(batch).then(() => this.acknowledge(batch), () => {})
      }
    }

    this.dispose()
  }

  /** The page is closing: hand every pending answer to the keep-alive sender. They stay mirrored in case it fails. */
  sendOnPageHide(): void {
    if (this.pending.size > 0) {
      this.options.saveOnPageHide?.([...this.pending].map(([questionNumber, value]) => ({ questionNumber, value })))
    }
  }

  dispose(): void {
    this.isClosed = true
    clearTimeout(this.timer)
  }

  private batches(): SaveItem[][] {
    const all = [...this.pending].map(([questionNumber, value]) => ({ questionNumber, value }))
    const batches: SaveItem[][] = []
    for (let from = 0; from < all.length; from += MAX_BATCH) {
      batches.push(all.slice(from, from + MAX_BATCH))
    }
    return batches
  }

  /** Forget only what was sent unchanged; newer edits stay pending. */
  private acknowledge(batch: SaveItem[]): void {
    batch.filter((item) => this.pending.get(item.questionNumber) === item.value).forEach((item) => this.pending.delete(item.questionNumber))
    this.mirror()
  }

  private mirror(): void {
    const key = this.options.storageKey
    if (!key) {
      return
    }

    try {
      if (this.pending.size === 0) {
        localStorage.removeItem(key)
      } else {
        localStorage.setItem(key, JSON.stringify([...this.pending].map(([questionNumber, value]) => ({ questionNumber, value }))))
      }
    } catch {
      // No storage: the server copy is the only one.
    }
  }

  private schedule(delayMs: number): void {
    clearTimeout(this.timer)
    this.timer = setTimeout(() => void this.flush(), delayMs)
  }

  private async flush(): Promise<void> {
    if (this.isClosed || this.isSending || this.pending.size === 0) {
      return
    }

    const batch = this.batches()[0]
    this.waitingSince = undefined
    this.isSending = true
    this.options.onStatus('saving')
    try {
      await this.save(batch)
      this.acknowledge(batch)
      this.options.onStatus(this.pending.size === 0 ? 'saved' : 'saving')
    } catch (error) {
      this.handleFailure(error)
      return
    } finally {
      this.isSending = false
    }

    if (this.pending.size > 0) {
      this.waitingSince = Date.now()
      this.schedule(this.options.delayMs)
    }
  }

  private handleFailure(error: unknown): void {
    if (error instanceof ApiError && error.status === 409 && error.code === 'paused') {
      // Answers typed just before the pause: keep them all and send them after Resume.
      this.options.onStatus('saving')
      this.options.onPaused?.()
      this.schedule(this.options.retryMs)
      return
    }

    if (error instanceof ApiError && error.status === 409 && error.code === 'busy') {
      // The server was held up by a lock (a status change in progress): nothing was lost, try again shortly.
      this.options.onStatus('saving')
      this.schedule(this.options.retryMs)
      return
    }

    if (error instanceof ApiError && CLOSING_STATUSES.has(error.status)) {
      this.dispose()
      this.options.onClosed(error.message)
      return
    }

    this.options.onStatus('offline')
    this.schedule(this.options.retryMs)
  }
}
