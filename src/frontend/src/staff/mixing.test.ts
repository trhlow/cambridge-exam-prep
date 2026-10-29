import { describe, expect, it } from 'vitest'
import { maxVersions, sourceLetter } from './mixing'

describe('mixing limits', () => {
  it('allows three papers per source', () => {
    expect(maxVersions(2, 7)).toBe(6)
    expect(maxVersions(3, 5)).toBe(9)
    expect(maxVersions(4, 7)).toBe(12)
  })

  it('allows fewer when the section has too few parts', () => {
    expect(maxVersions(2, 2)).toBe(4)
  })

  it('names the sources A, B, C…', () => {
    expect([0, 1, 2, 5].map(sourceLetter)).toEqual(['A', 'B', 'C', 'F'])
  })
})
