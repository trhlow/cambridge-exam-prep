import { useEffect, useState, type FormEvent } from 'react'
import { adminApi, staffApi, type ExamTypeView, type PaperListItem, type PaperMixView } from '../api'
import { Link } from '../Link'
import { navigate } from '../router'
import { ErrorBox, PageHeader } from '../ui'
import { useLoad } from '../useLoad'

type Form = {
  name: string
  examTypeId: number
  scheduledDate: string
  room: string
  /** Per section template: "p:<paper id>" (one paper for everyone) or "m:<mix id>" (four papers by seat). */
  papers: Record<number, string>
  sessionPin: string
  invigilatorPassword: string
  sebConfigKey: string
  /** Empty: one row. */
  seatsPerRow: string
}

const today = () => new Date().toISOString().slice(0, 10)

/**
 * VE-21: create or edit a session. Each section of the exam type gets one published paper, or a mix of papers given
 * by seat (VE-67: neighbouring computers never get the same paper; computers per row decide who the neighbours are);
 * the PIN and invigilator password are generated when left empty. Once candidates have started, the papers are
 * locked by the server.
 */
export function ExamFormPage({ examId }: { examId?: number }) {
  const isNew = examId === undefined
  const { data: types } = useLoad(adminApi.examTypes, 'types')
  const { data: papers } = useLoad(adminApi.examPapers, 'exam-papers')
  const { data: mixes } = useLoad(adminApi.examPaperMixes, 'exam-paper-mixes')
  const [form, setForm] = useState<Form | null>(
    isNew ? { name: '', examTypeId: 0, scheduledDate: today(), room: '', papers: {}, sessionPin: '', invigilatorPassword: '', sebConfigKey: '', seatsPerRow: '' } : null,
  )
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
        papers: Object.fromEntries(exam.sections.map((s) => [s.sectionTemplateId, s.paperMixId ? `m:${s.paperMixId}` : `p:${s.testPaperId}`])),
        sessionPin: exam.sessionPin,
        invigilatorPassword: exam.invigilatorPassword,
        sebConfigKey: exam.sebConfigKey ?? '',
        seatsPerRow: exam.seatsPerRow ? String(exam.seatsPerRow) : '',
      })
    }, setError)
  }, [examId, isNew])

  if (!form || !types || !papers || !mixes) {
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
      papers: (type?.sections ?? []).map((s) => {
        const [kind, id] = (form.papers[s.id] ?? 'p:0').split(':')
        return kind === 'm' ? { sectionTemplateId: s.id, testPaperId: 0, paperMixId: Number(id) } : { sectionTemplateId: s.id, testPaperId: Number(id) }
      }),
      sessionPin: form.sessionPin || null,
      invigilatorPassword: form.invigilatorPassword || null,
      sebConfigKey: form.sebConfigKey.trim(),
      seatsPerRow: form.seatsPerRow ? Number(form.seatsPerRow) : 0,
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
        {type && <PaperChoices type={type} papers={papers} mixes={mixes} chosen={form.papers} disabled={hasStarted} onChoose={(templateId, choice) => update({ papers: { ...form.papers, [templateId]: choice } })} />}
        <label className="st-field st-field--narrow">
          <span>
            Computers per row <span className="st-faint">(for mixed papers)</span>
          </span>
          <input type="number" min={1} max={50} value={form.seatsPerRow} onChange={(e) => update({ seatsPerRow: e.target.value })} placeholder="One row" />
          <span className="st-faint">
            Seat numbers run row by row. With a mix of papers, computers side by side, in front, behind and diagonally next to each other never get the same paper. Leave empty if the computers stand in one row.
          </span>
        </label>

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
        <label className="st-field">
          <span>
            Safe Exam Browser config key <span className="st-faint">(optional)</span>
          </span>
          <input
            className="st-mono"
            value={form.sebConfigKey}
            onChange={(e) => update({ sebConfigKey: e.target.value })}
            placeholder="Leave empty to allow any browser"
            maxLength={64}
            pattern="[0-9A-Fa-f]{64}"
            spellCheck={false}
          />
          <span className="st-faint">From the SEB Config Tool (Config Key, 64 characters). When set, candidates can only sign in and work through Safe Exam Browser with that settings file.</span>
        </label>

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

function PaperChoices(props: {
  type: ExamTypeView
  papers: PaperListItem[]
  mixes: PaperMixView[]
  chosen: Record<number, string>
  disabled: boolean
  onChoose: (templateId: number, choice: string) => void
}) {
  const { type, papers, mixes, chosen, disabled, onChoose } = props
  return (
    <div className="st-grid-2">
      {type.sections.map((section) => {
        // The four papers of a mix are chosen through their mix, not one by one.
        const options = papers.filter((p) => p.sectionTemplateId === section.id && !p.paperMixId && (p.status === 'Published' || `p:${p.id}` === chosen[section.id]))
        const sectionMixes = mixes.filter((m) => m.sectionTemplateId === section.id)
        const isMix = chosen[section.id]?.startsWith('m:')
        return (
          <label key={section.id} className="st-field">
            <span>
              {section.title} <span className="st-faint">· {section.durationMinutes} minutes</span>
            </span>
            <select value={chosen[section.id] ?? ''} disabled={disabled} onChange={(e) => onChoose(section.id, e.target.value)} required>
              <option value="">Choose a published paper…</option>
              <optgroup label="One paper for everyone">
                {options.map((p) => (
                  <option key={p.id} value={`p:${p.id}`}>
                    {p.title}
                  </option>
                ))}
              </optgroup>
              {sectionMixes.length > 0 && (
                <optgroup label="Mixed papers — 4 versions by seat">
                  {sectionMixes.map((m) => (
                    <option key={m.id} value={`m:${m.id}`}>
                      {m.title}
                    </option>
                  ))}
                </optgroup>
              )}
            </select>
            {isMix && <span className="st-faint">Each computer gets one of four versions by its seat number; neighbours never share one.</span>}
            {options.length === 0 && sectionMixes.length === 0 && <span className="st-faint">No published {section.title} paper yet — import one under Papers.</span>}
          </label>
        )
      })}
    </div>
  )
}
