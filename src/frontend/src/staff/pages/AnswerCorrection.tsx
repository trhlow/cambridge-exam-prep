import { useState, type FormEvent } from 'react'
import { adminApi, type AnswersCorrected, type PaperGroupDetail, type PaperQuestionDetail } from '../api'
import { ConfirmButton, ErrorBox, Modal } from '../ui'

/**
 * A6 repair: a published paper cannot be edited, but a wrong answer key can be corrected by an administrator. Saving
 * marks the question again on every attempt already marked, so it asks twice.
 */
export function AnswerCorrection(props: {
  paperId: number
  group: PaperGroupDetail
  question: PaperQuestionDetail
  onClose: () => void
  onDone: (result: AnswersCorrected) => void
}) {
  const { paperId, group, question, onClose, onDone } = props
  const choiceKeys = (group.interactionType === 'DragDropMatch' ? group.optionPool : question.options)?.map((o) => o.key) ?? []
  const [answers, setAnswers] = useState(question.answers.join('\n'))
  const [error, setError] = useState<unknown>(null)
  const [isSaving, setIsSaving] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setIsSaving(true)
    setError(null)
    try {
      onDone(await adminApi.correctAnswers(paperId, question.number, answers.split('\n').map((a) => a.trim()).filter((a) => a.length > 0)))
    } catch (e) {
      setError(e)
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <Modal title={`Question ${question.number} · correct the answer`} onClose={onClose}>
      <form className="st-form st-editor" onSubmit={submit}>
        <p className="st-note">
          This paper is published, so only its answer key can change. Every attempt already marked on this paper is marked again for this question, and the scores follow.
        </p>
        {choiceKeys.length > 0 ? (
          <label className="st-field">
            Correct answer
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
            Correct answers <span className="st-faint">(one per line; case, spaces and a final full stop are ignored when marking)</span>
            <textarea value={answers} onChange={(e) => setAnswers(e.target.value)} rows={3} required />
          </label>
        )}
        <ErrorBox error={error} />
        <div className="st-form-actions">
          <ConfirmButton type="submit" label="Correct the answer" confirmLabel="Confirm: mark the attempts again" onConfirm={() => {}} disabled={isSaving} />
        </div>
      </form>
    </Modal>
  )
}
