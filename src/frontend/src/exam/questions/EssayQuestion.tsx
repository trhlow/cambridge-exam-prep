import type { Question } from '../../api/types'
import { countWords } from '../paperLogic'

export interface EssayQuestionProps {
  question: Question
  answer: string
  onAnswer: (number: number, value: string) => void
}

/** Free writing (A2 Writing Part 6 email, Part 7 story) with the live word count Inspera shows. */
export function EssayQuestion({ question, answer, onAnswer }: EssayQuestionProps) {
  return (
    <div className="essay">
      <textarea
        className="essay-box"
        data-question={question.number}
        aria-label={`Question ${question.number}`}
        value={answer}
        spellCheck={false}
        autoComplete="off"
        autoCorrect="off"
        onChange={(e) => onAnswer(question.number, e.target.value)}
      />
      <div className="essay-count">Words: {countWords(answer)}</div>
    </div>
  )
}
