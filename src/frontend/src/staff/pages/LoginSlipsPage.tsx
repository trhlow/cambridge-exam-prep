import { staffApi } from '../api'
import { ErrorBox } from '../ui'
import { formatDate } from '../format'
import { useLoad } from '../useLoad'
import { VictoryLogo } from '../../ui/VictoryLogo'

/** Printable login slips, one per candidate (cut along the dashed lines), as the Cambridge centre hands them out. */
export function LoginSlipsPage({ examId }: { examId: number }) {
  const { data: slips, error } = useLoad(() => staffApi.loginSlips(examId), examId)

  if (!slips) {
    return error ? <ErrorBox error={error} /> : <p className="st-empty">Loading…</p>
  }

  return (
    <div className="slips">
      <div className="slips-bar">
        <span>
          {slips.length} login slip{slips.length === 1 ? '' : 's'}
        </span>
        <button type="button" className="st-btn st-btn--primary" onClick={() => window.print()}>
          Print
        </button>
      </div>
      {slips.length === 0 && <p className="st-empty">No candidates are entered for this exam.</p>}
      <div className="slips-grid">
        {slips.map((slip) => (
          <article key={slip.candidateId} className="slip">
            <header>
              <VictoryLogo />
              <div>
                <strong>{slip.examName}</strong>
                <div>
                  {formatDate(slip.scheduledDate)}
                  {slip.room && ` · ${slip.room}`}
                </div>
              </div>
            </header>
            <div className="slip-who">
              {slip.fullName} <span>· {slip.candidateNumber}</span>
            </div>
            <dl>
              <dt>Username</dt>
              <dd>{slip.username}</dd>
              <dt>Password</dt>
              <dd>{slip.password ?? 'ask the administrator'}</dd>
            </dl>
            <p>Wait for the invigilator to give you the PIN.</p>
          </article>
        ))}
      </div>
    </div>
  )
}
