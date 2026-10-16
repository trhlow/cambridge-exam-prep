import { useEffect, useState, type FormEvent } from 'react'
import { adminApi, staffApi, type ExamTypeView, type PaperListItem } from '../api'
import { Link } from '../Link'
import { navigate } from '../router'
import { ErrorBox, PageHeader } from '../ui'
import { useLoad } from '../useLoad'

type Form = {
  name: string
  examTypeId: number
  scheduledDate: string
  room: string
  papers: Record<number, number>
  sessionPin: string
  invigilatorPassword: string
}

const today = () => new Date().toISOString().slice(0, 10)

/**
 * VE-21: create or edit a session. Each section of the exam type gets one published paper; the PIN and invigilator
 * password are generated when left empty. Once candidates have started, the papers are locked by the server.
 */
export function ExamFormPage({ examId }: { examId?: number }) {
  const isNew = examId === undefined
  const { data: types } = useLoad(adminApi.examTypes, 'types')
  const { data: papers } = useLoad(adminApi.examPapers, 'exam-papers')
  const [form, setForm] = useState<Form | null>(isNew ? { name: '', examTypeId: 0, scheduledDate: today(), room: '', papers: {}, sessionPin: '', invigilatorPassword: '' } : null)
  const [hasStarted, setHasStarted] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [isSaving, setIsSaving] = useState(false)

  useEffect(() => {
    if (isNew) {
      return
    }

    staffApi.exam(examId).then((exam) => {
      setHasStarted(exam.hasStarted)
      setForm({
        name: exam.name,
        examTypeId: exam.examTypeId,
        scheduledDate: exam.scheduledDate,
        room: exam.room ?? '',
        papers: Object.fromEntries(exam.sections.map((s) => [s.sectionTemplateId, s.testPaperId])),
        sessionPin: exam.sessionPin,
        invigilatorPassword: exam.invigilatorPassword,
      })
    }, setError)
  }, [examId, isNew])

  if (!form || !types || !papers) {
    return error ? <ErrorBox error={error} /> : <p className="st-empty">Loading…</p>
  }

  // A new exam starts on the first exam type until the administrator picks another.
  const examTypeId = form.examTypeId || types[0]?.id || 0
  const type = types.find((t) => t.id === examTypeId)
  const update = (changes: Partial<Form>) => setForm({ ...form, ...changes })
  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setIsSaving(true)
    setError(null)
    const body = {
      name: form.name,
      examTypeId,
      scheduledDate: form.scheduledDate,
      room: form.room || null,
      papers: (type?.sections ?? []).map((s) => ({ sectionTemplateId: s.id, testPaperId: form.papers[s.id] ?? 0 })),
      sessionPin: form.sessionPin || null,
      invigilatorPassword: form.invigilatorPassword || null,
    }
    try {
      const saved = isNew ? await adminApi.createExam(body) : await adminApi.updateExam(examId, body)
      navigate(`/staff/exams/${saved.id}`)
    } catch (e) {
      setError(e)
      setIsSaving(false)
    }
  }

  return (
    <>
      <PageHeader title={isNew ? 'New exam' : `Edit ${form.name}`} meta={<Link to={isNew ? '/staff' : `/staff/exams/${examId}`}>Cancel</Link>} />
      <form className="st-card st-form" onSubmit={submit}>
        <div className="st-grid-2">
          <label className="st-field">
            Name
            <input value={form.name} onChange={(e) => update({ name: e.target.value })} placeholder="A2 Key for Schools – 20/11 morning" required maxLength={200} />
          </label>
          <label className="st-field">
            Exam
            <select value={examTypeId} disabled={hasStarted} onChange={(e) => update({ examTypeId: Number(e.target.value), papers: {} })}>
              {types.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.name}
                </option>
              ))}
            </select>
          </label>
          <label className="st-field">
            Date
            <input type="date" value={form.scheduledDate} onChange={(e) => update({ scheduledDate: e.target.value })} required />
          </label>
          <label className="st-field">
            Room
            <input value={form.room} onChange={(e) => update({ room: e.target.value })} placeholder="Room 1" maxLength={64} />
          </label>
        </div>

        <h2 className="st-h2">Papers</h2>
        {hasStarted && <p className="st-note">Candidates have started this exam, so its papers can no longer change.</p>}
        {type && <PaperChoices type={type} papers={papers} chosen={form.papers} disabled={hasStarted} onChoose={(templateId, paperId) => update({ papers: { ...form.papers, [templateId]: paperId } })} />}

        <h2 className="st-h2">Exam room</h2>
        <div className="st-grid-2">
          <label className="st-field">
            Session PIN
            <input value={form.sessionPin} onChange={(e) => update({ sessionPin: e.target.value })} inputMode="numeric" placeholder="Leave empty for a random 4-digit PIN" />
          </label>
          <label className="st-field">
            Invigilator password
            <input value={form.invigilatorPassword} onChange={(e) => update({ invigilatorPassword: e.target.value })} placeholder="Leave empty to generate one" />
          </label>
        </div>

        <ErrorBox error={error} />
        <div className="st-form-actions">
          <button type="submit" className="st-btn st-btn--primary" disabled={isSaving}>
            {isNew ? 'Create exam' : 'Save'}
          </button>
        </div>
      </form>
    </>
  )
}

function PaperChoices(props: { type: ExamTypeView; papers: PaperListItem[]; chosen: Record<number, number>; disabled: boolean; onChoose: (templateId: number, paperId: number) => void }) {
  const { type, papers, chosen, disabled, onChoose } = props
  return (
    <div className="st-grid-2">
      {type.sections.map((section) => {
        const options = papers.filter((p) => p.sectionTemplateId === section.id && (p.status === 'Published' || p.id === chosen[section.id]))
        return (
          <label key={section.id} className="st-field">
            <span>
              {section.title} <span className="st-faint">· {section.durationMinutes} minutes</span>
            </span>
            <select value={chosen[section.id] ?? ''} disabled={disabled} onChange={(e) => onChoose(section.id, Number(e.target.value))} required>
              <option value="">Choose a published paper…</option>
              {options.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.title}
                </option>
              ))}
            </select>
            {options.length === 0 && <span className="st-faint">No published {section.title} paper yet — import one under Papers.</span>}
          </label>
        )
      })}
    </div>
  )
}
