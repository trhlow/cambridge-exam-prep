import { useEffect, useLayoutEffect, useState } from 'react'
import { EMPTY, type AnnotationState } from './store'
import { rangeFromSpan } from './textSpans'

/** Marks the element holding the page's text; its value says which page it is. */
export const ROOT_ATTRIBUTE = 'data-annotation-root'

const storageKey = (sectionId: number) => `victory.annotations.${sectionId}`

function read(sectionId: number): AnnotationState {
  try {
    const stored = sessionStorage.getItem(storageKey(sectionId))
    return stored ? { ...EMPTY, ...(JSON.parse(stored) as Partial<AnnotationState>) } : EMPTY
  } catch {
    return EMPTY
  }
}

/**
 * The candidate's highlights and notes for one section. Private to this tab and kept only for the test, as in
 * Inspera ("only available during the test"); a reload keeps them.
 */
export function useAnnotations(sectionId: number): [AnnotationState, (update: (state: AnnotationState) => AnnotationState) => void] {
  const [state, setState] = useState(() => read(sectionId))

  useEffect(() => {
    try {
      sessionStorage.setItem(storageKey(sectionId), JSON.stringify(state))
    } catch {
      // Storage blocked: highlights simply do not survive a reload.
    }
  }, [sectionId, state])

  return [state, setState]
}

export function currentRoot(): HTMLElement | null {
  return document.querySelector<HTMLElement>(`[${ROOT_ATTRIBUTE}]`)
}

/**
 * Colours the highlights and noted words of the page now shown, with the CSS Custom Highlight API: the exam
 * text itself is never rewritten, so answers and gap controls are untouched. Runs again whenever the page changes.
 */
export function usePaintedAnnotations(state: AnnotationState, pageKey: string) {
  useLayoutEffect(() => {
    if (typeof CSS === 'undefined' || !('highlights' in CSS) || typeof Highlight === 'undefined') {
      return
    }

    const root = currentRoot()
    const key = root?.getAttribute(ROOT_ATTRIBUTE)
    const ranges = (spans: { root: string; start: number; end: number }[]) =>
      root ? spans.filter((s) => s.root === key).map((s) => rangeFromSpan(root, s)).filter((r): r is Range => r !== null) : []

    CSS.highlights.set('exam-highlight', new Highlight(...ranges(state.highlights)))
    CSS.highlights.set('exam-note', new Highlight(...ranges(state.notes)))
    return () => {
      CSS.highlights.delete('exam-highlight')
      CSS.highlights.delete('exam-note')
    }
  }, [state, pageKey])
}
