import { useState, type FormEvent } from 'react'
import type { Access } from '../access'
import { staffApi, type ApprovalKind, type ApprovalView } from '../api'
import { formatDateTime } from '../format'
import { Badge, ConfirmButton, ErrorBox, Modal, PageHeader } from '../ui'
import { useLoad } from '../useLoad'

const KIND_TEXT: Record<ApprovalKind, string> = {
  DeletePaper: 'Delete paper',
  DeleteExam: 'Delete exam',
  DeleteCandidate: 'Remove candidate',
  ChangeStaffAccount: 'Change staff account',
  CreateAdmin: 'New administrator',
}

/**
 * VE-60 (Victory, 16/10/2026): so that no administrator acts alone, deletions and staff account changes wait here
 * for an administrator other than the one who asked. Administrators see every request and decide the others';
 * managers follow (and may cancel) their own. Decided requests stay as the log.
 */
export function ApprovalsPage({ access }: { access: Access }) {
  const { data: requests, error, reload } = useLoad(staffApi.approvals, 'approvals')
  const [showAll, setShowAll] = useState(false)
  const [rejecting, setRejecting] = useState<ApprovalView | null>(null)
  const [actionError, setActionError] = useState<unknown>(null)
  const pending = (requests ?? []).filter((r) => r.status === 'Pending')
  const shown = showAll ? (requests ?? []) : pending

  const act = async (call: () => Promise<unknown>) => {
    setActionError(null)
    try {
      await call()
    } catch (e) {
      setActionError(e)
    }

    reload()
  }

  return (
    <>
      <PageHeader
        title="Approvals"
        meta={access.isAdmin ? 'Deletions and account changes wait for an administrator other than the one who asked' : 'Your requests to the administrators'}
        actions={
          <div className="st-segmented" role="group" aria-label="Show">
            <button type="button" className={showAll ? undefined : 'is-on'} onClick={() => setShowAll(false)}>
              Waiting ({pending.length})
            </button>
            <button type="button" className={showAll ? 'is-on' : undefined} onClick={() => setShowAll(true)}>
              All
            </button>
          </div>
        }
      />
      <ErrorBox error={error ?? actionError} />
      {requests && shown.length === 0 && <p className="st-empty">{showAll ? 'No requests yet.' : 'Nothing is waiting for approval.'}</p>}
      {shown.length > 0 && (
        <table className="st-table">
          <thead>
            <tr>
              <th>Request</th>
              <th>Asked by</th>
              <th>Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {shown.map((request) => {
              const isOwn = request.requestedById === access.userId
              const canDecide = access.isAdmin && !isOwn && request.status === 'Pending'
              return (
                <tr key={request.id}>
                  <td>
                    <div className="st-faint">
                      #{request.id} · {KIND_TEXT[request.kind] ?? request.kind}
                    </div>
                    <strong>{request.summary}</strong>
                  </td>
                  <td>
                    {request.requestedBy}
                    {isOwn && <span className="st-faint"> (you)</span>}
                    <div className="st-faint">{formatDateTime(request.requestedAt)}</div>
                  </td>
                  <td>
                    <Badge value={request.status} />
                    {request.decidedBy && (
                      <div className="st-faint">
                        {request.decidedBy}, {formatDateTime(request.decidedAt)}
                      </div>
                    )}
                    {request.decisionNote && <div>“{request.decisionNote}”</div>}
                    {request.outcome && <div className="st-faint">{request.outcome}</div>}
                    {request.status === 'Pending' && isOwn && <div className="st-faint">Another administrator must decide.</div>}
                  </td>
                  <td className="st-row-actions">
                    {canDecide && <ConfirmButton className="st-link" label="Approve" confirmLabel="Click again to approve" onConfirm={() => void act(() => staffApi.approve(request.id))} />}
                    {canDecide && (
                      <button type="button" className="st-link st-link--danger" onClick={() => setRejecting(request)}>
                        Reject
                      </button>
                    )}
                    {isOwn && request.status === 'Pending' && (
                      <ConfirmButton className="st-link" label="Cancel request" confirmLabel="Confirm cancel" onConfirm={() => void act(() => staffApi.cancelRequest(request.id))} />
                    )}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      )}
      {rejecting && (
        <RejectRequest
          request={rejecting}
          onClose={() => setRejecting(null)}
          onRejected={() => {
            setRejecting(null)
            reload()
          }}
        />
      )}
    </>
  )
}

function RejectRequest({ request, onClose, onRejected }: { request: ApprovalView; onClose: () => void; onRejected: () => void }) {
  const [note, setNote] = useState('')
  const [error, setError] = useState<unknown>(null)
  const submit = async (event: FormEvent) => {
    event.preventDefault()
    try {
      await staffApi.reject(request.id, note.trim())
      onRejected()
    } catch (e) {
      setError(e)
    }
  }

  return (
    <Modal title="Reject request" onClose={onClose}>
      <form className="st-form" onSubmit={submit}>
        <p>
          <strong>{request.summary}</strong>
          <span className="st-faint"> — asked by {request.requestedBy}</span>
        </p>
        <label className="st-field">
          Reason <span className="st-faint">(shown to {request.requestedBy})</span>
          <textarea value={note} onChange={(e) => setNote(e.target.value)} required maxLength={500} rows={3} />
        </label>
        <ErrorBox error={error} />
        <div className="st-form-actions">
          <button type="submit" className="st-btn st-btn--danger" disabled={note.trim().length === 0}>
            Reject
          </button>
        </div>
      </form>
    </Modal>
  )
}
