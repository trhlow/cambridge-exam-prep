import { staffApi } from '../api'
import { Link } from '../Link'
import { Badge, ErrorBox, PageHeader, VersionBadge } from '../ui'
import { GRADING_TEXT, resultsCsv } from '../resultsCsv'
import { useLoad } from '../useLoad'

/**
 * Scores per candidate and section (VE-11 auto-marking), for administrators. Writing is dealt at random to the
 * session's examiners, who mark it by code (VE-73); this table shows which code and examiner each candidate has.
 */
export function ResultsPage({ examId }: { examId: number }) {
  const { data: exam } = useLoad(() => staffApi.exam(examId), `exam-${examId}`)
  const { data: results, error } = useLoad(() => staffApi.results(examId), examId)
  const sections = results?.[0]?.sections ?? []
  // B11: Writing finished but held by nobody who can mark it: never dealt (no examiner on the session), or dealt to one
  // since switched off, no longer an examiner or off the session (the server flags those; the deal waits for an eligible one).
  const undealt = (results ?? []).filter(
    (c) => (!c.examiner || c.examinerUnavailable) && c.sections.some((s) => s.status === 'Submitted' && s.gradingStatus === 'NeedsManualMarking'),
  ).length
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
      <p className="st-faint">Reading and Listening are marked automatically when a section ends. Writing is dealt at random and evenly to the session's examiners, who see only its code; the score is final once every Writing question has a mark.</p>
      <ErrorBox error={error} />
      {undealt > 0 && (
        <p className="st-error" role="alert">
          {undealt} Writing script{undealt === 1 ? ' has' : 's have'} no examiner who can mark them. Assign an active examiner to the session and they will be dealt.
        </p>
      )}
      {results && results.length === 0 && <p className="st-empty">No candidates.</p>}
      {results && results.length > 0 && (
        <table className="st-table">
          <thead>
            <tr>
              <th>Candidate</th>
              <th>Writing script</th>
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
                <td>
                  {candidate.markingCode ? <span className="st-mono">{candidate.markingCode}</span> : <span className="st-faint">not dealt yet</span>}
                  {candidate.examiner && (
                    <div className="st-faint">
                      {candidate.examiner}
                      {candidate.examinerUnavailable && candidate.sections.some((s) => s.gradingStatus === 'NeedsManualMarking') && ' (can no longer mark)'}
                    </div>
                  )}
                </td>
                {candidate.sections.map((s) => (
                  <td key={s.sectionId}>
                    <Badge value={s.status} />
                    {s.paperVersion ? <VersionBadge version={s.paperVersion} /> : null}
                    {s.rawScore !== null && (
                      <div className="st-score">
                        {s.rawScore} / {s.maxScore}
                      </div>
                    )}
                    {s.status === 'Submitted' && <div className="st-faint">{GRADING_TEXT[s.gradingStatus] ?? s.gradingStatus}</div>}
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
