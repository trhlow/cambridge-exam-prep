import type { Answers, Paper } from '../api/types'
import { CheckIcon } from '../ui/icons'
import { answeredCount, isAnswered, questionsOf, type Page } from './paperLogic'

export interface BottomBarProps {
  paper: Paper
  pages: Page[]
  pageIndex: number
  answers: Answers
  onGoTo: (pageIndex: number) => void
  onReview: () => void
  flagged?: ReadonlySet<number>
  /** The question being worked on; defaults to the first question of the page. */
  activeNumber?: number
  onFocusQuestion?: (number: number) => void
}

/**
 * Inspera's part bar: the open part lists its question numbers (answered ones marked, current one boxed);
 * the others show "Part n  x of y". The last tile (✓) leads to the review and submit page.
 */
export function BottomBar({ paper, pages, pageIndex, answers, onGoTo, onReview, flagged = new Set(), activeNumber, onFocusQuestion }: BottomBarProps) {
  const current = pages[pageIndex]
  const active = activeNumber !== undefined && current?.questionNumbers.includes(activeNumber) ? activeNumber : current?.questionNumbers[0]

  return (
    <nav className="bottom-bar" aria-label="Parts and questions">
      {paper.parts.map((part, partIndex) => {
        const questions = questionsOf(part)
        const firstPage = pages.findIndex((p) => p.partIndex === partIndex)

        if (current?.partIndex !== partIndex) {
          return (
            <button key={part.partNo} type="button" className="part-tile" onClick={() => onGoTo(firstPage)}>
              <span className="part-label">Part {part.partNo}</span>
              <span className="part-count">
                {answeredCount(part, answers)} of {questions.length}
              </span>
            </button>
          )
        }

        return (
          <div key={part.partNo} className="part-tile is-active">
            <span className="part-label">Part {part.partNo}</span>
            <span className="part-numbers">
              {questions.map((q) => {
                const isCurrent = q.number === active
                const classes = ['q-link', isCurrent && 'is-current', isAnswered(answers, q.number) && 'is-answered', flagged.has(q.number) && 'is-flagged']
                return (
                  <button
                    key={q.number}
                    type="button"
                    className={classes.filter(Boolean).join(' ')}
                    aria-label={`Question ${q.number}`}
                    aria-current={isCurrent ? 'true' : undefined}
                    onClick={() => {
                      onGoTo(pages.findIndex((p) => p.questionNumbers.includes(q.number)))
                      onFocusQuestion?.(q.number)
                    }}
                  >
                    {q.number}
                  </button>
                )
              })}
            </span>
          </div>
        )
      })}
      <button type="button" className="review-tile" aria-label="Review and submit" onClick={onReview}>
        <CheckIcon />
      </button>
    </nav>
  )
}
