import { act, renderHook } from '@testing-library/react'
import { TRACK_LOAD_TIMEOUT_MS, useRecordingPlayer } from './useRecordingPlayer'

/** Stands in for HTMLAudioElement: metadata arrives when load() is called, as the test decides. */
class FakeAudio {
  static instances: FakeAudio[] = []
  static loads: Array<() => void> = []
  preload = ''
  volume = 1
  currentTime = 0
  duration = 10
  ended = false
  onloadedmetadata: (() => void) | null = null
  onerror: (() => void) | null = null
  playCalls = 0
  paused = true
  private listeners = new Map<string, Array<() => void>>()
  private source = ''

  constructor() {
    FakeAudio.instances.push(this)
  }

  set src(value: string) {
    this.source = value
    // The duration probe (never load()ed) answers at once.
    queueMicrotask(() => this.onloadedmetadata?.())
  }
  get src() {
    return this.source
  }
  removeAttribute() {}
  addEventListener(type: string, fn: () => void) {
    this.listeners.set(type, [...(this.listeners.get(type) ?? []), fn])
  }
  removeEventListener() {}
  load() {
    // Held until the test releases it, so a pause can arrive while the track is loading.
    FakeAudio.loads.push(() => (this.listeners.get('loadedmetadata') ?? []).forEach((fn) => fn()))
  }
  play() {
    this.playCalls += 1
    if (FakeAudio.refusePlay) {
      return Promise.reject(new DOMException('blocked', 'NotAllowedError'))
    }
    this.paused = false
    return Promise.resolve()
  }
  pause() {
    this.paused = true
  }
  emit(type: string) {
    ;(this.listeners.get(type) ?? []).forEach((fn) => fn())
  }
  static refusePlay = false
}

/** Lets the duration probes answer and the start() reach its load. */
async function settle() {
  await act(async () => {
    for (let i = 0; i < 5; i++) {
      await Promise.resolve()
    }
  })
}

/** The element the player plays through (the probes are never load()ed). */
const playerAudio = () => FakeAudio.instances.find((a) => a.preload === 'auto')!

describe('useRecordingPlayer', () => {
  beforeEach(() => {
    FakeAudio.instances = []
    FakeAudio.loads = []
    FakeAudio.refusePlay = false
    vi.stubGlobal('Audio', FakeAudio)
  })
  afterEach(() => vi.unstubAllGlobals())

  it('does not play a track that finished loading after the invigilator paused (start/stop race)', async () => {
    const { result } = renderHook(() => useRecordingPlayer(['a.mp3']))
    await act(async () => {
      await Promise.resolve()
      await Promise.resolve()
    })

    let started: Promise<boolean> = Promise.resolve(true)
    await act(async () => {
      started = result.current.start(0)
      await Promise.resolve()
      await Promise.resolve()
    })
    act(() => result.current.stop())
    await act(async () => {
      FakeAudio.loads.forEach((release) => release())
      await started
    })

    const player = FakeAudio.instances.find((a) => a.playCalls > 0 || a.paused === false)
    expect(player).toBeUndefined()
    expect(result.current.state).toBe('idle')
    expect(result.current.positionMs()).toBeNull()
    await expect(started).resolves.toBe(false)
  })

  it('tells the caller it played', async () => {
    const { result } = renderHook(() => useRecordingPlayer(['a.mp3']))
    await settle()

    let started: Promise<boolean> = Promise.resolve(false)
    await act(async () => {
      started = result.current.start(0)
    })
    await settle()
    await act(async () => {
      FakeAudio.loads.forEach((release) => release())
    })

    await expect(started).resolves.toBe(true)
    expect(result.current.state).toBe('playing')
  })

  it('reports nothing played when the server position is past the end of the recording', async () => {
    const { result } = renderHook(() => useRecordingPlayer(['a.mp3']))
    await settle()

    let started: Promise<boolean> = Promise.resolve(true)
    await act(async () => {
      started = result.current.start(60_000)
    })

    await expect(started).resolves.toBe(false)
    expect(result.current.state).toBe('finished')
  })

  it('does not go on to the next track once the invigilator paused', async () => {
    const { result } = renderHook(() => useRecordingPlayer(['a.mp3', 'b.mp3']))
    await settle()
    await act(async () => {
      void result.current.start(0)
    })
    await settle()
    await act(async () => {
      FakeAudio.loads.forEach((release) => release())
    })
    const audio = playerAudio()
    const playsBefore = audio.playCalls

    act(() => result.current.stop())
    act(() => audio.emit('ended'))

    expect(audio.playCalls).toBe(playsBefore)
    expect(audio.src).toBe('a.mp3')
  })

  it('shows the Continue listening button again when the browser refuses to restart a finished recording', async () => {
    const { result } = renderHook(() => useRecordingPlayer(['a.mp3']))
    await settle()
    await act(async () => {
      await result.current.start(60_000)
    })
    expect(result.current.state).toBe('finished')

    FakeAudio.refusePlay = true
    let started: Promise<boolean> = Promise.resolve(true)
    await act(async () => {
      started = result.current.start(1_000)
    })
    await settle()
    await act(async () => {
      FakeAudio.loads.forEach((release) => release())
      await started.catch(() => undefined)
    })

    await expect(started).rejects.toThrow('blocked')
    expect(result.current.state).toBe('idle')
  })

  it('gives up on a track whose load fails, so a later restart can still happen', async () => {
    const { result } = renderHook(() => useRecordingPlayer(['a.mp3']))
    await settle()
    let started: Promise<boolean> = Promise.resolve(true)
    await act(async () => {
      started = result.current.start(0)
    })
    await settle()

    act(() => playerAudio().emit('error'))

    await expect(started).resolves.toBe(false)
    expect(playerAudio().playCalls).toBe(0)
  })

  it('gives up on a track that never finishes loading', async () => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] })
    try {
      const { result } = renderHook(() => useRecordingPlayer(['a.mp3']))
      await settle()
      let started: Promise<boolean> = Promise.resolve(true)
      await act(async () => {
        started = result.current.start(0)
      })
      await settle()

      await act(async () => {
        vi.advanceTimersByTime(TRACK_LOAD_TIMEOUT_MS)
      })

      await expect(started).resolves.toBe(false)
    } finally {
      vi.useRealTimers()
    }
  })

  it('does not play on an element left behind by an unmount during a load', async () => {
    const { result, unmount } = renderHook(() => useRecordingPlayer(['a.mp3']))
    await settle()
    let started: Promise<boolean> = Promise.resolve(true)
    await act(async () => {
      started = result.current.start(0)
    })
    await settle()
    const audio = playerAudio()

    unmount()
    FakeAudio.loads.forEach((release) => release())

    await expect(started).resolves.toBe(false)
    expect(audio.playCalls).toBe(0)
  })
})
