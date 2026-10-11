import type { Answers, Paper } from '../api/types'
import { answeredCount, isAnswered, questionsOf } from './paperLogic'

type Props = {
  paper: Paper
  answers: Answers
  onBack: () => void
}

/**
 * Where the ✓ tile leads: how many questions are answered per part and which are still empty.
 * Submitting (and the server clock that submits automatically) comes with VE-8.
 */
export function ReviewPage({ paper, answers, onBack }: Props) {
  return (
    <div className="review">
      <h2>{paper.title}: check your answers</h2>
      <table className="review-table">
        <thead>
          <tr>
            <th scope="col">Part</th>
            <th scope="col">Answered</th>
            <th scope="col">Not answered</th>
          </tr>
        </thead>
        <tbody>
          {paper.parts.map((part) => {
            const questions = questionsOf(part)
            const empty = questions.filter((q) => !isAnswered(answers, q.number)).map((q) => q.number)
            return (
              <tr key={part.partNo}>
                <td>Part {part.partNo}</td>
                <td>
                  {answeredCount(part, answers)} of {questions.length}
                </td>
                <td>{empty.length === 0 ? '—' : empty.join(', ')}</td>
              </tr>
            )
          })}
        </tbody>
      </table>
      <div className="review-actions">
        <button type="button" className="btn btn--secondary" onClick={onBack}>
          Back to the test
        </button>
        <button type="button" className="btn btn--primary" disabled title="Submitting arrives with VE-8">
          Submit
        </button>
      </div>
    </div>
  )
}
