import { useState } from 'react'
import type { Access } from '../access'
import { adminApi, EXAM_STATUSES, staffApi, type ApprovalView, type ExamDetail, type ExamStatus } from '../api'
import { Link } from '../Link'
import { Badge, ConfirmButton, ErrorBox, PageHeader, RequestSent } from '../ui'
import { formatDate } from '../format'
import { useLoad } from '../useLoad'
import { CandidatesPanel } from './CandidatesPanel'
import { StaffAssignmentPanel } from './StaffAssignmentPanel'

const STATUS_HELP: Record<ExamStatus, string> = {
  Draft: 'Being prepared. Candidates cannot sign in.',
  Scheduled: 'Ready. Candidates cannot sign in yet.',
  Active: 'Open: candidates can sign in with their slip and the PIN.',
  Closed: 'Finished. Candidates can no longer sign in.',
}

/**
 * The session (Inspera "parent test"): PIN and invigilator password, the sections with their monitoring pages and
 * login slips for the people who run the room; status, invigilators and examiners for managers of exams; the
 * candidates for managers of candidates; results for examiners and administrators. Monitoring happens per section.
 */
export function ExamPage({ examId, access }: { examId: number; access: Access }) {
  const { data: exam, error, reload, setData } = useLoad(() => staffApi.exam(examId), examId)
  const manages = access.worksIn('Exams')
  const { data: staff } = useLoad(() => (manages ? adminApi.assignableStaff() : Promise.resolve(null)), `staff-${manages}`)
  const [actionError, setActionError] = useState<unknown>(null)
  const [sent, setSent] = useState<ApprovalView | null>(null)

  if (!exam) {
    return error ? <ErrorBox error={error} /> : <p className="st-empty">Loading…</p>
  }

  const setStatus = async (status: ExamStatus) => {
    setActionError(null)
    try {
      setData(await adminApi.setStatus(exam.id, status))
    } catch (e) {
      setActionError(e)
    }
  }
  const remove = async () => {
    setActionError(null)
    try {
      setSent(await adminApi.deleteExam(exam.id))
    } catch (e) {
      setActionError(e)
    }
  }

  return (
    <>
      <PageHeader
        title={exam.name}
        meta={
          <>
            <Link to="/staff">Exams</Link> · {exam.examType} · {formatDate(exam.scheduledDate)}
            {exam.room && ` · ${exam.room}`}
          </>
        }
        actions={
          <>
            {access.printsSlips && (
              <a className="st-btn" href={`/staff/exams/${exam.id}/slips`} target="_blank" rel="noopener noreferrer">
                Print login slips
              </a>
            )}
            {access.seesResults && (
              <Link to={`/staff/exams/${exam.id}/results`} className="st-btn">
                Results
              </Link>
            )}
            {manages && (
              <Link to={`/staff/exams/${exam.id}/edit`} className="st-btn">
                Edit
              </Link>
            )}
          </>
        }
      />
      <ErrorBox error={actionError} />
      {sent && <RequestSent request={sent} onDismiss={() => setSent(null)} />}

      <div className="st-bento">
        {exam.sessionPin && <RoomCard exam={exam} />}

        <section className="st-card">
          <h2 className="st-h2">Status</h2>
          <p>
            <Badge value={exam.status} /> <span className="st-faint">{STATUS_HELP[exam.status]}</span>
          </p>
          {manages && (
            <div className="st-segmented" role="group" aria-label="Change status">
              {EXAM_STATUSES.map((status) => (
                <button key={status} type="button" className={status === exam.status ? 'is-on' : undefined} onClick={() => void setStatus(status)} disabled={status === exam.status}>
                  {status}
                </button>
              ))}
            </div>
          )}
        </section>
      </div>

      <h2 className="st-h2">Sections</h2>
      {access.monitors && <p className="st-faint">Monitor each section on its own page, as in Inspera: candidates are paused, given time or ended there.</p>}
      <div className="st-sections">
        {exam.sections.map((section) => {
          const body = (
            <>
              <span className="st-faint">Section {section.orderNo}</span>
              <strong>{section.title}</strong>
              <span>{section.durationMinutes} minutes</span>
              <span className="st-faint">{section.paperTitle}</span>
            </>
          )
          return access.monitors ? (
            <Link key={section.id} to={`/staff/exams/${exam.id}/sections/${section.id}`} className="st-card st-section-card">
              {body}
              <span className="st-section-go">Monitor →</span>
            </Link>
          ) : (
            <div key={section.id} className="st-card st-section-card">
              {body}
            </div>
          )
        })}
      </div>

      {access.seesCandidates && <CandidatesPanel exam={exam} canEdit={access.worksIn('Candidates')} onChanged={reload} />}
      {manages && <StaffAssignmentPanel exam={exam} role="Invigilator" staff={staff} onChanged={(invigilators) => setData({ ...exam, invigilators } satisfies ExamDetail)} />}
      {manages && <StaffAssignmentPanel exam={exam} role="Examiner" staff={staff} onChanged={(examiners) => setData({ ...exam, examiners } satisfies ExamDetail)} />}
      {manages && !exam.hasStarted && (
        <div className="st-danger-zone">
          <ConfirmButton label="Request deletion" confirmLabel="Click again: ask another administrator to delete this exam" onConfirm={() => void remove()} />
        </div>
      )}
    </>
  )
}

/** What the invigilator reads out and types; the server leaves both out for examiners, who are not in the room. */
function RoomCard({ exam }: { exam: ExamDetail }) {
  return (
    <section className="st-card st-room">
      <h2 className="st-h2">Exam room</h2>
      <dl className="st-keys">
        <div>
          <dt>Session PIN</dt>
          <dd className="st-mono st-big">{exam.sessionPin}</dd>
        </div>
        <div>
          <dt>Invigilator password</dt>
          <dd className="st-mono st-big">{exam.invigilatorPassword}</dd>
        </div>
      </dl>
      <p className="st-faint">Read the PIN out once every candidate has logged in. The invigilator password lets a candidate leave the locked browser or move to another computer.</p>
      <p>
        Safe Exam Browser:{' '}
        {exam.sebConfigKey ? <strong>required</strong> : <span className="st-faint">not required — any browser can sign in</span>}
      </p>
    </section>
  )
}
