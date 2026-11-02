import { renderHook } from '@testing-library/react'
import { realignTarget, useResumeAfterPause } from './audioSync'
import type { PlayerState } from './useRecordingPlayer'

describe('realignTarget', () => {
  it('leaves a recording that is close to the server position alone', () => {
    expect(realignTarget('playing', 61_000, 60_000)).toBeNull()
  })

  it('follows the server when a playing recording is far off', () => {
    expect(realignTarget('playing', 30_000, 60_000)).toBe(30_000)
  })

  it('plays again after a rewind when the recording had already finished (A10)', () => {
    expect(realignTarget('finished', 600_000, null)).toBe(600_000)
  })

  it('does nothing while idle, the candidate has to press Play', () => {
    expect(realignTarget('idle', 30_000, null)).toBeNull()
  })
})

describe('useResumeAfterPause (A9)', () => {
  const setup = (initial: { status: string; state: PlayerState }) => {
    const start = vi.fn().mockResolvedValue(undefined)
    const hook = renderHook((props) => useResumeAfterPause(props.status, props.state, () => 42_000, start), { initialProps: initial })
    return { start, ...hook }
  }

  it('restarts the recording at the server position when the pause ends', () => {
    const { start, rerender } = setup({ status: 'InProgress', state: 'playing' })
    rerender({ status: 'Paused', state: 'idle' })
    expect(start).not.toHaveBeenCalled()

    rerender({ status: 'InProgress', state: 'idle' })

    expect(start).toHaveBeenCalledExactlyOnceWith(42_000)
  })

  it('does not start a recording the candidate never started', () => {
    const { start, rerender } = setup({ status: 'InProgress', state: 'idle' })
    rerender({ status: 'Paused', state: 'idle' })
    rerender({ status: 'InProgress', state: 'idle' })

    expect(start).not.toHaveBeenCalled()
  })

  it('leaves the Continue listening button to the candidate when the browser refuses', async () => {
    const start = vi.fn().mockRejectedValue(new DOMException('blocked', 'NotAllowedError'))
    const { rerender } = renderHook((p) => useResumeAfterPause(p.status, p.state, () => 1_000, start), {
      initialProps: { status: 'InProgress', state: 'playing' as PlayerState },
    })
    rerender({ status: 'Paused', state: 'idle' })
    rerender({ status: 'InProgress', state: 'idle' })

    await Promise.resolve()
    expect(start).toHaveBeenCalledTimes(1)
  })
})
