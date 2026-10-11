import type { Question } from '../../api/types'
import { HtmlContent } from '../../ui/HtmlContent'
import { QuestionHead } from './QuestionHead'

export interface ChoiceQuestionProps {
  question: Question
  answer: string | undefined
  onAnswer: (number: number, value: string) => void
  flagged: boolean
  onToggleFlag: (number: number) => void
  /** Picture options sit side by side with the radio underneath (A2 Listening Part 1). */
  pictures?: boolean
}

/** One question answered by picking one option (radio), with its own notice or picture above if it has one. */
export function ChoiceQuestion({ question, answer, onAnswer, flagged, onToggleFlag, pictures = false }: ChoiceQuestionProps) {
  const name = `question-${question.number}`

  return (
    <section className="question" aria-label={`Question ${question.number}`} data-question={question.number}>
      <HtmlContent html={question.stimulusHtml} className="q-stimulus" />
      <QuestionHead number={question.number} promptHtml={question.promptHtml} flagged={flagged} onToggleFlag={onToggleFlag} />
      <div className={pictures ? 'options options--pictures' : 'options'} role="radiogroup" aria-label={`Answers for question ${question.number}`}>
        {(question.options ?? []).map((option) => {
          const checked = answer === option.key
          return (
            <label key={option.key} className={checked ? 'option is-selected' : 'option'}>
              {option.imageUrl && <img src={option.imageUrl} alt={option.text ?? `Option ${option.key}`} className="option-picture" />}
              <span className="option-line">
                <input type="radio" name={name} value={option.key} checked={checked} onChange={() => onAnswer(question.number, option.key)} />
                {!pictures && <span className="option-text">{option.text}</span>}
                {pictures && <span className="visually-hidden">{option.text ?? `Option ${option.key}`}</span>}
              </span>
            </label>
          )
        })}
      </div>
    </section>
  )
}
