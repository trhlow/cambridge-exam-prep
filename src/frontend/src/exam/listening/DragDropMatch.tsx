import DOMPurify from 'dompurify'
import { useLayoutEffect, useMemo, useRef, useState, type PointerEvent, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import type { Answers, Option, QuestionGroup } from '../../api/types'
import { HtmlContent } from '../../ui/HtmlContent'
import { gapNumbers, withGapSlots } from '../paperLogic'

export interface DragDropMatchProps {
  group: QuestionGroup
  answers: Answers
  onAnswer: (number: number, value: string) => void
}

/** Pointer travel before a press becomes a drag (so a plain click still picks the card). */
const DRAG_THRESHOLD_PX = 4

type Drag = { key: string; text: string; x: number; y: number }

/** What a card says, for the drag ghost and screen readers: its title, else its text. */
const cardName = (card: Option) => card.title ?? card.text ?? card.key

/**
 * Inspera's drag-and-drop tasks. Cards on the right are dragged onto numbered slots with the mouse or a finger — or
 * picked and then placed with two clicks from the keyboard; a placed card leaves the pool unless the task allows
 * reusing cards. The slots come in three layouts, read from the paper:
 * - rows of short labels (A2 Listening Part 5: a name, then its slot);
 * - people with a picture and a description, the slot under each (B1 Reading Part 2, long cards with a title);
 * - gaps in a text ({{q:n}} in the group's text, B1 Reading Part 4: sentences dragged into the article).
 */
export function DragDropMatch({ group, answers, onAnswer }: DragDropMatchProps) {
  const [picked, setPicked] = useState<string | null>(null)
  const [drag, setDrag] = useState<Drag | null>(null)
  const cards = group.optionPool ?? []
  const cardByKey = new Map(cards.map((c) => [c.key, c]))
  const used = new Set(group.questions.map((q) => answers[q.number]).filter((v): v is string => Boolean(v)))
  const available = group.allowOptionReuse ? cards : cards.filter((c) => !used.has(c.key))
  const isText = gapNumbers(group.bodyHtml ?? '').length > 0
  const isBlocks = !isText && group.questions.some((q) => q.stimulusHtml)
  const itemsHeading = group.itemsHeading ?? (isText ? null : 'People')

  const place = (number: number, key: string) => {
    if (cardByKey.has(key)) {
      onAnswer(number, key)
    }

    setPicked(null)
  }

  /** The slot under the pointer when a dragged card is let go. */
  const dropAt = (x: number, y: number, key: string) => {
    const slot = document.elementFromPoint(x, y)?.closest<HTMLElement>('[data-slot]')
    if (slot) {
      place(Number(slot.dataset.slot), key)
    }
  }

  const slot = (number: number, label: string | null, wide: boolean) => {
    const card = cardByKey.get(answers[number] ?? '')
    const slotClass = ['match-slot', wide && 'match-slot--wide', card && 'is-filled', (picked || drag) && !card && 'is-ready'].filter(Boolean).join(' ')
    const name = label ? `Question ${number}, ${label}` : `Question ${number}`
    return (
      <span className={slotClass} data-slot={number}>
        <button type="button" className="match-slot-button" aria-label={`${name}, ${card ? cardName(card) : 'empty'}`} onClick={() => picked && place(number, picked)}>
          {card ? <CardFace card={card} className="match-card match-card--placed" full={wide} /> : <strong className="match-slot-number">{number}</strong>}
        </button>
        {card && (
          <button type="button" className="match-remove" aria-label={`Remove ${cardName(card)} from question ${number}`} onClick={() => onAnswer(number, '')}>
            ×
          </button>
        )}
      </span>
    )
  }

  let items: ReactNode
  if (isText) {
    items = <GapPassage html={group.bodyHtml ?? ''} numbers={group.questions.map((q) => q.number)} slot={(n) => slot(n, null, true)} />
  } else if (isBlocks) {
    items = group.questions.map((q) => (
      <div key={q.number} className="match-person" data-question={q.number}>
        <HtmlContent html={q.stimulusHtml} className="match-person-picture" />
        <HtmlContent html={q.promptHtml} className="match-person-text" />
        {slot(q.number, null, true)}
      </div>
    ))
  } else {
    items = group.questions.map((q) => {
      const label = q.promptHtml ?? `Question ${q.number}`
      return (
        <div key={q.number} className="match-row" data-question={q.number}>
          <span className="match-label">{label}</span>
          {slot(q.number, label, false)}
        </div>
      )
    })
  }

  const isLong = isText || isBlocks
  return (
    <div className={isLong ? 'match match--long' : 'match'}>
      <div className="match-people">
        {itemsHeading && <h3 className="match-heading">{itemsHeading}</h3>}
        {items}
      </div>
      <div>
        {group.poolHeading && <h3 className="match-heading">{group.poolHeading}</h3>}
        <ul className={group.poolHeading ? 'match-pool match-pool--headed' : 'match-pool'} aria-label={group.poolHeading ?? 'Word cards'}>
          {available.map((card) => (
            <li key={card.key}>
              <Card
                card={card}
                full={isLong}
                isPicked={picked === card.key}
                isDragged={drag?.key === card.key}
                onPick={() => setPicked(picked === card.key ? null : card.key)}
                onDrag={(x, y) => setDrag({ key: card.key, text: cardName(card), x, y })}
                onDrop={(x, y) => {
                  setDrag(null)
                  dropAt(x, y, card.key)
                }}
              />
            </li>
          ))}
        </ul>
      </div>
      {drag && (
        <span className="match-card match-ghost" style={{ left: drag.x, top: drag.y }} aria-hidden="true">
          {drag.text}
        </span>
      )}
    </div>
  )
}

/** A card as it reads: a long card has its title in bold before its text (B1 Reading Part 2). */
function CardFace({ card, className, full }: { card: Option; className: string; full: boolean }) {
  return (
    <span className={full ? `${className} match-card--long` : className}>
      {card.title && <strong>{card.title} </strong>}
      {card.title ? card.text : (card.text ?? card.key)}
    </span>
  )
}

/** The group's text rendered once, with each gap's slot portalled into it (as GapText does for typed gaps). */
function GapPassage({ html, numbers, slot }: { html: string; numbers: number[]; slot: (n: number) => ReactNode }) {
  const containerRef = useRef<HTMLDivElement>(null)
  const [targets, setTargets] = useState<Map<number, HTMLElement>>(new Map())
  const safe = useMemo(() => DOMPurify.sanitize(withGapSlots(html)), [html])

  useLayoutEffect(() => {
    const found = new Map<number, HTMLElement>()
    containerRef.current?.querySelectorAll<HTMLElement>('[data-gap]').forEach((el) => found.set(Number(el.dataset.gap), el))
    // oxlint-disable-next-line react/set-state-in-effect -- reads DOM created by dangerouslySetInnerHTML
    setTargets(found)
  }, [safe])

  return (
    <div className="gap-text match-text">
      <div ref={containerRef} className="html" dangerouslySetInnerHTML={{ __html: safe }} />
      {numbers.map((n) => {
        const target = targets.get(n)
        return target ? createPortal(slot(n), target, String(n)) : null
      })}
    </div>
  )
}

type CardProps = {
  card: Option
  full: boolean
  isPicked: boolean
  isDragged: boolean
  onPick: () => void
  onDrag: (x: number, y: number) => void
  onDrop: (x: number, y: number) => void
}

function Card({ card, full, isPicked, isDragged, onPick, onDrag, onDrop }: CardProps) {
  const pressRef = useRef<{ x: number; y: number; isDragging: boolean } | null>(null)
  const suppressClickRef = useRef(false)

  const down = (e: PointerEvent<HTMLButtonElement>) => {
    e.currentTarget.setPointerCapture(e.pointerId)
    pressRef.current = { x: e.clientX, y: e.clientY, isDragging: false }
  }

  const move = (e: PointerEvent<HTMLButtonElement>) => {
    const press = pressRef.current
    if (!press) {
      return
    }

    if (!press.isDragging && Math.hypot(e.clientX - press.x, e.clientY - press.y) < DRAG_THRESHOLD_PX) {
      return
    }

    press.isDragging = true
    onDrag(e.clientX, e.clientY)
  }

  const up = (e: PointerEvent<HTMLButtonElement>) => {
    const press = pressRef.current
    pressRef.current = null
    if (press?.isDragging) {
      // A drag ends with a click on the card itself; that click must not also pick the card.
      suppressClickRef.current = true
      onDrop(e.clientX, e.clientY)
    }
  }

  const click = () => {
    if (suppressClickRef.current) {
      suppressClickRef.current = false
      return
    }

    onPick()
  }

  const classes = ['match-card', full && 'match-card--long', isPicked && 'is-picked', isDragged && 'is-dragged'].filter(Boolean).join(' ')
  return (
    <button type="button" className={classes} aria-pressed={isPicked} onPointerDown={down} onPointerMove={move} onPointerUp={up} onClick={click}>
      {card.title && <strong>{card.title} </strong>}
      {card.title ? card.text : (card.text ?? card.key)}
    </button>
  )
}
