import DOMPurify from 'dompurify'
import { useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import type { Answers, Question, QuestionGroup } from '../../api/types'
import { withGapSlots } from '../paperLogic'

export interface GapTextProps {
  group: QuestionGroup
  answers: Answers
  onAnswer: (number: number, value: string) => void
}

/**
 * A text with numbered gaps ({{q:n}}). The text is rendered as HTML once; each gap control is portalled
 * into its slot, so typing or choosing never re-renders the passage.
 */
export function GapText({ group, answers, onAnswer }: GapTextProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const [slots, setSlots] = useState<Map<number, HTMLElement>>(new Map())
  const [openGap, setOpenGap] = useState<number | null>(null)
  const html = useMemo(() => DOMPurify.sanitize(withGapSlots(group.bodyHtml ?? '')), [group.bodyHtml])

  // The gap slots only exist in the DOM after React has injected the passage HTML, so they are read here
  // (synchronising with that injected DOM) and stored to portal the gap controls into them.
  useLayoutEffect(() => {
    const found = new Map<number, HTMLElement>()
    containerRef.current?.querySelectorAll<HTMLElement>('[data-gap]').forEach((el) => found.set(Number(el.dataset.gap), el))
    // oxlint-disable-next-line react/set-state-in-effect -- reads DOM created by dangerouslySetInnerHTML
    setSlots(found)
  }, [html])

  return (
    <div className="gap-text">
      <div ref={containerRef} className="html" dangerouslySetInnerHTML={{ __html: html }} />
      {group.questions.map((question) => {
        const slot = slots.get(question.number)
        if (!slot) {
          return null
        }

        const control =
          group.interactionType === 'InlineChoice' ? (
            <ChoiceGap
              question={question}
              value={answers[question.number]}
              isOpen={openGap === question.number}
              onOpen={() => setOpenGap(openGap === question.number ? null : question.number)}
              onClose={() => setOpenGap(null)}
              onAnswer={onAnswer}
            />
          ) : (
            <TextGap question={question} value={answers[question.number] ?? ''} onAnswer={onAnswer} />
          )
        return createPortal(control, slot, String(question.number))
      })}
    </div>
  )
}

type ChoiceGapProps = {
  question: Question
  value: string | undefined
  isOpen: boolean
  onOpen: () => void
  onClose: () => void
  onAnswer: (number: number, value: string) => void
}

/** Inspera's word picker: a dark bar above the gap with × and the words to choose from. */
function ChoiceGap({ question, value, isOpen, onOpen, onClose, onAnswer }: ChoiceGapProps) {
  const ref = useRef<HTMLSpanElement>(null)
  const chosen = question.options?.find((o) => o.key === value)

  useEffect(() => {
    if (!isOpen) {
      return
    }

    const close = (event: Event) => {
      if (event instanceof KeyboardEvent ? event.key === 'Escape' : !ref.current?.contains(event.target as Node)) {
        onClose()
      }
    }
    document.addEventListener('mousedown', close)
    document.addEventListener('keydown', close)
    return () => {
      document.removeEventListener('mousedown', close)
      document.removeEventListener('keydown', close)
    }
  }, [isOpen, onClose])

  const choose = (key: string) => {
    onAnswer(question.number, key)
    onClose()
  }

  return (
    <span ref={ref} className="gap gap--choice">
      {isOpen && (
        <span className="gap-picker" role="listbox" aria-label={`Choices for question ${question.number}`}>
          <button type="button" className="gap-clear" aria-label="Clear answer" onClick={() => choose('')}>
            ×
          </button>
          {(question.options ?? []).map((option) => (
            <span
              key={option.key}
              role="option"
              tabIndex={0}
              aria-selected={option.key === value}
              className="gap-choice"
              onClick={() => choose(option.key)}
              onKeyDown={(e) => (e.key === 'Enter' || e.key === ' ') && choose(option.key)}
            >
              {option.text}
            </span>
          ))}
        </span>
      )}
      <button
        type="button"
        className={['gap-box', isOpen && 'is-open', chosen && 'is-answered'].filter(Boolean).join(' ')}
        data-question={question.number}
        aria-label={`Question ${question.number}`}
        aria-expanded={isOpen}
        onClick={onOpen}
      >
        {chosen?.text ?? question.number}
      </button>
    </span>
  )
}

function TextGap({ question, value, onAnswer }: { question: Question; value: string; onAnswer: (number: number, value: string) => void }) {
  return (
    <input
      className="gap gap-box gap--text"
      data-question={question.number}
      aria-label={`Question ${question.number}`}
      placeholder={String(question.number)}
      value={value}
      spellCheck={false}
      autoComplete="off"
      autoCorrect="off"
      autoCapitalize="off"
      onChange={(e) => onAnswer(question.number, e.target.value)}
    />
  )
}
