import { useState, type FormEvent } from 'react'
import { countWords } from '../../exam/paperLogic'
import { adminApi, staffApi, type AttemptMarking, type QuestionMark } from '../api'
import { Link } from '../Link'
import { Badge, ErrorBox, PageHeader } from '../ui'
import { useLoad } from '../useLoad'

/**
 * VE-26: one candidate's section for the teacher. Writing answers are marked here (mark and comment); the
 * automatically marked questions are listed with the candidate's answer and the accepted answers.
 */
export function MarkingPage({ examId, attemptId }: { examId: number; attemptId: number }) {
  const { data: marking, error, setData } = useLoad(() => adminApi.marking(attemptId), attemptId)
  const { data: results } = useLoad(() => staffApi.results(examId), `results-${examId}`)
  const candidate = results?.find((r) => r.sections.some((s) => s.attemptId === attemptId))

  if (!marking) {
    return error ? <ErrorBox error={error} /> : <p className="st-empty">Loading…</p>
  }

  // VE-35: a question the candidate did not choose (B1 Writing Part 2) is shown but not marked.
  const essays = marking.questions.filter((q) => q.interactionType === 'Essay')
  const writing = essays.filter((q) => q.counts !== false)
  const notChosen = essays.filter((q) => q.counts === false)
  const automatic = marking.questions.filter((q) => q.interactionType !== 'Essay')

  return (
    <>
      <PageHeader
        title={candidate ? `${candidate.fullName} · ${marking.sectionTitle}` : marking.sectionTitle}
        meta={
          <>
            <Link to={`/staff/exams/${examId}/results`}>Results</Link>
            {candidate && <> · candidate {candidate.candidateNumber}</>}
            {marking.paperTitle && <> · {marking.paperTitle}</>}
          </>
        }
      />
      <div className="st-bento">
        <section className="st-card">
          <h2 className="st-h2">Score</h2>
          <p className="st-score">
            {marking.rawScore ?? '–'} / {marking.maxScore ?? '–'}
          </p>
          <Badge value={marking.gradingStatus === 'Final' ? 'Final' : 'NeedsManualMarking'} />
        </section>
        <section className="st-card">
          <h2 className="st-h2">Writing</h2>
          <p className="st-faint">
            {writing.filter((q) => q.score !== null).length} of {writing.length} Writing question(s) marked. The score becomes final when every Writing question has a mark.
          </p>
        </section>
      </div>

      {writing.map((question) => (
        <WritingMark key={question.number} attemptId={attemptId} question={question} onSaved={setData} />
      ))}

      {notChosen.map((question) => (
        <section key={question.number} className="st-panel st-card st-writing">
          <div className="st-panel-head">
            <h2 className="st-h2">
              Question {question.number} · Part {question.partNo}
            </h2>
            <span className="st-faint">Not chosen by the candidate ({question.selection}) — not marked</span>
          </div>
          {question.response && <div className="st-essay">{question.response}</div>}
        </section>
      ))}

      {automatic.length > 0 && (
        <>
          <h2 className="st-h2">Marked automatically</h2>
          <table className="st-table">
            <thead>
              <tr>
                <th className="st-num">Question</th>
                <th>Part</th>
                <th>Answer</th>
                <th>Accepted</th>
                <th>Mark</th>
              </tr>
            </thead>
            <tbody>
              {automatic.map((q) => (
                <tr key={q.number}>
                  <td className="st-num">{q.number}</td>
                  <td>{q.partNo}</td>
                  <td className="st-mono">{q.response || <span className="st-faint">no answer</span>}</td>
                  <td className="st-mono">{q.acceptedAnswers.join(' / ')}</td>
                  <td>{q.isCorrect ? <span className="st-mark st-mark--right">✓ {q.score}</span> : <span className="st-mark st-mark--wrong">✗ 0</span>}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      )}
    </>
  )
}

function WritingMark({ attemptId, question, onSaved }: { attemptId: number; question: QuestionMark; onSaved: (marking: AttemptMarking) => void }) {
  const [score, setScore] = useState(question.score === null ? '' : String(question.score))
  const [comment, setComment] = useState(question.comment ?? '')
  const [error, setError] = useState<unknown>(null)
  const [savedAt, setSavedAt] = useState<string | null>(null)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)
    try {
      onSaved(await adminApi.markWriting(attemptId, question.number, Number(score), comment))
      setSavedAt(new Date().toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' }))
    } catch (e) {
      setError(e)
    }
  }

  return (
    <section className="st-panel st-card st-writing">
      <div className="st-panel-head">
        <h2 className="st-h2">
          Question {question.number} · Part {question.partNo}
        </h2>
        <span className="st-faint">
          {question.selection && <>{question.selection === 'Yes' ? 'Chosen by the candidate' : 'Left undecided, marked because it was written'} · </>}
          {question.response ? `${countWords(question.response)} words` : 'nothing written'}
        </span>
      </div>
      <div className="st-essay">{question.response || <span className="st-faint">The candidate wrote nothing.</span>}</div>
      <form className="st-mark-form" onSubmit={submit}>
        <label className="st-field">
          Mark (0–{question.points})
          <input type="number" min={0} max={question.points} step={0.5} value={score} onChange={(e) => setScore(e.target.value)} required />
        </label>
        <label className="st-field st-grow">
          Comment
          <input value={comment} onChange={(e) => setComment(e.target.value)} maxLength={2000} placeholder="Optional" />
        </label>
        <button type="submit" className="st-btn st-btn--primary">
          {question.score === null ? 'Save mark' : 'Update mark'}
        </button>
        {savedAt && <span className="st-faint">Saved {savedAt}</span>}
      </form>
      <ErrorBox error={error} />
    </section>
  )
}
