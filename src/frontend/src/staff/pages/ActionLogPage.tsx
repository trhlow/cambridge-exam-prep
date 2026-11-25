import { staffApi } from '../api'
import { actionDetail, actionLabel, actionLogCsv, logTime } from '../actionLog'
import { Link } from '../Link'
import { downloadCsv } from '../resultsCsv'
import { ErrorBox, PageHeader } from '../ui'
import { useLoad } from '../useLoad'

/**
 * Everything the invigilators did in the session, oldest first: pauses, extra time, end times, rewinds, messages.
 * Cambridge asks the centre to record every incident; this is that record, downloadable for the report.
 */
export function ActionLogPage({ examId }: { examId: number }) {
  const { data: exam } = useLoad(() => staffApi.exam(examId), `exam-${examId}`)
  const { data: rows, error } = useLoad(() => staffApi.actionLog(examId), `log-${examId}`)
  const download = () => rows && downloadCsv(actionLogCsv(rows), 'invigilator-log', exam?.name ?? `exam-${examId}`)

  return (
    <>
      <PageHeader
        title="Invigilator log"
        meta={<Link to={`/staff/exams/${examId}`}>{exam?.name ?? 'Exam'}</Link>}
        actions={
          <button type="button" className="st-btn" onClick={download} disabled={!rows || rows.length === 0}>
            Download for Excel (.csv)
          </button>
        }
      />
      <ErrorBox error={error} />
      {rows && rows.length === 0 && <p className="st-empty">No invigilator actions yet.</p>}
      {rows && rows.length > 0 && (
        <table className="st-table">
          <thead>
            <tr>
              <th>Time</th>
              <th>Action</th>
              <th>Candidate</th>
              <th>Section</th>
              <th>Reason</th>
              <th>By</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.id}>
                <td className="st-mono">{logTime(row.at)}</td>
                <td>
                  <strong>{actionLabel(row)}</strong>
                  {actionDetail(row) && <div className="st-faint">{actionDetail(row)}</div>}
                </td>
                <td>
                  {row.candidate ?? <span className="st-faint">{row.message !== null && !row.recipients ? 'Whole room' : '–'}</span>}
                  {row.candidateNumber && <div className="st-faint st-mono">{row.candidateNumber}</div>}
                </td>
                <td>{row.section ?? '–'}</td>
                <td>{row.reason ?? '–'}</td>
                <td>{row.by}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  )
}
