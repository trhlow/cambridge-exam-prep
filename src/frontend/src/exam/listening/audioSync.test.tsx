import { act, renderHook } from '@testing-library/react'
import { realignTarget, useFollowServerPosition, useResumeAfterPause } from './audioSync'
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
    const start = vi.fn().mockResolvedValue(true)
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

describe('useFollowServerPosition', () => {
  interface Props {
    status: string
    syncedAt: number | null
    state: PlayerState
    server: number | null
    local: number | null
  }

  const setup = (initial: Props, start = vi.fn().mockResolvedValue(true)) => {
    const onRewound = vi.fn()
    const hook = renderHook(
      (p: Props) =>
        useFollowServerPosition({
          status: p.status,
          syncedAt: p.syncedAt,
          playerState: p.state,
          serverPosition: () => p.server,
          localPosition: () => p.local,
          start,
          onRewound,
        }),
      { initialProps: initial },
    )
    return { start, onRewound, ...hook }
  }

  const flush = () => act(async () => {
    await Promise.resolve()
    await Promise.resolve()
  })

  it('says the recording moved back only when it really played again after a rewind (A10)', async () => {
    const { start, onRewound } = setup({ status: 'InProgress', syncedAt: 1, state: 'finished', server: 300_000, local: null })
    await flush()

    expect(start).toHaveBeenCalledExactlyOnceWith(300_000)
    expect(onRewound).toHaveBeenCalledTimes(1)
  })

  it('stays quiet on every heartbeat once the recording finished and the server is past its end', async () => {
    const start = vi.fn().mockResolvedValue(false)
    const { onRewound, rerender } = setup({ status: 'InProgress', syncedAt: 1, state: 'finished', server: 2_000_000, local: null }, start)
    await flush()
    rerender({ status: 'InProgress', syncedAt: 2, state: 'finished', server: 2_005_000, local: null })
    await flush()

    expect(onRewound).not.toHaveBeenCalled()
  })

  it('never restarts the recording while the candidate is paused, even when a rewind arrives with the pause', async () => {
    const { start } = setup({ status: 'Paused', syncedAt: 1, state: 'finished', server: 300_000, local: null })
    await flush()

    expect(start).not.toHaveBeenCalled()
  })

  it('does not start the same restart again on each heartbeat while the track is still loading', async () => {
    let finish: (played: boolean) => void = () => undefined
    const start = vi.fn(() => new Promise<boolean>((resolve) => (finish = resolve)))
    const { rerender, onRewound } = setup({ status: 'InProgress', syncedAt: 1, state: 'finished', server: 300_000, local: null }, start)
    rerender({ status: 'InProgress', syncedAt: 2, state: 'finished', server: 305_000, local: null })
    rerender({ status: 'InProgress', syncedAt: 3, state: 'finished', server: 310_000, local: null })
    expect(start).toHaveBeenCalledTimes(1)

    await act(async () => finish(true))
    expect(onRewound).toHaveBeenCalledTimes(1)
  })

  it('follows a playing recording that drifted without calling it a rewind when it only caught up', async () => {
    const { start, onRewound } = setup({ status: 'InProgress', syncedAt: 1, state: 'playing', server: 70_000, local: 60_000 })
    await flush()

    expect(start).toHaveBeenCalledExactlyOnceWith(70_000)
    expect(onRewound).not.toHaveBeenCalled()
  })
})
