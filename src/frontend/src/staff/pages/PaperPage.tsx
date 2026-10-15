import { useState } from 'react'
import { HtmlContent } from '../../ui/HtmlContent'
import { adminApi, type PaperGroupDetail, type PaperOption, type PaperPartDetail, type PaperQuestionDetail } from '../api'
import { Link } from '../Link'
import { gapsAsLabels, isAccepted } from '../paperPreview'
import { Badge, ErrorBox, PageHeader } from '../ui'
import { useLoad } from '../useLoad'

/**
 * A paper as the administrator checks it before publishing: every part with its recording, passages, questions,
 * options (pictures included) and the accepted answers. Gaps in a text show as their question number.
 */
export function PaperPage({ paperId }: { paperId: number }) {
  const { data: paper, error, reload } = useLoad(() => adminApi.paper(paperId), `paper-${paperId}`)
  const [actionError, setActionError] = useState<unknown>(null)

  if (!paper) {
    return error ? <ErrorBox error={error} /> : <p className="st-empty">Loading…</p>
  }

  const publish = async () => {
    setActionError(null)
    try {
      await adminApi.publishPaper(paper.id)
      reload()
    } catch (e) {
      setActionError(e)
    }
  }

  const questionCount = paper.parts.reduce((sum, part) => sum + part.groups.reduce((n, group) => n + group.questions.length, 0), 0)

  return (
    <>
      <PageHeader
        title={paper.title}
        meta={
          <>
            <Link to="/staff/papers">Papers</Link> · {paper.examType} · {paper.section} · {paper.parts.length} parts, {questionCount} questions · <Badge value={paper.status} />
          </>
        }
        actions={
          paper.status === 'Draft' && (
            <button type="button" className="st-btn st-btn--primary" onClick={() => void publish()}>
              Publish
            </button>
          )
        }
      />
      {paper.status === 'Draft' && (
        <p className="st-note">Check every question, picture, answer and recording against the PDF. Publish when it is right; until then the paper cannot be scheduled.</p>
      )}
      <ErrorBox error={actionError} />
      {paper.parts.map((part) => (
        <PartPreview key={part.partNo} part={part} />
      ))}
    </>
  )
}

function PartPreview({ part }: { part: PaperPartDetail }) {
  return (
    <section className="st-card st-paper-part">
      <h2 className="st-h2">
        Part {part.partNo}
        {part.title && <> · {part.title}</>}
      </h2>
      {part.audioUrl && <audio className="st-paper-audio" controls preload="none" src={part.audioUrl} aria-label={`Part ${part.partNo} recording`} />}
      {part.stimulusHtml && <HtmlContent html={part.stimulusHtml} className="st-paper-html" />}
      {part.groups.map((group, index) => (
        <GroupPreview key={index} group={group} />
      ))}
    </section>
  )
}

function GroupPreview({ group }: { group: PaperGroupDetail }) {
  return (
    <div className="st-paper-group">
      <p className="st-faint">
        {group.interactionType} · {group.instructions}
      </p>
      {group.bodyHtml && <HtmlContent html={gapsAsLabels(group.bodyHtml)} className="st-paper-html" />}
      {group.optionPool && (
        <p className="st-paper-pool">
          Cards:{' '}
          {group.optionPool.map((card) => (
            <span key={card.key} className="st-badge st-badge--grey">
              {card.key} {card.text}
            </span>
          ))}
        </p>
      )}
      <table className="st-table">
        <thead>
          <tr>
            <th className="st-num">No.</th>
            <th>Question</th>
            <th>Options</th>
            <th>Answer</th>
          </tr>
        </thead>
        <tbody>
          {group.questions.map((question) => (
            <QuestionRow key={question.number} question={question} pool={group.optionPool} />
          ))}
        </tbody>
      </table>
    </div>
  )
}

function QuestionRow({ question, pool }: { question: PaperQuestionDetail; pool: PaperOption[] | null }) {
  const essay = question.minWords !== null
  const card = pool?.find((option) => isAccepted(option, question.answers))
  return (
    <tr>
      <td className="st-num">{question.number}</td>
      <td>
        {question.stimulusHtml && <HtmlContent html={question.stimulusHtml} className="st-paper-html" />}
        {question.promptHtml && <HtmlContent html={question.promptHtml} />}
        {essay && <div className="st-faint">At least {question.minWords} words · {question.points} marks</div>}
      </td>
      <td>
        {question.options && (
          <div className="st-paper-options">
            {question.options.map((option) => (
              <span key={option.key} className={isAccepted(option, question.answers) ? 'st-paper-option is-answer' : 'st-paper-option'}>
                <strong>{option.key}</strong>
                {option.imageUrl ? <img src={option.imageUrl} alt={option.text ?? `Option ${option.key}`} /> : <span>{option.text}</span>}
              </span>
            ))}
          </div>
        )}
      </td>
      <td>{essay ? <span className="st-faint">Marked by a teacher</span> : <strong>{question.answers.join(' / ')}{card && <span className="st-faint"> ({card.text})</span>}</strong>}</td>
    </tr>
  )
}
