import { remainingSecondsNow, timeLeftText } from './timing'

describe('timeLeftText', () => {
  it.each([
    [3600, '60 minutes left'],
    [3541, '60 minutes left'],
    [3540, '59 minutes left'],
    [301, '6 minutes left'],
    [300, '5:00 left'],
    [299, '4:59 left'],
    [61, '1:01 left'],
    [9, '0:09 left'],
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

  it('never gains time when the last clock tick is older than the reply', () => {
    // The clock ticks once a second, so right after a reply "now" can still be up to a second behind it.
    expect(remainingSecondsNow(3600, 1_000_000, 999_600)).toBe(3600)
    expect(timeLeftText(remainingSecondsNow(3600, 1_000_000, 999_600))).toBe('60 minutes left')
  })

  it('never goes below zero', () => {
    expect(remainingSecondsNow(5, 0, 60_000)).toBe(0)
  })
})
