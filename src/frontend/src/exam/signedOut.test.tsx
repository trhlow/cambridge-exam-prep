import { act, renderHook, waitFor } from '@testing-library/react'
import { ApiError, candidateApi } from '../api/client'
import type { AttemptState } from '../api/types'
import { useAttempt } from './useAttempt'

vi.mock('../api/live', () => ({ useLive: () => false }))

const running = { status: 'InProgress', remainingSeconds: 3_000, audioPositionMs: null, answers: {}, selections: {}, messages: [] } as unknown as AttemptState

/**
 * VE-43 (mock exam 26/10): after the candidate logged in on another computer, the old one kept showing the test with
 * a frozen clock and "Not connected", because a 401 on the heartbeat was taken for a network failure.
 */
describe('useAttempt when the server no longer knows this computer', () => {
  afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
  })

  it('reports the computer signed out when the heartbeat answers 401', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    vi.spyOn(candidateApi, 'startAttempt').mockResolvedValue(running)
    vi.spyOn(candidateApi, 'heartbeat').mockRejectedValue(new ApiError(401, 'Not signed in.'))
    const { result } = renderHook(() => useAttempt(6))
    await waitFor(() => expect(result.current.isLoaded).toBe(true))
    expect(result.current.isSignedOut).toBe(false)

    await act(() => vi.advanceTimersByTimeAsync(5_000))

    expect(result.current.isSignedOut).toBe(true)
    expect(result.current.saveStatus).toBe('saved')
  })

  it('stays signed in and shows the connection lost when the network fails', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    vi.spyOn(candidateApi, 'startAttempt').mockResolvedValue(running)
    vi.spyOn(candidateApi, 'heartbeat').mockRejectedValue(new TypeError('Failed to fetch'))
    const { result } = renderHook(() => useAttempt(6))
    await waitFor(() => expect(result.current.isLoaded).toBe(true))

    await act(() => vi.advanceTimersByTimeAsync(5_000))

    expect(result.current.isSignedOut).toBe(false)
    expect(result.current.saveStatus).toBe('offline')
  })

  it('reports the computer signed out when the section cannot even be opened (401)', async () => {
    vi.spyOn(candidateApi, 'startAttempt').mockRejectedValue(new ApiError(401, 'Not signed in.'))
    const { result } = renderHook(() => useAttempt(6))

    await waitFor(() => expect(result.current.isSignedOut).toBe(true))
    expect(result.current.error).toBeNull()
  })

  it('shows the sentence when Start is refused because the invigilator ended the test (409), and stays signed in', async () => {
    vi.spyOn(candidateApi, 'startAttempt').mockRejectedValue(new ApiError(409, 'The invigilator has ended your test.'))
    const { result } = renderHook(() => useAttempt(6))

    await waitFor(() => expect(result.current.error).toBe('The invigilator has ended your test.'))
    expect(result.current.isSignedOut).toBe(false)
    expect(result.current.isLoaded).toBe(false)
  })
})

/** Task 1 (review of 2 November): answers survive a reload, a closed page and a pause. */
describe('useAttempt keeps answers', () => {
  afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
    localStorage.clear()
  })

  it('sends again what an earlier page left unsent, and shows it', async () => {
    localStorage.setItem('exam-pending:Mock/123/6', JSON.stringify([{ questionNumber: 3, value: 'left over' }]))
    vi.spyOn(candidateApi, 'startAttempt').mockResolvedValue(running)
    vi.spyOn(candidateApi, 'heartbeat').mockResolvedValue(running)
    const save = vi.spyOn(candidateApi, 'saveResponses').mockResolvedValue(undefined)
    const { result } = renderHook(() => useAttempt(6, 'Mock/123'))
    await waitFor(() => expect(result.current.isLoaded).toBe(true))

    await waitFor(() => expect(result.current.answers[3]).toBe('left over'))
    await waitFor(() => expect(save).toHaveBeenCalledWith(6, [{ questionNumber: 3, value: 'left over' }]), { timeout: 3000 })
  })

  it('does not replay old answers into a section that has finished', async () => {
    localStorage.setItem('exam-pending:Mock/123/6', JSON.stringify([{ questionNumber: 3, value: 'left over' }]))
    vi.spyOn(candidateApi, 'startAttempt').mockResolvedValue({ ...running, status: 'Submitted' } as AttemptState)
    const save = vi.spyOn(candidateApi, 'saveResponses').mockResolvedValue(undefined)
    const { result } = renderHook(() => useAttempt(6, 'Mock/123'))
    await waitFor(() => expect(result.current.isLoaded).toBe(true))

    expect(save).not.toHaveBeenCalled()
    expect(localStorage.getItem('exam-pending:Mock/123/6')).toBeNull()
  })

  it('sends unsent answers when the screen closes', async () => {
    vi.spyOn(candidateApi, 'startAttempt').mockResolvedValue(running)
    vi.spyOn(candidateApi, 'heartbeat').mockResolvedValue(running)
    const save = vi.spyOn(candidateApi, 'saveResponses').mockResolvedValue(undefined)
    const { result, unmount } = renderHook(() => useAttempt(6, 'Mock/123'))
    await waitFor(() => expect(result.current.isLoaded).toBe(true))
    act(() => result.current.setAnswer(1, 'B'))

    unmount()

    expect(save).toHaveBeenCalledWith(6, [{ questionNumber: 1, value: 'B' }])
  })

  it('sends unsent answers with keepalive when the page is hidden', async () => {
    vi.spyOn(candidateApi, 'startAttempt').mockResolvedValue(running)
    vi.spyOn(candidateApi, 'heartbeat').mockResolvedValue(running)
    vi.spyOn(candidateApi, 'saveResponses').mockResolvedValue(undefined)
    const keepalive = vi.spyOn(candidateApi, 'saveResponsesOnPageHide').mockImplementation(() => {})
    const { result } = renderHook(() => useAttempt(6, 'Mock/123'))
    await waitFor(() => expect(result.current.isLoaded).toBe(true))
    act(() => result.current.setAnswer(1, 'B'))

    window.dispatchEvent(new Event('pagehide'))

    expect(keepalive).toHaveBeenCalledWith(6, [{ questionNumber: 1, value: 'B' }])
  })

  it('removes the unsent copy from this computer when the section ends while answers are still pending (final review 6)', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    vi.spyOn(candidateApi, 'startAttempt').mockResolvedValue(running)
    const heartbeat = vi.spyOn(candidateApi, 'heartbeat').mockResolvedValue(running)
    // The network is down: the answer stays pending and mirrored.
    vi.spyOn(candidateApi, 'saveResponses').mockRejectedValue(new TypeError('Failed to fetch'))
    const { result } = renderHook(() => useAttempt(6, 'Mock/123'))
    await waitFor(() => expect(result.current.isLoaded).toBe(true))
    act(() => result.current.setAnswer(1, 'B'))
    await act(() => vi.advanceTimersByTimeAsync(1_000))
    expect(localStorage.getItem('exam-pending:Mock/123/6')).not.toBeNull()

    heartbeat.mockResolvedValue({ ...running, status: 'Submitted' } as AttemptState)
    await act(() => vi.advanceTimersByTimeAsync(5_000))

    await waitFor(() => expect(result.current.status).toBe('Submitted'))
    await act(() => vi.advanceTimersByTimeAsync(5_000))
    expect(localStorage.getItem('exam-pending:Mock/123/6')).toBeNull()
  })
})
