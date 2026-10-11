import type { Answers, Part, QuestionGroup } from '../api/types'
import { HtmlContent } from '../ui/HtmlContent'
import { questionRange } from './paperLogic'
import { ChoiceQuestion } from './questions/ChoiceQuestion'
import { EssayQuestion } from './questions/EssayQuestion'
import { GapText } from './questions/GapText'
import { BookmarkButton } from './questions/QuestionHead'
import { SplitPane } from './SplitPane'

type Props = {
  part: Part
  questionNumbers: number[]
  answers: Answers
  onAnswer: (number: number, value: string) => void
  flagged: ReadonlySet<number>
  onToggleFlag: (number: number) => void
}

/** One page of a part: the instruction box, then the part laid out in one column or split in two. */
export function PartView({ part, questionNumbers, answers, onAnswer, flagged, onToggleFlag }: Props) {
  const groups = part.groups
    .map((group) => ({ group, questions: group.questions.filter((q) => questionNumbers.includes(q.number)) }))
    .filter((g) => g.questions.length > 0)

  const passage = (
    <>
      {part.title && <h2 className="part-title">{part.title}</h2>}
      <HtmlContent html={part.stimulusHtml} className="passage" />
    </>
  )

  const questions = groups.map(({ group }) => (
    <GroupBody key={group.questions[0].number} group={group} visible={questionNumbers} answers={answers} onAnswer={onAnswer} flagged={flagged} onToggleFlag={onToggleFlag} />
  ))

  return (
    <div className="part-page">
      {part.groups.map((group) => (
        <InstructionBox key={group.questions[0].number} group={group} />
      ))}
      {part.layout === 'Split' ? (
        <SplitPane left={passage} right={questions} />
      ) : (
        <div className="single">
          {passage}
          {questions}
        </div>
      )}
    </div>
  )
}

function InstructionBox({ group }: { group: QuestionGroup }) {
  return (
    <div className="instructions">
      <h3 className="instructions-range">{questionRange(group.questions)}</h3>
      <div className="instructions-text">{group.instructions}</div>
    </div>
  )
}

type GroupBodyProps = Omit<Props, 'part' | 'questionNumbers'> & { group: QuestionGroup; visible: number[] }

function GroupBody({ group, visible, answers, onAnswer, flagged, onToggleFlag }: GroupBodyProps) {
  switch (group.interactionType) {
    case 'SingleChoice':
    case 'ImageChoice':
      return (
        <>
          {group.questions
            .filter((q) => visible.includes(q.number))
            .map((q) => (
              <ChoiceQuestion
                key={q.number}
                question={q}
                answer={answers[q.number]}
                onAnswer={onAnswer}
                flagged={flagged.has(q.number)}
                onToggleFlag={onToggleFlag}
                pictures={group.interactionType === 'ImageChoice'}
              />
            ))}
        </>
      )
    case 'InlineChoice':
    case 'InlineText':
      return (
        <div className="with-bookmark">
          <GapText group={group} answers={answers} onAnswer={onAnswer} />
          <BookmarkButton number={group.questions[0].number} flagged={flagged.has(group.questions[0].number)} onToggle={onToggleFlag} />
        </div>
      )
    case 'Essay':
      return (
        <>
          {group.questions.map((q) => (
            <div key={q.number} className="with-bookmark">
              <EssayQuestion question={q} answer={answers[q.number] ?? ''} onAnswer={onAnswer} />
              <BookmarkButton number={q.number} flagged={flagged.has(q.number)} onToggle={onToggleFlag} />
            </div>
          ))}
        </>
      )
    case 'DragDropMatch':
      return <p className="not-yet">Drag-and-drop matching arrives with the Listening screen (VE-10).</p>
  }
}
