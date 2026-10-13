import { staffApi } from '../api'
import { Link } from '../Link'
import { Badge, ErrorBox, PageHeader } from '../ui'
import { resultsCsv } from '../resultsCsv'
import { useLoad } from '../useLoad'

const GRADING_TEXT: Record<string, string> = {
  NotGraded: 'Not marked yet',
  AutoGraded: 'Marked',
  NeedsManualMarking: 'Writing to mark',
  Final: 'Final',
}

/** Scores per candidate and section (VE-11 auto-marking). Writing is marked by a teacher later (VE-26). */
export function ResultsPage({ examId, isAdmin }: { examId: number; isAdmin: boolean }) {
  const { data: exam } = useLoad(() => staffApi.exam(examId), `exam-${examId}`)
  const { data: results, error } = useLoad(() => staffApi.results(examId), examId)
  const sections = results?.[0]?.sections ?? []
  const download = () => {
    if (!results) {
      return
    }

    const url = URL.createObjectURL(new Blob([resultsCsv(results)], { type: 'text/csv;charset=utf-8' }))
    const link = document.createElement('a')
    link.href = url
    link.download = `results-${(exam?.name ?? `exam-${examId}`).replace(/[^\p{L}\p{N}]+/gu, '-')}.csv`
    link.click()
    URL.revokeObjectURL(url)
  }

  return (
    <>
      <PageHeader
        title="Results"
        meta={<Link to={`/staff/exams/${examId}`}>{exam?.name ?? 'Exam'}</Link>}
        actions={
          <button type="button" className="st-btn" onClick={download} disabled={!results || results.length === 0}>
            Download for Excel (.csv)
          </button>
        }
      />
      <p className="st-faint">Reading and Listening are marked automatically when a section ends. Writing is marked by a teacher (Mark Writing); the score is final once every Writing question has a mark.</p>
      <ErrorBox error={error} />
      {results && results.length === 0 && <p className="st-empty">No candidates.</p>}
      {results && results.length > 0 && (
        <table className="st-table">
          <thead>
            <tr>
              <th>Candidate</th>
              {sections.map((s) => (
                <th key={s.sectionId}>{s.title}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {results.map((candidate) => (
              <tr key={candidate.candidateId}>
                <td>
                  <strong>{candidate.fullName}</strong>
                  <div className="st-faint st-mono">{candidate.candidateNumber}</div>
                </td>
                {candidate.sections.map((s) => (
                  <td key={s.sectionId}>
                    <Badge value={s.status} />
                    {s.rawScore !== null && (
                      <div className="st-score">
                        {s.rawScore} / {s.maxScore}
                      </div>
                    )}
                    {s.status === 'Submitted' && <div className="st-faint">{GRADING_TEXT[s.gradingStatus] ?? s.gradingStatus}</div>}
                    {isAdmin && s.status === 'Submitted' && s.attemptId !== null && (
                      <Link to={`/staff/exams/${examId}/marking/${s.attemptId}`} className="st-link">
                        {s.gradingStatus === 'NeedsManualMarking' ? 'Mark Writing' : 'View marking'}
                      </Link>
                    )}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  )
}
