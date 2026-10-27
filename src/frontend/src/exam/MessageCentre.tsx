import { useState } from 'react'
import type { CandidateMessage } from '../api/types'
import { BellIcon } from '../ui/icons'

/**
 * Messages belong to the exam, not to a section, so what was read is kept per candidate and exam: a message read
 * during Reading & Writing must not pop up again when Listening opens (mock exam, VE-43).
 */
const seenKey = (reader: string) => `victory.messages.seen.${reader}`

function readSeen(reader: string): number {
  try {
    return Number(sessionStorage.getItem(seenKey(reader)) ?? 0)
  } catch {
    return 0
  }
}

const timeOf = (iso: string) => new Date(iso).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })

/**
 * VE-17: the bell in the candidate header. A new message from the invigilator appears on screen until the candidate
 * presses OK; every message stays listed under the bell, on a screen that hides the test. What was read survives a
 * reload of this tab.
 */
export function MessageCentre({ reader, messages }: { reader: string; messages: CandidateMessage[] }) {
  const [seenId, setSeenId] = useState(() => readSeen(reader))
  const [isOpen, setIsOpen] = useState(false)
  const unread = messages.filter((m) => m.id > seenId)
  const newest = unread.at(-1)

  const markRead = () => {
    const lastId = messages.at(-1)?.id ?? seenId
    setSeenId(lastId)
    try {
      sessionStorage.setItem(seenKey(reader), String(lastId))
    } catch {
      // Private mode: messages are simply shown again after a reload.
    }
  }

  return (
    <div className="messages">
      <button
        type="button"
        className="top-icon"
        aria-label={unread.length > 0 ? `Messages, ${unread.length} new` : 'Messages'}
        aria-expanded={isOpen}
        onClick={() => {
          setIsOpen(!isOpen)
          markRead()
        }}
      >
        <BellIcon />
        {unread.length > 0 && <span className="messages-count">{unread.length}</span>}
      </button>
      {isOpen && (
        // Covers the test like the Options screen: the Cambridge booklet lets candidates hide their answers with the
        // notification icon too, and click X to come back (Exam Day Booklet for Digital exams 2026, pp. 12-13).
        <div className="opt-screen messages-panel" role="dialog" aria-modal="true" aria-label="Messages">
          <div className="opt-screen-head">
            <h2>Messages</h2>
            <button type="button" className="opt-screen-close" aria-label="Close messages" onClick={() => setIsOpen(false)}>
              ✕
            </button>
          </div>
          {messages.length === 0 ? (
            <p className="messages-empty">No messages.</p>
          ) : (
            <ul aria-label="Messages from the invigilator">
              {[...messages].reverse().map((m) => (
                <li key={m.id}>
                  <span className="messages-time">{timeOf(m.sentAt)}</span>
                  {m.text}
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
      {newest && !isOpen && (
        <div className="message-popup" role="alertdialog" aria-label="Message from the invigilator">
          <p className="message-popup-title">Message from the invigilator</p>
          <p>{newest.text}</p>
          <button type="button" className="btn btn--primary" onClick={markRead}>
            OK
          </button>
        </div>
      )}
    </div>
  )
}
