import { useState } from 'react'
import { adminApi, EXAM_STATUSES, staffApi, type ExamDetail, type ExamStatus } from '../api'
import { Link } from '../Link'
import { navigate } from '../router'
import { Badge, ConfirmButton, ErrorBox, PageHeader } from '../ui'
import { formatDate } from '../format'
import { useLoad } from '../useLoad'
import { CandidatesPanel } from './CandidatesPanel'
import { InvigilatorsPanel } from './InvigilatorsPanel'

const STATUS_HELP: Record<ExamStatus, string> = {
  Draft: 'Being prepared. Candidates cannot sign in.',
  Scheduled: 'Ready. Candidates cannot sign in yet.',
  Active: 'Open: candidates can sign in with their slip and the PIN.',
  Closed: 'Finished. Candidates can no longer sign in.',
}

/**
 * The session (Inspera "parent test"): PIN and invigilator password, the sections with their monitoring pages,
 * login slips, and — for administrators — status, candidates and invigilators. Monitoring happens per section.
 */
export function ExamPage({ examId, isAdmin }: { examId: number; isAdmin: boolean }) {
  const { data: exam, error, reload, setData } = useLoad(() => staffApi.exam(examId), examId)
  const [actionError, setActionError] = useState<unknown>(null)

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
    try {
      await adminApi.deleteExam(exam.id)
      navigate('/staff')
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
            <a className="st-btn" href={`/staff/exams/${exam.id}/slips`} target="_blank" rel="noopener noreferrer">
              Print login slips
            </a>
            <Link to={`/staff/exams/${exam.id}/results`} className="st-btn">
              Results
            </Link>
            {isAdmin && (
              <Link to={`/staff/exams/${exam.id}/edit`} className="st-btn">
                Edit
              </Link>
            )}
          </>
        }
      />
      <ErrorBox error={actionError} />

      <div className="st-bento">
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
        </section>

        <section className="st-card">
          <h2 className="st-h2">Status</h2>
          <p>
            <Badge value={exam.status} /> <span className="st-faint">{STATUS_HELP[exam.status]}</span>
          </p>
          {isAdmin && (
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
      <p className="st-faint">Monitor each section on its own page, as in Inspera: candidates are paused, given time or ended there.</p>
      <div className="st-sections">
        {exam.sections.map((section) => (
          <Link key={section.id} to={`/staff/exams/${exam.id}/sections/${section.id}`} className="st-card st-section-card">
            <span className="st-faint">Section {section.orderNo}</span>
            <strong>{section.title}</strong>
            <span>{section.durationMinutes} minutes</span>
            <span className="st-faint">{section.paperTitle}</span>
            <span className="st-section-go">Monitor →</span>
          </Link>
        ))}
      </div>

      {isAdmin && <CandidatesPanel exam={exam} onChanged={reload} />}
      {isAdmin && <InvigilatorsPanel exam={exam} onChanged={(invigilators) => setData({ ...exam, invigilators } satisfies ExamDetail)} />}
      {isAdmin && !exam.hasStarted && (
        <div className="st-danger-zone">
          <ConfirmButton label="Delete exam" confirmLabel="Click again: delete the exam and its candidates" onConfirm={() => void remove()} />
        </div>
      )}
    </>
  )
}
