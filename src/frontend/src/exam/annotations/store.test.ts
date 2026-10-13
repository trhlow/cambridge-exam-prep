import { addHighlight, addNote, EMPTY, removeHighlights, removeNote, updateNote } from './store'

const span = (start: number, end: number, root = 'p1') => ({ root, start, end })

describe('annotation store (VE-19)', () => {
  it('merges a new highlight with the ones it touches on the same page', () => {
    let state = addHighlight(EMPTY, span(10, 20))
    state = addHighlight(state, span(30, 40))
    state = addHighlight(state, span(15, 32))
    state = addHighlight(state, span(15, 32, 'p2'))

    expect(state.highlights).toEqual([span(10, 40), span(15, 32, 'p2')])
  })

  it('removes the highlights a selection overlaps, and only on that page', () => {
    const state = [span(0, 5), span(10, 20), span(10, 20, 'p2')].reduce(addHighlight, EMPTY)

    expect(removeHighlights(state, span(12, 13)).highlights).toEqual([span(0, 5), span(10, 20, 'p2')])
  })

  it('keeps notes with the quoted text, edits and deletes them', () => {
    let state = addNote(EMPTY, span(3, 9), 'quoted', 'n1')
    state = updateNote(state, 'n1', 'remember this')

    expect(state.notes).toEqual([{ id: 'n1', ...span(3, 9), quote: 'quoted', text: 'remember this' }])
    expect(removeNote(state, 'n1').notes).toEqual([])
  })
})
