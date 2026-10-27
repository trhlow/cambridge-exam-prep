import { useEffect, useRef, useState } from 'react'
import { CloseIcon, PersonIcon } from './icons'

/**
 * The candidate icon at the top right. Cambridge's exam day instructions have every candidate open it after the PIN
 * to check the spelling of their name, then close it with the cross or by clicking anywhere on the screen
 * (Exam Day Booklet for Digital exams 2026, p. 11).
 */
export function CandidateIcon({ fullName, candidateNumber }: { fullName: string; candidateNumber: string }) {
  const [isOpen, setIsOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!isOpen) {
      return
    }

    const close = (event: MouseEvent) => {
      if (!ref.current?.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }
    document.addEventListener('mousedown', close)
    return () => document.removeEventListener('mousedown', close)
  }, [isOpen])

  return (
    <div className="candidate-icon" ref={ref}>
      <button type="button" className="top-icon" aria-label="Candidate" aria-expanded={isOpen} onClick={() => setIsOpen(!isOpen)}>
        <PersonIcon />
      </button>
      {isOpen && (
        <div className="candidate-card" role="dialog" aria-label="Candidate">
          <button type="button" className="candidate-card-close" aria-label="Close" onClick={() => setIsOpen(false)}>
            <CloseIcon />
          </button>
          <div className="candidate-card-name">{fullName}</div>
          <div className="candidate-card-number">{candidateNumber}</div>
        </div>
      )}
    </div>
  )
}
