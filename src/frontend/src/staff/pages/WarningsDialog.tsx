import { useState } from 'react'
import { staffApi, type WarningType, type WarningView } from '../api'
import { formatTime } from '../format'
import { ErrorBox, Modal } from '../ui'
import { useLoad } from '../useLoad'

const TYPE_TEXT: Record<WarningType, string> = {
  InvalidSebKey: 'Not in Safe Exam Browser',
  LockdownBrowserExited: 'Left the locked browser',
  ConnectionLost: 'Connection lost',
  ClockSkew: 'Computer clock wrong',
  StartedEarly: 'Started early',
}

/**
 * VE-29: the details behind the warning triangle (Inspera: click the item in the Warnings column). Resolving keeps
 * the warning in the log but takes it off the candidate's count.
 */
export function WarningsDialog(props: { examId: number; sectionId: number; candidateId: number | null; title: string; onClose: () => void; onChanged: () => void }) {
  const { examId, sectionId, candidateId, title, onClose, onChanged } = props
  const { data, error, setData } = useLoad(() => staffApi.warnings(examId, sectionId), `warnings-${sectionId}`)
  const [actionError, setActionError] = useState<unknown>(null)
  const warnings = (data ?? []).filter((w) => candidateId === null || w.candidateId === candidateId)

  const resolve = async (warning: WarningView) => {
    setActionError(null)
    try {
      const resolved = await staffApi.resolveWarning(examId, sectionId, warning.id)
      setData((data ?? []).map((w) => (w.id === resolved.id ? resolved : w)))
      onChanged()
    } catch (e) {
      setActionError(e)
    }
  }

  return (
    <Modal title={title} onClose={onClose}>
      <ErrorBox error={error ?? actionError} />
      {data && warnings.length === 0 && <p className="st-empty">No warnings.</p>}
      {warnings.length > 0 && (
        <table className="st-table">
          <thead>
            <tr>
              <th>Time</th>
              {candidateId === null && <th>Candidate</th>}
              <th>Warning</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {warnings.map((w) => (
              <tr key={w.id} className={w.resolvedAt ? 'is-muted' : undefined}>
                <td className="st-nowrap">{formatTime(w.detectedAt)}</td>
                {candidateId === null && <td>{w.fullName}</td>}
                <td>
                  <strong>{TYPE_TEXT[w.type] ?? w.type}</strong>
                  {w.detail && <div className="st-faint">{w.detail}</div>}
                  {w.resolvedAt && (
                    <div className="st-faint">
                      Resolved {formatTime(w.resolvedAt)}
                      {w.resolvedBy && ` by ${w.resolvedBy}`}
                    </div>
                  )}
                </td>
                <td className="st-row-actions">
                  {!w.resolvedAt && (
                    <button type="button" className="st-link" onClick={() => void resolve(w)}>
                      Resolve
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </Modal>
  )
}
