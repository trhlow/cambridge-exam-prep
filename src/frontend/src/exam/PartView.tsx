import type { Answers, Part, QuestionGroup, Selection, Selections } from '../api/types'
import { HtmlContent } from '../ui/HtmlContent'
import { DragDropMatch } from './listening/DragDropMatch'
import { questionRange } from './paperLogic'
import { ChoiceQuestion } from './questions/ChoiceQuestion'
import { EssayQuestion } from './questions/EssayQuestion'
import { GapText } from './questions/GapText'
import { BookmarkButton } from './questions/QuestionHead'
import { ROOT_ATTRIBUTE } from './annotations/useAnnotations'
import { SplitPane } from './SplitPane'

type Props = {
  part: Part
  questionNumbers: number[]
  answers: Answers
  onAnswer: (number: number, value: string) => void
  /** VE-35: "Answering this question?" choices, where the candidate answers only some questions. */
  selections?: Selections
  onSelect?: (number: number, selection: Selection) => Promise<string | null>
  flagged: ReadonlySet<number>
  onToggleFlag: (number: number) => void
}

/** One page of a part: the instruction box, then the part laid out in one column or split in two. */
export function PartView({ part, questionNumbers, answers, onAnswer, selections = {}, onSelect, flagged, onToggleFlag }: Props) {
  const groups = part.groups
    .map((group) => ({ group, questions: group.questions.filter((q) => questionNumbers.includes(q.number)) }))
    .filter((g) => g.questions.length > 0)
  // B1 Writing: each question brings its own task (email, notice, story opening), shown with the part's text.
  const tasks = groups.filter(({ group }) => group.interactionType === 'Essay').flatMap(({ questions }) => questions.filter((q) => q.stimulusHtml))

  const passage = (
    <>
      {part.title && <h2 className="part-title">{part.title}</h2>}
      <HtmlContent html={part.stimulusHtml} className="passage" />
      {tasks.map((q) => (
        <HtmlContent key={q.number} html={q.stimulusHtml} className="passage" />
      ))}
    </>
  )

  const questions = groups.map(({ group }) => (
    <GroupBody
      key={group.questions[0].number}
      group={group}
      visible={questionNumbers}
      answers={answers}
      onAnswer={onAnswer}
      selections={selections}
      onSelect={onSelect}
      flagged={flagged}
      onToggleFlag={onToggleFlag}
    />
  ))

  return (
    <div className="part-page" {...{ [ROOT_ATTRIBUTE]: `${part.partNo}:${questionNumbers.join(',')}` }}>
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

function GroupBody({ group, visible, answers, onAnswer, selections = {}, onSelect, flagged, onToggleFlag }: GroupBodyProps) {
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
    case 'Essay': {
      const chooseCount = group.chooseCount
      const selected = group.questions.filter((q) => selections[q.number] === 'Yes').length
      return (
        <>
          {group.questions
            .filter((q) => visible.includes(q.number))
            .map((q) => (
              <div key={q.number} className="with-bookmark">
                <EssayQuestion
                  question={q}
                  answer={answers[q.number] ?? ''}
                  onAnswer={onAnswer}
                  choice={
                    chooseCount && onSelect
                      ? { selection: selections[q.number] ?? 'Undecided', selected, chooseCount, onSelect: (selection) => onSelect(q.number, selection) }
                      : undefined
                  }
                />
                <BookmarkButton number={q.number} flagged={flagged.has(q.number)} onToggle={onToggleFlag} />
              </div>
            ))}
        </>
      )
    }
    case 'DragDropMatch':
      return (
        <div className="with-bookmark">
          <DragDropMatch group={group} answers={answers} onAnswer={onAnswer} />
          <BookmarkButton number={group.questions[0].number} flagged={flagged.has(group.questions[0].number)} onToggle={onToggleFlag} />
        </div>
      )
  }
}
