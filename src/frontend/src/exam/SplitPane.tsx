import { useRef, useState, type ReactNode, type PointerEvent, type KeyboardEvent } from 'react'

type Props = {
  left: ReactNode
  right: ReactNode
}

const MIN_PERCENT = 20
const MAX_PERCENT = 80
const KEY_STEP = 5

/** Passage left, questions right, each scrolling on its own; the ↔ handle resizes them, as in Inspera. */
export function SplitPane({ left, right }: Props) {
  const containerRef = useRef<HTMLDivElement>(null)
  const [leftPercent, setLeftPercent] = useState(50)
  const clamp = (value: number) => Math.min(MAX_PERCENT, Math.max(MIN_PERCENT, value))

  const startDrag = (event: PointerEvent<HTMLButtonElement>) => {
    event.currentTarget.setPointerCapture(event.pointerId)
  }

  const drag = (event: PointerEvent<HTMLButtonElement>) => {
    const box = containerRef.current?.getBoundingClientRect()
    if (!box || !event.currentTarget.hasPointerCapture(event.pointerId)) {
      return
    }

    setLeftPercent(clamp(((event.clientX - box.left) / box.width) * 100))
  }

  const nudge = (event: KeyboardEvent<HTMLButtonElement>) => {
    if (event.key === 'ArrowLeft') setLeftPercent((p) => clamp(p - KEY_STEP))
    if (event.key === 'ArrowRight') setLeftPercent((p) => clamp(p + KEY_STEP))
  }

  return (
    <div ref={containerRef} className="split" style={{ gridTemplateColumns: `${leftPercent}fr auto ${100 - leftPercent}fr` }}>
      <div className="split-pane split-pane--left">{left}</div>
      <div className="split-divider">
        <button
          type="button"
          className="split-handle"
          aria-label="Resize the two columns"
          onPointerDown={startDrag}
          onPointerMove={drag}
          onKeyDown={nudge}
        >
          ↔
        </button>
      </div>
      <div className="split-pane split-pane--right">{right}</div>
    </div>
  )
}
