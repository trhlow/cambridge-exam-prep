import { useRef, useState } from 'react'
import { HtmlContent } from '../../ui/HtmlContent'
import { adminApi, type PaperGroupDetail, type PaperOption, type PaperPartDetail, type PaperQuestionDetail } from '../api'
import { Link } from '../Link'
import { gapsAsLabels, isAccepted } from '../paperPreview'
import { Badge, ErrorBox, PageHeader } from '../ui'
import { useLoad } from '../useLoad'
import { PaperEditor, type PaperEdit } from './PaperEditors'

/**
 * A paper as the administrator checks it before publishing: every part with its recording, passages, questions,
 * options (pictures included) and the accepted answers. Gaps in a text show as their question number. A draft
 * can be fixed here (VE-25) before it is published.
 */
export function PaperPage({ paperId }: { paperId: number }) {
  const { data: paper, error, reload, setData } = useLoad(() => adminApi.paper(paperId), `paper-${paperId}`)
  const [actionError, setActionError] = useState<unknown>(null)
  const [edit, setEdit] = useState<PaperEdit | null>(null)

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
  const onEdit = paper.status === 'Draft' ? setEdit : null
  const replaceAudio = async (part: PaperPartDetail, file: File) => {
    setActionError(null)
    try {
      setData(await adminApi.replaceAudio(paper.id, part.partNo, file))
    } catch (e) {
      setActionError(e)
    }
  }

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
            <>
              <button type="button" className="st-btn" onClick={() => setEdit({ kind: 'title' })}>
                Rename
              </button>
              <button type="button" className="st-btn st-btn--primary" onClick={() => void publish()}>
                Publish
              </button>
            </>
          )
        }
      />
      {paper.status === 'Draft' && (
        <p className="st-note">
          Check every question, picture, answer and recording against the PDF and fix what the converter got wrong with Edit. Publish when it is right; until then the paper cannot be scheduled, and afterwards it can no longer be edited.
        </p>
      )}
      {paper.mixVersion && paper.mixPattern && (
        <p className="st-note">
          Version {paper.mixVersion} of the mix “{paper.mixTitle}”: {[...paper.mixPattern].map((source, i) => `Part ${i + 1} from ${source}`).join(', ')}. A mixed paper is a copy of the source parts and cannot be edited; it is given to candidates by seat.
        </p>
      )}
      <ErrorBox error={actionError} />
      {paper.parts.map((part) => (
        <PartPreview key={part.partNo} part={part} onEdit={onEdit} onReplaceAudio={(file) => void replaceAudio(part, file)} />
      ))}
      {edit && (
        <PaperEditor
          paper={paper}
          edit={edit}
          onClose={() => setEdit(null)}
          onChanged={setData}
          onSaved={(saved) => {
            setData(saved)
            setEdit(null)
          }}
        />
      )}
    </>
  )
}

type OnEdit = ((edit: PaperEdit) => void) | null

function EditLink({ onClick, label = 'Edit' }: { onClick: () => void; label?: string }) {
  return (
    <button type="button" className="st-link" onClick={onClick}>
      {label}
    </button>
  )
}

function PartPreview({ part, onEdit, onReplaceAudio }: { part: PaperPartDetail; onEdit: OnEdit; onReplaceAudio: (file: File) => void }) {
  const fileRef = useRef<HTMLInputElement>(null)
  return (
    <section className="st-card st-paper-part">
      <h2 className="st-h2 st-paper-head">
        <span>
          Part {part.partNo}
          {part.title && <> · {part.title}</>}
          {part.questionPerPage && (
            <>
              {' '}
              <span className="st-badge st-badge--grey">one question per page</span>
            </>
          )}
        </span>
        {onEdit && <EditLink onClick={() => onEdit({ kind: 'part', part })} />}
      </h2>
      {part.audioUrl && <audio className="st-paper-audio" controls preload="none" src={part.audioUrl} aria-label={`Part ${part.partNo} recording`} />}
      {onEdit && part.audioUrl && (
        <label className="st-link">
          Replace recording…
          <input
            ref={fileRef}
            type="file"
            accept=".mp3,.m4a,.ogg,.wav"
            hidden
            onChange={(e) => {
              const file = e.target.files?.[0]
              if (file) {
                onReplaceAudio(file)
              }

              if (fileRef.current) {
                fileRef.current.value = ''
              }
            }}
          />
        </label>
      )}
      {part.stimulusHtml && <HtmlContent html={part.stimulusHtml} className="st-paper-html" />}
      {part.groups.map((group) => (
        <GroupPreview
          key={group.orderNo}
          group={group}
          onEditGroup={onEdit && (() => onEdit({ kind: 'group', part, group }))}
          onEditQuestion={onEdit && ((question) => onEdit({ kind: 'question', group, question }))}
        />
      ))}
    </section>
  )
}

function GroupPreview(props: { group: PaperGroupDetail; onEditGroup: (() => void) | null; onEditQuestion: ((question: PaperQuestionDetail) => void) | null }) {
  const { group, onEditGroup, onEditQuestion } = props
  return (
    <div className="st-paper-group">
      <p className="st-faint">
        {group.interactionType}
        {group.chooseCount && <> · candidates answer {group.chooseCount} of {group.questions.length}</>} · {group.instructions} {onEditGroup && <EditLink onClick={onEditGroup} label="Edit instructions and text" />}
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
            {onEditQuestion && <th />}
          </tr>
        </thead>
        <tbody>
          {group.questions.map((question) => (
            <QuestionRow key={question.number} question={question} pool={group.optionPool} onEdit={onEditQuestion && (() => onEditQuestion(question))} />
          ))}
        </tbody>
      </table>
    </div>
  )
}

function QuestionRow({ question, pool, onEdit }: { question: PaperQuestionDetail; pool: PaperOption[] | null; onEdit: (() => void) | null }) {
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
      <td>
        {essay ? (
          <span className="st-faint">Marked by an examiner</span>
        ) : (
          <strong>
            {question.answers.join(' / ')}
            {card && <span className="st-faint"> ({card.text})</span>}
          </strong>
        )}
      </td>
      {onEdit && (
        <td className="st-row-actions">
          <EditLink onClick={onEdit} />
        </td>
      )}
    </tr>
  )
}
