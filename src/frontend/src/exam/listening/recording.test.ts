import { locateInRecording } from './recording'

// Five Listening tracks played back to back as one recording, as Inspera does.
const durations = [120_000, 150_000, 180_000, 180_000, 200_000]

describe('locateInRecording', () => {
  it('finds the track and the offset for a position in the whole recording', () => {
    expect(locateInRecording(0, durations)).toEqual({ trackIndex: 0, offsetMs: 0 })
    expect(locateInRecording(119_999, durations)).toEqual({ trackIndex: 0, offsetMs: 119_999 })
    expect(locateInRecording(120_000, durations)).toEqual({ trackIndex: 1, offsetMs: 0 })
    expect(locateInRecording(300_000, durations)).toEqual({ trackIndex: 2, offsetMs: 30_000 })
  })

  it('reports the end once every track has played', () => {
    expect(locateInRecording(830_000, durations)).toBeNull()
    expect(locateInRecording(999_999, durations)).toBeNull()
  })
})
