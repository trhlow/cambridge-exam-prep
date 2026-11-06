import { clearAnnotations } from './useAnnotations'

// Review B13: notes are "only available during the test"; Exit removes them so the next person at the computer sees none.
describe('clearAnnotations', () => {
  beforeEach(() => sessionStorage.clear())

  it('removes the notes and highlights of every section and leaves other keys alone', () => {
    sessionStorage.setItem('victory.annotations.1', '{"notes":[1]}')
    sessionStorage.setItem('victory.annotations.2', '{"highlights":[1]}')
    sessionStorage.setItem('victory.openSection', '2')

    clearAnnotations()

    expect(sessionStorage.getItem('victory.annotations.1')).toBeNull()
    expect(sessionStorage.getItem('victory.annotations.2')).toBeNull()
    expect(sessionStorage.getItem('victory.openSection')).toBe('2')
  })
})
