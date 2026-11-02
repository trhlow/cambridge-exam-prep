import { act, renderHook } from '@testing-library/react'
import { useRecordingPlayer } from './useRecordingPlayer'

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
    this.paused = false
    return Promise.resolve()
  }
  pause() {
    this.paused = true
  }
}

describe('useRecordingPlayer', () => {
  beforeEach(() => {
    FakeAudio.instances = []
    FakeAudio.loads = []
    vi.stubGlobal('Audio', FakeAudio)
  })
  afterEach(() => vi.unstubAllGlobals())

  it('does not play a track that finished loading after the invigilator paused (start/stop race)', async () => {
    const { result } = renderHook(() => useRecordingPlayer(['a.mp3']))
    await act(async () => {
      await Promise.resolve()
      await Promise.resolve()
    })

    let started: Promise<void> = Promise.resolve()
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
  })
})
