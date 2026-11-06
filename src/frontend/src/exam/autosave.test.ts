import { ApiError } from '../api/client'
import { AutosaveQueue, loadStoredAnswers, MAX_ANSWER_LENGTH, type AutosaveOptions, type SaveStatus } from './autosave'

describe('AutosaveQueue', () => {
  beforeEach(() => vi.useFakeTimers())
  afterEach(() => vi.useRealTimers())

  beforeEach(() => localStorage.clear())

  const setup = (save: (items: { questionNumber: number; value: string }[]) => Promise<void>, extra: Partial<AutosaveOptions> = {}) => {
    const statuses: SaveStatus[] = []
    const onClosed = vi.fn()
    const onPaused = vi.fn()
    const queue = new AutosaveQueue(save, { delayMs: 800, retryMs: 3000, maxWaitMs: 3000, onStatus: (s) => statuses.push(s), onClosed, onPaused, ...extra })
    return { queue, statuses, onClosed, onPaused }
  }

  it('sends the latest value of each changed answer once the candidate pauses', async () => {
    const save = vi.fn().mockResolvedValue(undefined)
    const { queue, statuses } = setup(save)

    queue.push(25, 'f')
    queue.push(25, 'fo')
    queue.push(25, 'for')
    queue.push(1, 'B')
    expect(save).not.toHaveBeenCalled()
    await vi.advanceTimersByTimeAsync(800)

    expect(save).toHaveBeenCalledTimes(1)
    expect(save).toHaveBeenCalledWith([
      { questionNumber: 25, value: 'for' },
      { questionNumber: 1, value: 'B' },
    ])
    expect(statuses.at(-1)).toBe('saved')
  })

  it('keeps unsaved answers and retries when the network fails', async () => {
    const save = vi.fn().mockRejectedValueOnce(new TypeError('Failed to fetch')).mockResolvedValue(undefined)
    const { queue, statuses } = setup(save)

    queue.push(1, 'B')
    await vi.advanceTimersByTimeAsync(800)
    expect(statuses.at(-1)).toBe('offline')

    await vi.advanceTimersByTimeAsync(3000)

    expect(save).toHaveBeenCalledTimes(2)
    expect(save).toHaveBeenLastCalledWith([{ questionNumber: 1, value: 'B' }])
    expect(statuses.at(-1)).toBe('saved')
  })

  it('does not lose an answer changed while a save is on its way', async () => {
    let finish: () => void = () => {}
    const save = vi.fn().mockImplementationOnce(() => new Promise<void>((resolve) => (finish = resolve))).mockResolvedValue(undefined)
    const { queue } = setup(save)

    queue.push(1, 'B')
    await vi.advanceTimersByTimeAsync(800)
    queue.push(1, 'C')
    finish()
    await vi.advanceTimersByTimeAsync(800)

    expect(save).toHaveBeenLastCalledWith([{ questionNumber: 1, value: 'C' }])
  })

  it('stops and reports when the server says the section has finished', async () => {
    const save = vi.fn().mockRejectedValue(new ApiError(409, 'This part of the test has finished.'))
    const { queue, onClosed } = setup(save)

    queue.push(1, 'B')
    await vi.advanceTimersByTimeAsync(800)
    await vi.advanceTimersByTimeAsync(10_000)

    expect(onClosed).toHaveBeenCalledWith('This part of the test has finished.')
    expect(save).toHaveBeenCalledTimes(1)
  })

  describe('while the test is paused (A1)', () => {
    const paused = () => new ApiError(409, 'Your test is paused.', [], 'paused')

    it('keeps every pending answer, shows saving, asks for the clock and sends them after Resume', async () => {
      const save = vi.fn().mockRejectedValueOnce(paused()).mockRejectedValueOnce(paused()).mockResolvedValue(undefined)
      const { queue, statuses, onClosed, onPaused } = setup(save)

      queue.push(1, 'B')
      await vi.advanceTimersByTimeAsync(800)
      queue.push(2, 'C')
      await vi.advanceTimersByTimeAsync(800)

      expect(onClosed).not.toHaveBeenCalled()
      expect(onPaused).toHaveBeenCalled()
      expect(statuses.at(-1)).toBe('saving')

      await vi.advanceTimersByTimeAsync(3000)

      expect(save).toHaveBeenCalledTimes(3)
      expect(save).toHaveBeenLastCalledWith([
        { questionNumber: 1, value: 'B' },
        { questionNumber: 2, value: 'C' },
      ])
      expect(statuses.at(-1)).toBe('saved')
    })

    it('keeps the answers and tries again when the server was busy with a lock (code busy)', async () => {
      const busy = new ApiError(409, 'The session is being changed; try again.', [], 'busy')
      const save = vi.fn().mockRejectedValueOnce(busy).mockResolvedValue(undefined)
      const { queue, statuses, onClosed } = setup(save)

      queue.push(1, 'B')
      await vi.advanceTimersByTimeAsync(800)
      expect(onClosed).not.toHaveBeenCalled()

      await vi.advanceTimersByTimeAsync(3000)

      expect(save).toHaveBeenCalledTimes(2)
      expect(save).toHaveBeenLastCalledWith([{ questionNumber: 1, value: 'B' }])
      expect(statuses.at(-1)).toBe('saved')
    })

    it('still closes when the server says the section has finished', async () => {
      const save = vi.fn().mockRejectedValue(new ApiError(409, 'This part of the test has finished.', [], 'finished'))
      const { queue, onClosed } = setup(save)

      queue.push(1, 'B')
      await vi.advanceTimersByTimeAsync(800)

      expect(onClosed).toHaveBeenCalledTimes(1)
    })

    it.each([401, 403])('still closes on %i', async (status) => {
      const save = vi.fn().mockRejectedValue(new ApiError(status, 'No.'))
      const { queue, onClosed } = setup(save)

      queue.push(1, 'B')
      await vi.advanceTimersByTimeAsync(800)

      expect(onClosed).toHaveBeenCalledTimes(1)
    })
  })

  describe('answers are not lost (A3)', () => {
    it('saves during continuous typing at least every maxWaitMs', async () => {
      const save = vi.fn().mockResolvedValue(undefined)
      const { queue } = setup(save)

      for (let i = 0; i < 20; i++) {
        queue.push(1, 'x'.repeat(i + 1))
        await vi.advanceTimersByTimeAsync(500)
      }

      expect(save.mock.calls.length).toBeGreaterThanOrEqual(3)
    })

    it('sends what is pending once when told to flush before closing', async () => {
      const save = vi.fn().mockResolvedValue(undefined)
      const { queue } = setup(save)

      queue.push(1, 'B')
      queue.flushAndDispose()
      await vi.advanceTimersByTimeAsync(5000)

      expect(save).toHaveBeenCalledTimes(1)
      expect(save).toHaveBeenCalledWith([{ questionNumber: 1, value: 'B' }])
    })

    it('mirrors pending answers in localStorage until the server has them', async () => {
      const save = vi.fn().mockResolvedValue(undefined)
      const { queue } = setup(save, { storageKey: 'pending:a' })

      queue.push(1, 'B')
      expect(loadStoredAnswers('pending:a')).toEqual([{ questionNumber: 1, value: 'B' }])
      await vi.advanceTimersByTimeAsync(800)

      expect(loadStoredAnswers('pending:a')).toEqual([])
      expect(localStorage.getItem('pending:a')).toBeNull()
    })

    it('keeps the mirror when the save fails, so a reload can send it', async () => {
      const save = vi.fn().mockRejectedValue(new TypeError('Failed to fetch'))
      const { queue } = setup(save, { storageKey: 'pending:a' })

      queue.push(1, 'B')
      await vi.advanceTimersByTimeAsync(800)

      expect(loadStoredAnswers('pending:a')).toEqual([{ questionNumber: 1, value: 'B' }])
    })

    it('sends the pending answers with the keep-alive sender when the page is hidden', () => {
      const save = vi.fn().mockResolvedValue(undefined)
      const keepalive = vi.fn()
      const { queue } = setup(save, { saveOnPageHide: keepalive })

      queue.push(1, 'B')
      queue.sendOnPageHide()

      expect(keepalive).toHaveBeenCalledWith([{ questionNumber: 1, value: 'B' }])
    })

    it('ignores a damaged mirror', () => {
      localStorage.setItem('pending:a', '{nope')

      expect(loadStoredAnswers('pending:a')).toEqual([])
    })

    it('sends at most 100 answers in one request', async () => {
      const save = vi.fn().mockResolvedValue(undefined)
      const { queue } = setup(save)

      for (let n = 1; n <= 120; n++) {
        queue.push(n, 'a')
      }
      await vi.advanceTimersByTimeAsync(800)
      await vi.advanceTimersByTimeAsync(800)

      expect(save.mock.calls.map(([items]) => items.length)).toEqual([100, 20])
    })
  })

  it('allows as many characters as the server accepts', () => {
    expect(MAX_ANSWER_LENGTH).toBe(20_000)
  })
})
