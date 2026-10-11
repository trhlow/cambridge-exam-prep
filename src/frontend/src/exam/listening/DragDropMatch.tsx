import { useRef, useState, type PointerEvent } from 'react'
import type { Answers, Option, QuestionGroup } from '../../api/types'

export interface DragDropMatchProps {
  group: QuestionGroup
  answers: Answers
  onAnswer: (number: number, value: string) => void
}

/** Pointer travel before a press becomes a drag (so a plain click still picks the card). */
const DRAG_THRESHOLD_PX = 4

type Drag = { key: string; text: string; x: number; y: number }

/**
 * Inspera's matching task (A2 Listening Part 5): people with numbered slots on the left, word cards on the
 * right. A card is dragged onto a slot with the mouse or a finger — or picked and then placed with two clicks
 * from the keyboard. A placed card leaves the pool unless the task allows reusing cards.
 */
export function DragDropMatch({ group, answers, onAnswer }: DragDropMatchProps) {
  const [picked, setPicked] = useState<string | null>(null)
  const [drag, setDrag] = useState<Drag | null>(null)
  const cards = group.optionPool ?? []
  const cardByKey = new Map(cards.map((c) => [c.key, c]))
  const used = new Set(group.questions.map((q) => answers[q.number]).filter((v): v is string => Boolean(v)))
  const available = group.allowOptionReuse ? cards : cards.filter((c) => !used.has(c.key))

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

  return (
    <div className="match">
      <div className="match-people">
        <h3 className="match-heading">People</h3>
        {group.questions.map((q) => {
          const card = cardByKey.get(answers[q.number] ?? '')
          const label = q.promptHtml ?? `Question ${q.number}`
          const slotClass = ['match-slot', card && 'is-filled', (picked || drag) && !card && 'is-ready'].filter(Boolean).join(' ')
          return (
            <div key={q.number} className="match-row" data-question={q.number}>
              <span className="match-label">{label}</span>
              <span className={slotClass} data-slot={q.number}>
                <button
                  type="button"
                  className="match-slot-button"
                  aria-label={`Question ${q.number}, ${label}, ${card?.text ?? 'empty'}`}
                  onClick={() => picked && place(q.number, picked)}
                >
                  {card ? <span className="match-card match-card--placed">{card.text}</span> : <strong>{q.number}</strong>}
                </button>
                {card && (
                  <button type="button" className="match-remove" aria-label={`Remove ${card.text} from question ${q.number}`} onClick={() => onAnswer(q.number, '')}>
                    ×
                  </button>
                )}
              </span>
            </div>
          )
        })}
      </div>
      <ul className="match-pool" aria-label="Word cards">
        {available.map((card) => (
          <li key={card.key}>
            <Card
              card={card}
              isPicked={picked === card.key}
              isDragged={drag?.key === card.key}
              onPick={() => setPicked(picked === card.key ? null : card.key)}
              onDrag={(x, y) => setDrag({ key: card.key, text: card.text ?? card.key, x, y })}
              onDrop={(x, y) => {
                setDrag(null)
                dropAt(x, y, card.key)
              }}
            />
          </li>
        ))}
      </ul>
      {drag && (
        <span className="match-card match-ghost" style={{ left: drag.x, top: drag.y }} aria-hidden="true">
          {drag.text}
        </span>
      )}
    </div>
  )
}

type CardProps = {
  card: Option
  isPicked: boolean
  isDragged: boolean
  onPick: () => void
  onDrag: (x: number, y: number) => void
  onDrop: (x: number, y: number) => void
}

function Card({ card, isPicked, isDragged, onPick, onDrag, onDrop }: CardProps) {
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

  const classes = ['match-card', isPicked && 'is-picked', isDragged && 'is-dragged'].filter(Boolean).join(' ')
  return (
    <button type="button" className={classes} aria-pressed={isPicked} onPointerDown={down} onPointerMove={move} onPointerUp={up} onClick={click}>
      {card.text}
    </button>
  )
}
