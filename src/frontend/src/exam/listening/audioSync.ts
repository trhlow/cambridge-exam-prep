import { useEffect, useRef } from 'react'
import type { PlayerState } from './useRecordingPlayer'

/** How far the local recording may drift from the server's position before it is realigned. */
export const AUDIO_DRIFT_MS = 4_000

/**
 * Where the recording should be restarted to follow the server, or null when it is fine where it is. A recording
 * that already finished is restarted too when the server position went back (an invigilator rewind).
 */
export function realignTarget(playerState: PlayerState, server: number | null, local: number | null): number | null {
  if (server === null) {
    return null
  }

  if (playerState === 'finished') {
    return server
  }

  if (playerState !== 'playing' || local === null || Math.abs(server - local) < AUDIO_DRIFT_MS) {
    return null
  }

  return server
}

/**
 * After an invigilator pause the recording stops. When the status goes Paused → InProgress and the candidate had
 * already pressed Play, it restarts at the server position without another click (the browser still allows it, the
 * page was started by a click). If the browser refuses, the player stays idle and the Continue listening button shows.
 */
export function useResumeAfterPause(
  status: string | null,
  playerState: PlayerState,
  positionNow: () => number | null,
  start: (positionMs: number) => Promise<void>,
) {
  const previousStatus = useRef(status)
  const hasPlayed = useRef(false)

  useEffect(() => {
    if (playerState === 'playing') {
      hasPlayed.current = true
    }
  }, [playerState])

  useEffect(() => {
    const wasPaused = previousStatus.current === 'Paused'
    previousStatus.current = status
    if (!wasPaused || status !== 'InProgress' || !hasPlayed.current || playerState !== 'idle') {
      return
    }

    const position = positionNow()
    if (position !== null) {
      start(position).catch(() => undefined)
    }
  }, [status, playerState, positionNow, start])
}
