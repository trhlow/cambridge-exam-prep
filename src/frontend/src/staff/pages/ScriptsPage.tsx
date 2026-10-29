import { adminApi } from '../api'
import { Link } from '../Link'
import { Badge, ErrorBox, PageHeader } from '../ui'
import { formatDate } from '../format'
import { useLoad } from '../useLoad'

/**
 * VE-73 (Victory 29/10): the examiner's start page. The Writing scripts of the sessions they mark are dealt at random
 * and evenly among the session's examiners; each script is known by its code, never by the candidate's name, and the
 * examiner sees only the Writing.
 */
export function ScriptsPage() {
  const { data: scripts, error } = useLoad(adminApi.scripts, 'scripts')
  const toMark = scripts?.filter((s) => s.gradingStatus !== 'Final').length ?? 0

  return (
    <>
      <PageHeader title="Marking" meta={scripts ? `${scripts.length} script${scripts.length === 1 ? '' : 's'} dealt to you · ${toMark} to mark` : 'Writing scripts dealt to you'} />
      <p className="st-faint">Scripts are dealt at random and evenly among a session's examiners once a candidate has finished the section with Writing. You see each one by its code only.</p>
      <ErrorBox error={error} />
      {scripts && scripts.length === 0 && <p className="st-empty">No scripts yet. They appear here when candidates finish the Writing of a session you mark.</p>}
      {scripts && scripts.length > 0 && (
        <table className="st-table">
          <thead>
            <tr>
              <th>Script</th>
              <th>Session</th>
              <th>Section</th>
              <th>Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {scripts.map((script) => (
              <tr key={script.attemptId}>
                <td className="st-mono st-strong-link">{script.code}</td>
                <td>
                  {script.examName}
                  <div className="st-faint">{formatDate(script.scheduledDate)}</div>
                </td>
                <td>{script.section}</td>
                <td>
                  <Badge value={script.gradingStatus === 'Final' ? 'Final' : 'NeedsManualMarking'} />
                </td>
                <td>
                  <Link to={`/staff/exams/${script.examId}/marking/${script.attemptId}`} className="st-link">
                    {script.gradingStatus === 'Final' ? 'View marks' : 'Mark Writing'}
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  )
}
