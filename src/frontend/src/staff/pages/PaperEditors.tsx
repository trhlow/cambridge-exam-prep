import { useState, type FormEvent, type ReactNode } from 'react'
import { adminApi, type OptionEdit, type PaperDetail, type PaperGroupDetail, type PaperOption, type PaperPartDetail, type PaperQuestionDetail } from '../api'
import { ErrorBox, Modal } from '../ui'

/** What the editors open on; each saves through the API and hands back the whole paper. */
export type PaperEdit =
  | { kind: 'title' }
  | { kind: 'part'; part: PaperPartDetail }
  | { kind: 'group'; part: PaperPartDetail; group: PaperGroupDetail }
  | { kind: 'question'; group: PaperGroupDetail; question: PaperQuestionDetail }

type Props = { paper: PaperDetail; edit: PaperEdit; onClose: () => void; onSaved: (paper: PaperDetail) => void }

/**
 * VE-25: fixing a converted draft on the web. HTML fields are edited as HTML (the server sanitises them); option
 * and card keys stay as imported so the answer key keeps pointing at them; the server applies the importer's rules
 * and lists every problem.
 */
export function PaperEditor({ paper, edit, onClose, onSaved }: Props) {
  const save = (call: () => Promise<PaperDetail>) => call().then(onSaved)
  switch (edit.kind) {
    case 'title':
      return <TitleEditor title={paper.title} onClose={onClose} onSave={(title) => save(() => adminApi.editPaperTitle(paper.id, title))} />
    case 'part':
      return <PartEditor part={edit.part} onClose={onClose} onSave={(body) => save(() => adminApi.editPart(paper.id, edit.part.partNo, body))} />
    case 'group':
      return <GroupEditor group={edit.group} partNo={edit.part.partNo} onClose={onClose} onSave={(body) => save(() => adminApi.editGroup(paper.id, edit.part.partNo, edit.group.orderNo, body))} />
    case 'question':
      return <QuestionEditor group={edit.group} question={edit.question} onClose={onClose} onSave={(body) => save(() => adminApi.editQuestion(paper.id, edit.question.number, body))} />
  }
}

function EditorForm({ title, onClose, onSubmit, children }: { title: string; onClose: () => void; onSubmit: () => Promise<unknown>; children: ReactNode }) {
  const [error, setError] = useState<unknown>(null)
  const [isSaving, setIsSaving] = useState(false)
  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setIsSaving(true)
    setError(null)
    try {
      await onSubmit()
    } catch (e) {
      setError(e)
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <Modal title={title} onClose={onClose}>
      <form className="st-form st-editor" onSubmit={submit}>
        {children}
        <ErrorBox error={error} />
        <div className="st-form-actions">
          <button type="submit" className="st-btn st-btn--primary" disabled={isSaving}>
            Save
          </button>
        </div>
      </form>
    </Modal>
  )
}

function HtmlField({ label, value, onChange, rows = 6 }: { label: string; value: string; onChange: (value: string) => void; rows?: number }) {
  return (
    <label className="st-field">
      {label} <span className="st-faint">(HTML)</span>
      <textarea className="st-mono" value={value} onChange={(e) => onChange(e.target.value)} rows={rows} spellCheck={false} />
    </label>
  )
}

function TitleEditor({ title, onClose, onSave }: { title: string; onClose: () => void; onSave: (title: string) => Promise<unknown> }) {
  const [value, setValue] = useState(title)
  return (
    <EditorForm title="Paper title" onClose={onClose} onSubmit={() => onSave(value.trim())}>
      <label className="st-field">
        Title
        <input value={value} onChange={(e) => setValue(e.target.value)} required maxLength={256} />
      </label>
    </EditorForm>
  )
}

function PartEditor({ part, onClose, onSave }: { part: PaperPartDetail; onClose: () => void; onSave: (body: { title: string | null; stimulusHtml: string | null }) => Promise<unknown> }) {
  const [title, setTitle] = useState(part.title ?? '')
  const [stimulus, setStimulus] = useState(part.stimulusHtml ?? '')
  return (
    <EditorForm title={`Part ${part.partNo}`} onClose={onClose} onSubmit={() => onSave({ title: title.trim() || null, stimulusHtml: stimulus.trim() || null })}>
      <label className="st-field">
        Title
        <input value={title} onChange={(e) => setTitle(e.target.value)} maxLength={256} />
      </label>
      <HtmlField label="Passage" value={stimulus} onChange={setStimulus} rows={10} />
    </EditorForm>
  )
}

