import { useCallback, useEffect, useRef, useState } from 'react'
import { locateInRecording } from './recording'

export type PlayerState = 'idle' | 'playing' | 'finished'

export interface RecordingPlayer {
  state: PlayerState
  /** Starts (or resumes) the whole recording at a position given by the server. Call from a click. */
  start: (positionMs: number) => Promise<void>
  /** Only for an invigilator pause; the candidate has no way to stop the recording. */
  stop: () => void
  /** Where the recording is now (ms from its start), or null while it is not playing. */
  positionMs: () => number | null
}

function trackDuration(url: string): Promise<number> {
  return new Promise((resolve) => {
    const probe = new Audio()
    probe.preload = 'metadata'
    probe.onloadedmetadata = () => resolve(Number.isFinite(probe.duration) ? probe.duration * 1000 : 0)
    probe.onerror = () => resolve(0)
    probe.src = url
  })
}

const IGNORED_MEDIA_ACTIONS: MediaSessionAction[] = ['pause', 'stop', 'seekbackward', 'seekforward', 'seekto', 'previoustrack', 'nexttrack']

/**
 * Plays the Listening tracks back to back as one recording, as Inspera does. There are no controls: the
 * candidate cannot pause or seek, media keys are ignored, and changing part does not touch the recording.
 */
export function useRecordingPlayer(urls: string[]): RecordingPlayer {
  const [state, setState] = useState<PlayerState>('idle')
  const urlsRef = useRef(urls)
  const audioRef = useRef<HTMLAudioElement | null>(null)
  const durationsRef = useRef<Promise<number[]> | null>(null)
  /** Start of each track within the whole recording, once the durations are known. */
  const offsetsRef = useRef<number[] | null>(null)
  const trackRef = useRef(0)
  const shouldPlayRef = useRef(false)
  const urlsKey = urls.join('|')

  useEffect(() => {
    const tracks = urlsKey === '' ? [] : urlsKey.split('|')
    urlsRef.current = tracks
    if (tracks.length === 0) {
      return
    }

    const audio = new Audio()
    audio.preload = 'auto'
    audioRef.current = audio
    durationsRef.current = Promise.all(tracks.map(trackDuration))
    void durationsRef.current.then((durations) => {
      offsetsRef.current = durations.map((_, i) => durations.slice(0, i).reduce((sum, d) => sum + d, 0))
    })

    const playNext = () => {
      trackRef.current += 1
      if (trackRef.current >= tracks.length) {
        shouldPlayRef.current = false
        setState('finished')
        return
      }

      audio.src = tracks[trackRef.current]
      void audio.play()
    }
    // Anything that pauses the recording other than the end of a track (media keys, headset buttons) is undone.
    const keepPlaying = () => {
      if (shouldPlayRef.current && !audio.ended) {
        void audio.play()
      }
    }
    audio.addEventListener('ended', playNext)
    audio.addEventListener('pause', keepPlaying)
    IGNORED_MEDIA_ACTIONS.forEach((action) => {
      try {
        navigator.mediaSession?.setActionHandler(action, keepPlaying)
      } catch {
        // Not every browser supports every action.
      }
    })

    return () => {
      shouldPlayRef.current = false
      audio.removeEventListener('ended', playNext)
      audio.removeEventListener('pause', keepPlaying)
      audio.pause()
      audio.removeAttribute('src')
      audioRef.current = null
    }
  }, [urlsKey])

  const start = useCallback(async (positionMs: number) => {
    const audio = audioRef.current
    if (!audio || !durationsRef.current) {
      return
    }

    const point = locateInRecording(positionMs, await durationsRef.current)
    if (!point) {
      setState('finished')
      return
    }

    trackRef.current = point.trackIndex
    audio.src = urlsRef.current[point.trackIndex]
    await new Promise<void>((resolve) => {
      audio.addEventListener('loadedmetadata', () => resolve(), { once: true })
      audio.load()
    })
    audio.currentTime = point.offsetMs / 1000
    shouldPlayRef.current = true
    await audio.play()
    setState('playing')
  }, [])

  const stop = useCallback(() => {
    shouldPlayRef.current = false
    audioRef.current?.pause()
    setState((current) => (current === 'finished' ? current : 'idle'))
  }, [])

  const positionMs = useCallback(() => {
    const audio = audioRef.current
    const offsets = offsetsRef.current
    if (!audio || !offsets || !shouldPlayRef.current) {
      return null
    }

    return (offsets[trackRef.current] ?? 0) + audio.currentTime * 1000
  }, [])

  return { state, start, stop, positionMs }
}
