import { useEffect, useState } from 'react'
import { ROOT_ATTRIBUTE, currentRoot } from './useAnnotations'
import { spanFromRange, type TextSpan } from './textSpans'

type Target = { span: TextSpan; quote: string; x: number; y: number }

type Props = {
  /** Whether the selection overlaps a highlight already (then it can be removed). */
  isHighlighted: (span: TextSpan) => boolean
  /** Notes are switched off with large text sizes, as in the Cambridge booklet. */
  canNote: boolean
  onHighlight: (span: TextSpan) => void
  onRemoveHighlight: (span: TextSpan) => void
  onNote: (span: TextSpan, quote: string) => void
}

const MAX_QUOTE = 200

/** The selected text of the page, unless it is inside an answer box. */
function readSelection(): Target | null {
  const selection = document.getSelection()
  const root = currentRoot()
  if (!selection || selection.isCollapsed || selection.rangeCount === 0 || !root) {
    return null
  }

  const range = selection.getRangeAt(0)
  const start = range.startContainer instanceof Element ? range.startContainer : range.startContainer.parentElement
  if (!root.contains(range.commonAncestorContainer) || start?.closest('input, textarea')) {
    return null
  }

  const span = spanFromRange(root, root.getAttribute(ROOT_ATTRIBUTE) ?? '', range)
  if (!span) {
    return null
  }

  const rect = range.getBoundingClientRect()
  return { span, quote: range.toString().replace(/\s+/g, ' ').trim().slice(0, MAX_QUOTE), x: rect.left + rect.width / 2, y: rect.top }
}

/**
 * VE-19: selecting words of the paper shows a small menu above them — Highlight (or Remove highlight) and Note —
 * as in Inspera.
 */
export function SelectionMenu({ isHighlighted, canNote, onHighlight, onRemoveHighlight, onNote }: Props) {
  const [target, setTarget] = useState<Target | null>(null)

  useEffect(() => {
    const update = () => setTarget(readSelection())
    const hideWhenCleared = () => {
      if (document.getSelection()?.isCollapsed) {
        setTarget(null)
      }
    }
    document.addEventListener('mouseup', update)
    document.addEventListener('keyup', update)
    document.addEventListener('selectionchange', hideWhenCleared)
    return () => {
      document.removeEventListener('mouseup', update)
      document.removeEventListener('keyup', update)
      document.removeEventListener('selectionchange', hideWhenCleared)
    }
  }, [])

  if (!target) {
    return null
  }

  const done = (action: () => void) => {
    action()
    document.getSelection()?.removeAllRanges()
    setTarget(null)
  }
  const highlighted = isHighlighted(target.span)

  return (
    <div
      className="selection-menu"
      role="toolbar"
      aria-label="Selected text"
      style={{ left: target.x, top: target.y }}
      // Keep the selection while the menu is clicked.
      onMouseDown={(event) => event.preventDefault()}
    >
      {highlighted ? (
        <button type="button" onClick={() => done(() => onRemoveHighlight(target.span))}>
          Remove highlight
        </button>
      ) : (
        <button type="button" onClick={() => done(() => onHighlight(target.span))}>
          Highlight
        </button>
      )}
      {canNote && (
        <button type="button" onClick={() => done(() => onNote(target.span, target.quote))}>
          Note
        </button>
      )}
    </div>
  )
}
