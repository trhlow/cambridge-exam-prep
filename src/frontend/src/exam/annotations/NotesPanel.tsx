import type { Note } from './store'

type Props = {
  notes: Note[]
  /** The note just created gets the cursor. */
  focusId: string | null
  onChange: (id: string, text: string) => void
  onDelete: (id: string) => void
  onClose: () => void
}

/** VE-19: the Notes panel on the right (pen icon). Notes are private and only exist during the test. */
export function NotesPanel({ notes, focusId, onChange, onDelete, onClose }: Props) {
  return (
    <aside className="notes-panel" aria-label="Notes">
      <div className="notes-head">
        <h2>Notes</h2>
        <button type="button" className="notes-close" aria-label="Close notes" onClick={onClose}>
          ✕
        </button>
      </div>
      {notes.length === 0 ? (
        <p className="notes-empty">Your private notes will show here. Select text to create notes and highlights.</p>
      ) : (
        <ul className="notes-list">
          {notes.map((note) => (
            <li key={note.id}>
              <blockquote>{note.quote}</blockquote>
              <textarea
                aria-label={`Note on “${note.quote}”`}
                value={note.text}
                onChange={(e) => onChange(note.id, e.target.value)}
                rows={3}
                spellCheck={false}
                autoFocus={note.id === focusId}
              />
              <button type="button" className="notes-delete" onClick={() => onDelete(note.id)}>
                Delete note
              </button>
            </li>
          ))}
        </ul>
      )}
    </aside>
  )
}
