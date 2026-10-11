import { remainingSecondsNow, timeLeftText } from './timing'

describe('timeLeftText', () => {
  it.each([
    [3600, '60 minutes left'],
    [3541, '60 minutes left'],
    [3540, '59 minutes left'],
    [61, '2 minutes left'],
    [60, '1 minute left'],
    [59, '59 seconds left'],
    [1, '1 second left'],
    [0, 'Time is up'],
  ])('shows %i seconds as "%s"', (seconds, text) => {
    expect(timeLeftText(seconds)).toBe(text)
  })
})

describe('remainingSecondsNow', () => {
  it('counts down from what the server said, using only elapsed time on this computer', () => {
    // The server said 600 s were left; 12.4 s have passed locally since the reply arrived.
    expect(remainingSecondsNow(600, 1_000_000, 1_012_400)).toBe(588)
  })

  it('never goes below zero', () => {
    expect(remainingSecondsNow(5, 0, 60_000)).toBe(0)
  })
})
