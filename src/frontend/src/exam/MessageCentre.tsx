import { useState } from 'react'
import type { CandidateMessage } from '../api/types'
import { BellIcon } from '../ui/icons'

const seenKey = (sectionId: number) => `victory.messages.seen.${sectionId}`

function readSeen(sectionId: number): number {
  try {
    return Number(sessionStorage.getItem(seenKey(sectionId)) ?? 0)
  } catch {
    return 0
  }
}

const timeOf = (iso: string) => new Date(iso).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })

/**
 * VE-17: the bell in the candidate header. A new message from the invigilator appears on screen until the candidate
 * presses OK; every message stays listed under the bell. What was read survives a reload of this tab.
 */
export function MessageCentre({ sectionId, messages }: { sectionId: number; messages: CandidateMessage[] }) {
  const [seenId, setSeenId] = useState(() => readSeen(sectionId))
  const [isOpen, setIsOpen] = useState(false)
  const unread = messages.filter((m) => m.id > seenId)
  const newest = unread.at(-1)

  const markRead = () => {
    const lastId = messages.at(-1)?.id ?? seenId
    setSeenId(lastId)
    try {
      sessionStorage.setItem(seenKey(sectionId), String(lastId))
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
        <div className="messages-panel">
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
