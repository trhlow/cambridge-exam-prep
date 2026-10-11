import { HtmlContent } from '../../ui/HtmlContent'
import { BookmarkIcon } from '../../ui/icons'

type Props = {
  number: number
  promptHtml: string | null
  flagged: boolean
  onToggleFlag: (number: number) => void
}

/** The boxed question number, the bold question text and the bookmark, as in Inspera. */
export function QuestionHead({ number, promptHtml, flagged, onToggleFlag }: Props) {
  return (
    <div className="q-head">
      <span className="q-number">{number}</span>
      <HtmlContent html={promptHtml} className="q-prompt" />
      <BookmarkButton number={number} flagged={flagged} onToggle={onToggleFlag} />
    </div>
  )
}

export function BookmarkButton({ number, flagged, onToggle }: { number: number; flagged: boolean; onToggle: (number: number) => void }) {
  return (
    <button
      type="button"
      className={flagged ? 'bookmark is-flagged' : 'bookmark'}
      aria-pressed={flagged}
      aria-label={`Flag question ${number}`}
      onClick={() => onToggle(number)}
    >
      <BookmarkIcon filled={flagged} />
    </button>
  )
}
