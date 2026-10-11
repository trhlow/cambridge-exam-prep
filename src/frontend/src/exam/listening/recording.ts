export interface RecordingPoint {
  trackIndex: number
  offsetMs: number
}

/**
 * The Listening parts are separate tracks played back to back as one recording. Maps a position in that
 * recording to a track and an offset inside it; null once the whole recording has played.
 */
export function locateInRecording(positionMs: number, durationsMs: number[]): RecordingPoint | null {
  let start = 0
  for (let trackIndex = 0; trackIndex < durationsMs.length; trackIndex++) {
    const end = start + durationsMs[trackIndex]
    if (positionMs < end) {
      return { trackIndex, offsetMs: Math.max(0, positionMs - start) }
    }

    start = end
  }

  return null
}