function OptionFields({ label, options, onChange }: { label: string; options: OptionEdit[]; onChange: (options: OptionEdit[]) => void }) {
  return (
    <fieldset className="st-fieldset">
      <legend>{label}</legend>
      {options.map((option, i) => (
        <label key={option.key} className="st-field st-option-field">
          <strong>{option.key}</strong>
          <input value={option.text ?? ''} onChange={(e) => onChange(options.map((o, j) => (j === i ? { ...o, text: e.target.value } : o)))} maxLength={500} />
        </label>
      ))}
    </fieldset>
  )
}

const asEdits = (options: PaperOption[] | null): OptionEdit[] | undefined => options?.map((o) => ({ key: o.key, text: o.text }))

function GroupEditor(props: { group: PaperGroupDetail; partNo: number; onClose: () => void; onSave: (body: { instructions: string; bodyHtml: string | null; optionPool?: OptionEdit[] }) => Promise<unknown> }) {
  const { group, partNo, onClose, onSave } = props
  const [instructions, setInstructions] = useState(group.instructions)
  const [body, setBody] = useState(group.bodyHtml ?? '')
  const [pool, setPool] = useState(asEdits(group.optionPool))
  const hasGaps = group.interactionType === 'InlineChoice' || group.interactionType === 'InlineText'
  return (
    <EditorForm title={`Part ${partNo} · ${group.interactionType}`} onClose={onClose} onSubmit={() => onSave({ instructions: instructions.trim(), bodyHtml: body.trim() || null, optionPool: pool })}>
      <label className="st-field">
        Instructions
        <textarea value={instructions} onChange={(e) => setInstructions(e.target.value)} rows={3} required maxLength={1000} />
      </label>
      <HtmlField label="Text" value={body} onChange={setBody} rows={10} />
      {hasGaps && <p className="st-faint">Each question of the group appears once in the text as {'{{q:19}}'}.</p>}
      {pool && <OptionFields label="Cards" options={pool} onChange={setPool} />}
    </EditorForm>
  )
}

function QuestionEditor(props: { group: PaperGroupDetail; question: PaperQuestionDetail; onClose: () => void; onSave: (body: Parameters<typeof adminApi.editQuestion>[2]) => Promise<unknown> }) {
  const { group, question, onClose, onSave } = props
  const type = group.interactionType
  const [prompt, setPrompt] = useState(question.promptHtml ?? '')
  const [stimulus, setStimulus] = useState(question.stimulusHtml ?? '')
  const [options, setOptions] = useState(asEdits(question.options))
  const [answers, setAnswers] = useState(question.answers.join('\n'))
  const [points, setPoints] = useState(String(question.points))
  const [minWords, setMinWords] = useState(question.minWords === null ? '' : String(question.minWords))
  const choiceKeys = (type === 'DragDropMatch' ? group.optionPool : question.options)?.map((o) => o.key) ?? []
  const isChoice = choiceKeys.length > 0
  const isEssay = type === 'Essay'

  const submit = () =>
    onSave({
      promptHtml: prompt.trim() || null,
      stimulusHtml: stimulus.trim() || null,
      options,
      answers: isEssay ? [] : answers.split('\n').map((a) => a.trim()).filter((a) => a.length > 0),
      points: Number(points),
      minWords: isEssay ? Number(minWords) : null,
    })

  return (
    <EditorForm title={`Question ${question.number} · ${type}`} onClose={onClose} onSubmit={submit}>
      <HtmlField label="Question" value={prompt} onChange={setPrompt} rows={3} />
      <HtmlField label="Text above the question" value={stimulus} onChange={setStimulus} rows={3} />
      {options && <OptionFields label="Options" options={options} onChange={setOptions} />}
      {!isEssay &&
        (isChoice ? (
          <label className="st-field">
            Answer
            <select value={answers} onChange={(e) => setAnswers(e.target.value)} required>
              {!choiceKeys.includes(answers) && <option value="">Choose…</option>}
              {choiceKeys.map((key) => (
                <option key={key} value={key}>
                  {key}
                </option>
              ))}
            </select>
          </label>
        ) : (
          <label className="st-field">
            Accepted answers <span className="st-faint">(one per line; case, spaces and a final full stop are ignored when marking)</span>
            <textarea value={answers} onChange={(e) => setAnswers(e.target.value)} rows={3} required />
          </label>
        ))}
      <div className="st-grid-2">
        <label className="st-field">
          Points
          <input type="number" min={0} max={100} step={0.5} value={points} onChange={(e) => setPoints(e.target.value)} required />
        </label>
        {isEssay && (
          <label className="st-field">
            Minimum words
            <input type="number" min={1} value={minWords} onChange={(e) => setMinWords(e.target.value)} required />
          </label>
        )}
      </div>
    </EditorForm>
  )
}
