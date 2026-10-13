import type { TextSpan } from './textSpans'

export interface Note extends TextSpan {
  id: string
  /** The selected words the note is about, shown above it in the Notes panel. */
  quote: string
  text: string
}

export interface AnnotationState {
  highlights: TextSpan[]
  notes: Note[]
}

export const EMPTY: AnnotationState = { highlights: [], notes: [] }

const overlaps = (a: TextSpan, b: TextSpan) => a.root === b.root && a.start <= b.end && b.start <= a.end

/** Adds a highlight, merged with any it touches, so removing one later removes the whole coloured stretch. */
export function addHighlight(state: AnnotationState, span: TextSpan): AnnotationState {
  const touching = state.highlights.filter((h) => overlaps(h, span))
  const merged = touching.reduce((all, h) => ({ ...all, start: Math.min(all.start, h.start), end: Math.max(all.end, h.end) }), span)
  return { ...state, highlights: [...state.highlights.filter((h) => !overlaps(h, span)), merged].sort((a, b) => a.root.localeCompare(b.root) || a.start - b.start) }
}

export function removeHighlights(state: AnnotationState, span: TextSpan): AnnotationState {
  return { ...state, highlights: state.highlights.filter((h) => !(h.root === span.root && h.start < span.end && span.start < h.end)) }
}

export function addNote(state: AnnotationState, span: TextSpan, quote: string, id: string): AnnotationState {
  return { ...state, notes: [...state.notes, { id, ...span, quote, text: '' }] }
}

export function updateNote(state: AnnotationState, id: string, text: string): AnnotationState {
  return { ...state, notes: state.notes.map((n) => (n.id === id ? { ...n, text } : n)) }
}

export function removeNote(state: AnnotationState, id: string): AnnotationState {
  return { ...state, notes: state.notes.filter((n) => n.id !== id) }
}
