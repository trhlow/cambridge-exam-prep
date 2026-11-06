import { localDate } from './format'

describe('localDate', () => {
  it('is the date on the wall clock, not the UTC date (just after midnight in Vietnam it is still yesterday in UTC)', () => {
    expect(localDate(new Date(2026, 10, 7, 0, 30))).toBe('2026-11-07')
    expect(localDate(new Date(2026, 0, 5, 23, 59))).toBe('2026-01-05')
  })
})
