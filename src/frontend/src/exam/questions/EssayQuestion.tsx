import { useState } from 'react'
import type { Question, Selection } from '../../api/types'
import { countWords } from '../paperLogic'

/** VE-35: "Answering this question?" where the candidate answers only some of the group's questions. */
export interface EssayChoice {
  selection: Selection
  /** Questions of the group set to Yes, this one included. */
  selected: number
  chooseCount: number
  /** Resolves to the server's refusal, or null once saved. */
  onSelect: (selection: Selection) => Promise<string | null>
}

export interface EssayQuestionProps {
  question: Question
  answer: string
  onAnswer: (number: number, value: string) => void
  choice?: EssayChoice
}

/** Free writing (A2 Writing Part 6 email, Part 7 story; B1 Writing) with the live word count Inspera shows. */
export function EssayQuestion({ question, answer, onAnswer, choice }: EssayQuestionProps) {
  return (
    <div className="essay">
      {choice && <ChoiceBox number={question.number} choice={choice} />}
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

/** Inspera's box above the editor: a Yes / No / Undecided list and how many questions are chosen so far. */
function ChoiceBox({ number, choice }: { number: number; choice: EssayChoice }) {
  const [error, setError] = useState<string | null>(null)
  const isFull = choice.selected >= choice.chooseCount && choice.selection !== 'Yes'

  return (
    <div className="essay-choice">
      <label className="essay-choice-label">
        Answering this question?
        <select
          aria-label={`Answering question ${number}?`}
          value={choice.selection}
          onChange={(e) => {
            setError(null)
            void choice.onSelect(e.target.value as Selection).then(setError)
          }}
        >
          <option value="Yes" disabled={isFull}>
            Yes
          </option>
          <option value="No">No</option>
          <option value="Undecided">Undecided</option>
        </select>
      </label>
      <span className="essay-choice-count">
        {choice.selected} of {choice.chooseCount} {choice.chooseCount === 1 ? 'question' : 'questions'} selected
      </span>
      {error && (
        <span className="essay-choice-error" role="alert">
          {error}
        </span>
      )}
    </div>
  )
}
