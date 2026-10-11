import { useEffect, useRef, useState, type ReactNode } from 'react'
import { ApiError } from '../api/client'
import { messageOf } from './format'

/** The server's title and, when it listed them, every problem it found. */
export function ErrorBox({ error }: { error: unknown }) {
  if (!error) {
    return null
  }

  const details = error instanceof ApiError ? error.errors.filter((e) => e !== error.message) : []
  return (
    <div className="st-error" role="alert">
      <strong>{messageOf(error)}</strong>
      {details.length > 0 && (
        <ul>
          {details.map((detail) => (
            <li key={detail}>{detail}</li>
          ))}
        </ul>
      )}
    </div>
  )
}

const BADGE_TONE: Record<string, string> = {
  Draft: 'grey',
  Scheduled: 'blue',
  Active: 'green',
  Closed: 'grey',
  Published: 'green',
  Archived: 'grey',
  NotStarted: 'grey',
  InProgress: 'green',
  Paused: 'yellow',
  Submitted: 'blue',
  Invigilator: 'blue',
  Admin: 'yellow',
}

const BADGE_TEXT: Record<string, string> = { NotStarted: 'Not started', InProgress: 'In progress' }

export function Badge({ value }: { value: string }) {
  return <span className={`st-badge st-badge--${BADGE_TONE[value] ?? 'grey'}`}>{BADGE_TEXT[value] ?? value}</span>
}

export function PageHeader({ title, meta, actions }: { title: string; meta?: ReactNode; actions?: ReactNode }) {
  return (
    <div className="st-page-head">
      <div>
        <h1>{title}</h1>
        {meta && <div className="st-meta">{meta}</div>}
      </div>
      {actions && <div className="st-actions">{actions}</div>}
    </div>
  )
}

/** A modal built on <dialog>: Escape and the Close button both close it. */
export function Modal({ title, onClose, children }: { title: string; onClose: () => void; children: ReactNode }) {
  const ref = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    const dialog = ref.current
    if (dialog && !dialog.open) {
      dialog.showModal?.()
    }
  }, [])

  return (
    <dialog ref={ref} className="st-modal" onClose={onClose} aria-label={title}>
      <div className="st-modal-head">
        <h2>{title}</h2>
        <button type="button" className="st-link" onClick={onClose}>
          Close
        </button>
      </div>
      {children}
    </dialog>
  )
}

/** A destructive button that asks for a second click instead of a browser confirm box. */
export function ConfirmButton(props: { label: string; confirmLabel: string; onConfirm: () => void; className?: string; type?: 'button' | 'submit'; disabled?: boolean }) {
  const { label, confirmLabel, onConfirm, className = 'st-btn st-btn--danger', type = 'button', disabled } = props
  const [isArmed, setIsArmed] = useState(false)

  useEffect(() => {
    if (!isArmed) {
      return
    }

    const timer = setTimeout(() => setIsArmed(false), 5_000)
    return () => clearTimeout(timer)
  }, [isArmed])

  return (
    <button
      type={isArmed ? type : 'button'}
      className={isArmed ? `${className} is-armed` : className}
      disabled={disabled}
      onClick={(event) => {
        if (!isArmed) {
          event.preventDefault()
          setIsArmed(true)
          return
        }

        if (type === 'button') {
          setIsArmed(false)
          onConfirm()
        }
      }}
    >
      {isArmed ? confirmLabel : label}
    </button>
  )
}
