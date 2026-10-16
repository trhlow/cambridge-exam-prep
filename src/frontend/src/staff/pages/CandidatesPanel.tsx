import { useState, type FormEvent } from 'react'
import { adminApi, type ApprovalView, type CandidateView, type ExamDetail, type LoginSlip } from '../api'
import { parseCandidateRows } from '../candidateRows'
import { ConfirmButton, ErrorBox, Modal, RequestSent } from '../ui'
import { useLoad } from '../useLoad'

/**
 * VE-22: the candidates entered for the session. Rows are pasted from Excel (or typed one per line); the server
 * gives each a Cambridge username and a password, printed on the login slips. Managers of exams only read the list;
 * removing a candidate waits for a second administrator (VE-60).
 */
export function CandidatesPanel({ exam, canEdit, onChanged }: { exam: ExamDetail; canEdit: boolean; onChanged: () => void }) {
  const { data: candidates, error, reload } = useLoad(() => adminApi.candidates(exam.id), exam.id)
  const [isAdding, setIsAdding] = useState(false)
  const [editing, setEditing] = useState<CandidateView | null>(null)
  const [newSlip, setNewSlip] = useState<LoginSlip | null>(null)
  const [actionError, setActionError] = useState<unknown>(null)
  const [sent, setSent] = useState<ApprovalView | null>(null)

  const changed = () => {
    reload()
    onChanged()
  }
  const resetPassword = async (candidate: CandidateView) => {
    setActionError(null)
    try {
      setNewSlip(await adminApi.resetCandidatePassword(candidate.id))
    } catch (e) {
      setActionError(e)
    }
  }
  const remove = async (candidate: CandidateView) => {
    setActionError(null)
    try {
      setSent(await adminApi.deleteCandidate(candidate.id))
    } catch (e) {
      setActionError(e)
    }
  }

  return (
    <section className="st-panel">
      <div className="st-panel-head">
        <h2 className="st-h2">Candidates {candidates && <span className="st-faint">({candidates.length})</span>}</h2>
        {canEdit && (
          <button type="button" className="st-btn st-btn--primary" onClick={() => setIsAdding(true)}>
            Add candidates
          </button>
        )}
      </div>
      <ErrorBox error={error ?? actionError} />
      {sent && <RequestSent request={sent} onDismiss={() => setSent(null)} />}
      {candidates && candidates.length === 0 && <p className="st-empty">No candidates yet. Paste the entry list from Excel.</p>}
      {candidates && candidates.length > 0 && (
        <table className="st-table">
          <thead>
            <tr>
              <th>Number</th>
              <th>Family name</th>
              <th>Given name</th>
              <th>Username</th>
              <th className="st-num">Extra time</th>
              <th>Seat</th>
              {canEdit && <th />}
            </tr>
          </thead>
          <tbody>
            {candidates.map((c) => (
              <tr key={c.id}>
                <td className="st-mono">{c.candidateNumber}</td>
                <td>{c.lastName}</td>
                <td>{c.firstName}</td>
                <td className="st-mono">{c.username}</td>
                <td className="st-num">{c.plannedExtraPercent > 0 ? `${c.plannedExtraPercent} %` : '–'}</td>
                <td>{c.seatNo ?? '–'}</td>
                {canEdit && (
                  <td className="st-row-actions">
                    <button type="button" className="st-link" onClick={() => setEditing(c)}>
                      Edit
                    </button>
                    <ConfirmButton className="st-link" label="New password" confirmLabel="Confirm new password" onConfirm={() => void resetPassword(c)} />
                    {!c.hasStarted && (
                      <ConfirmButton className="st-link st-link--danger" label="Request removal" confirmLabel="Ask an administrator to remove" onConfirm={() => void remove(c)} />
                    )}
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {isAdding && (
        <AddCandidates
          examId={exam.id}
          onClose={() => setIsAdding(false)}
          onAdded={() => {
            setIsAdding(false)
            changed()
          }}
        />
      )}
      {newSlip && (
        <Modal title="New password" onClose={() => setNewSlip(null)}>
          <p className="st-faint">
            Give {newSlip.fullName} ({newSlip.candidateNumber}) this login. The old password no longer works and any computer they were signed in on has been signed out.
          </p>
          <dl className="st-keys">
            <div>
              <dt>Username</dt>
              <dd className="st-mono st-big">{newSlip.username}</dd>
            </div>
            <div>
              <dt>Password</dt>
              <dd className="st-mono st-big">{newSlip.password}</dd>
            </div>
          </dl>
          <p className="st-faint">It is also on the login slips page from now on.</p>
        </Modal>
      )}
      {editing && (
        <EditCandidate
          candidate={editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null)
            changed()
          }}
        />
      )}
    </section>
  )
}

function AddCandidates({ examId, onClose, onAdded }: { examId: number; onClose: () => void; onAdded: () => void }) {
  const [text, setText] = useState('')
  const [error, setError] = useState<unknown>(null)
  const parsed = parseCandidateRows(text)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)
    try {
      await adminApi.addCandidates(examId, parsed.rows)
      onAdded()
    } catch (e) {
      setError(e)
    }
  }

  return (
    <Modal title="Add candidates" onClose={onClose}>
      <form onSubmit={submit} className="st-form">
        <p className="st-faint">
          Copy the rows from Excel and paste them below: <strong>candidate number, family name, given name</strong>, and optionally extra time (%) and seat. A header row is ignored.
        </p>
        <textarea className="st-paste" value={text} onChange={(e) => setText(e.target.value)} rows={10} placeholder={'0101\tNguyễn\tAn\n0102\tLê\tThị Hoa\t25'} aria-label="Candidate rows" />
        {parsed.errors.length > 0 && (
          <ul className="st-error">
            {parsed.errors.map((e) => (
              <li key={e}>{e}</li>
            ))}
          </ul>
        )}
        <ErrorBox error={error} />
        <div className="st-form-actions">
          <span className="st-faint">{parsed.rows.length} candidate(s) ready</span>
          <button type="submit" className="st-btn st-btn--primary" disabled={parsed.rows.length === 0 || parsed.errors.length > 0}>
            Add {parsed.rows.length || ''}
          </button>
        </div>
      </form>
    </Modal>
  )
}

function EditCandidate({ candidate, onClose, onSaved }: { candidate: CandidateView; onClose: () => void; onSaved: () => void }) {
  const [form, setForm] = useState({ firstName: candidate.firstName, lastName: candidate.lastName, plannedExtraPercent: candidate.plannedExtraPercent, seatNo: candidate.seatNo ?? '' })
  const [error, setError] = useState<unknown>(null)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    try {
      await adminApi.updateCandidate(candidate.id, { ...form, seatNo: form.seatNo || null, dateOfBirth: candidate.dateOfBirth })
      onSaved()
    } catch (e) {
      setError(e)
    }
  }

  return (
    <Modal title={`Candidate ${candidate.candidateNumber}`} onClose={onClose}>
      <form onSubmit={submit} className="st-form">
        <div className="st-grid-2">
          <label className="st-field">
            Family name
            <input value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} required />
          </label>
          <label className="st-field">
            Given name
            <input value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} required />
          </label>
          <label className="st-field">
            Extra time (special arrangement, %)
            <input type="number" min={0} max={100} value={form.plannedExtraPercent} onChange={(e) => setForm({ ...form, plannedExtraPercent: Number(e.target.value) })} />
          </label>
          <label className="st-field">
            Seat
            <input value={form.seatNo} onChange={(e) => setForm({ ...form, seatNo: e.target.value })} maxLength={16} />
          </label>
        </div>
        <p className="st-faint">The username stays {candidate.username}, as printed on the slip.</p>
        <ErrorBox error={error} />
        <div className="st-form-actions">
          <button type="submit" className="st-btn st-btn--primary">
            Save
          </button>
        </div>
      </form>
    </Modal>
  )
}
