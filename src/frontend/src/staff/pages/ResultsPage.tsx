import { useState } from 'react'
import { staffApi, type CandidateResult } from '../api'
import { messageOf } from '../format'
import { Link } from '../Link'
import { Badge, ErrorBox, PageHeader, VersionBadge } from '../ui'
import { GRADING_TEXT, downloadCsv, resultsCsv } from '../resultsCsv'
import { useLoad } from '../useLoad'

/**
 * Scores per candidate and section (VE-11 auto-marking), for administrators. Writing is dealt at random to the
 * session's examiners, who mark it by code (VE-73); this table shows which code and examiner each candidate has.
 */
export function ResultsPage({ examId }: { examId: number }) {
  const { data: exam } = useLoad(() => staffApi.exam(examId), `exam-${examId}`)
  const { data: results, error, reload } = useLoad(() => staffApi.results(examId), examId)
  const sections = results?.[0]?.sections ?? []
  const scaled = (results ?? []).some((r) => r.skills)
  // B11: Writing finished but held by nobody who can mark it: never dealt (no examiner on the session), or dealt to one
  // since switched off, no longer an examiner or off the session (the server flags those; the deal waits for an eligible one).
  const undealt = (results ?? []).filter(
    (c) => (!c.examiner || c.examinerUnavailable) && c.sections.some((s) => s.status === 'Submitted' && s.gradingStatus === 'NeedsManualMarking'),
  ).length
  const download = () => results && downloadCsv(resultsCsv(results), 'results', exam?.name ?? `exam-${examId}`)

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
      {scaled && (
        <p className="st-faint">
          The Cambridge English Scale scores follow Cambridge&apos;s conversion tables for practice tests (exams from 2020): an indication of readiness, not
          the score of a live exam. The overall score and grade appear once Speaking is entered and every skill is marked.
        </p>
      )}
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
              {scaled && <th>Speaking</th>}
              {scaled && <th>Cambridge English Scale</th>}
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
                {scaled && (
                  <td>
                    <SpeakingInput examId={examId} candidate={candidate} onSaved={reload} />
                  </td>
                )}
                {scaled && (
                  <td>
                    <ScaleCell candidate={candidate} />
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  )
}

/** Each skill's scale score (– not marked yet, < below the minimum reported), then the overall score and grade. */
function ScaleCell({ candidate }: { candidate: CandidateResult }) {
  const skills = candidate.skills ?? []
  return (
    <>
      <div className="st-faint">
        {skills.map((s) => `${s.skill[0]} ${s.scale ?? (s.rawScore === null ? '–' : '<')}`).join(' · ')}
      </div>
      {candidate.overall != null && (
        <div className="st-score">
          {candidate.overall}
          {candidate.grade ? ` · ${candidate.grade} (${candidate.cefr})` : ' · below the level'}
        </div>
      )}
    </>
  )
}

/** The face-to-face Speaking marks, saved when the field is left or Enter is pressed; empty clears them. */
function SpeakingInput({ examId, candidate, onSaved }: { examId: number; candidate: CandidateResult; onSaved: () => void }) {
  const speaking = candidate.skills?.find((s) => s.skill === 'Speaking')
  const saved = speaking?.rawScore == null ? '' : String(speaking.rawScore)
  const [value, setValue] = useState(saved)
  const [error, setError] = useState<string | null>(null)
  const save = () => {
    if (value.trim() === saved) {
      return
    }

    staffApi.setSpeaking(examId, candidate.candidateId, value.trim() === '' ? null : Number(value)).then(
      () => {
        setError(null)
        onSaved()
      },
      (e: unknown) => setError(messageOf(e)),
    )
  }

  return (
    <>
      <input
        type="number"
        className="st-speaking"
        min={0}
        max={speaking?.maxScore ?? undefined}
        step={0.5}
        value={value}
        aria-label={`Speaking marks of ${candidate.fullName}`}
        onChange={(e) => setValue(e.target.value)}
        onBlur={save}
        onKeyDown={(e) => e.key === 'Enter' && save()}
      />
      {speaking?.maxScore != null && <span className="st-faint"> / {speaking.maxScore}</span>}
      {error && <div className="st-error st-speaking-error">{error}</div>}
    </>
  )
}
