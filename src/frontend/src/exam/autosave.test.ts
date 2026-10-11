import { ApiError } from '../api/client'
import { AutosaveQueue, type SaveStatus } from './autosave'

describe('AutosaveQueue', () => {
  beforeEach(() => vi.useFakeTimers())
  afterEach(() => vi.useRealTimers())

  const setup = (save: (items: { questionNumber: number; value: string }[]) => Promise<void>) => {
    const statuses: SaveStatus[] = []
    const onClosed = vi.fn()
    const queue = new AutosaveQueue(save, { delayMs: 800, retryMs: 3000, onStatus: (s) => statuses.push(s), onClosed })
    return { queue, statuses, onClosed }
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
})
