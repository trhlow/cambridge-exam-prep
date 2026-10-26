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
})
