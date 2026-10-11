import { useState, type FormEvent } from 'react'
import { staffApi, type ActionOutcome, type MonitorActionRequest, type MonitorRow } from '../api'
import type { ActionButton } from '../monitorLogic'
import { ConfirmButton, ErrorBox, Modal } from '../ui'

export type DialogKind = { kind: ActionButton; minutes?: number; reason?: string } | { kind: 'message'; toRoom: boolean }

type Done = (result: { text: string; skipped: ActionOutcome['skipped'] }, next?: DialogKind) => void

type Props = { dialog: DialogKind; examId: number; sectionId: number; selected: MonitorRow[]; onClose: () => void; onDone: Done }

/** Incident reasons offered by Inspera's pop-ups; the choice is kept in the action log. */
const REASONS = ['Fire alarm or evacuation', 'Power cut', 'Technical problem with the computer', 'Headset or audio problem', 'Candidate unwell', 'Other']

const TITLES: Record<DialogKind['kind'], string> = {
  Pause: 'Pause',
  Resume: 'Resume',
  AddIncidentTime: 'Incident extra time',
  EditEndTime: 'Edit end time',
  RewindAudio: 'Rewind audio',
  message: 'Message',
}

const appliedText = (outcome: ActionOutcome, what: string) =>
  outcome.applied === 0 ? `${what}: no candidate changed.` : `${what}: done for ${outcome.applied} candidate${outcome.applied === 1 ? '' : 's'}.`

/** The pop-up for each action of the bar, as in the Inspera Admin Portal. */
export function ActionDialog({ dialog, examId, sectionId, selected, onClose, onDone }: Props) {
  const ids = selected.map((r) => r.candidateId)
  const [error, setError] = useState<unknown>(null)
  const [isBusy, setIsBusy] = useState(false)

  const run = async (request: Omit<MonitorActionRequest, 'candidateIds'>, what: string, next?: (outcome: ActionOutcome) => DialogKind | undefined) => {
    setIsBusy(true)
    setError(null)
    try {
      const outcome = await staffApi.act(examId, sectionId, { ...request, candidateIds: ids })
      onDone({ text: appliedText(outcome, what), skipped: outcome.skipped }, next?.(outcome))
    } catch (e) {
      setError(e)
      setIsBusy(false)
    }
  }

  const title = dialog.kind === 'message' && dialog.toRoom ? 'Message the whole room' : TITLES[dialog.kind]
  const who = dialog.kind === 'message' && dialog.toRoom ? 'every candidate in this exam' : `${selected.length} selected candidate${selected.length === 1 ? '' : 's'}`

  return (
    <Modal title={title} onClose={onClose}>
      <p className="st-faint">For {who}.</p>
      {dialog.kind === 'Pause' && <ReasonForm submitLabel="Submit event" isBusy={isBusy} onSubmit={(reason) => run({ action: 'Pause', reason }, 'Paused')} note="Candidates see a message that the test is paused. Their time stops; it is given back when you resume." />}
      {dialog.kind === 'Resume' && <ReasonForm submitLabel="Resume" isBusy={isBusy} onSubmit={(reason) => run({ action: 'Resume', reason }, 'Resumed')} />}
      {dialog.kind === 'AddIncidentTime' && <MinutesForm label="Minutes to add" submitLabel="Update" initialMinutes={dialog.minutes} initialReason={dialog.reason} isBusy={isBusy} onSubmit={(minutes, reason) => run({ action: 'AddIncidentTime', minutes, reason }, `Added ${minutes} min`)} note="Incident time is for time lost on the day. Special-arrangement extra time is set on the candidate before the exam." />}
      {dialog.kind === 'RewindAudio' && (
        <MinutesForm
          label="Rewind by (minutes)"
          submitLabel="Rewind now"
          isBusy={isBusy}
          note="At most the minutes already played, and not while a candidate is paused. Then add the same minutes as incident extra time."
          onSubmit={(minutes, reason) => run({ action: 'RewindAudio', minutes, reason }, `Rewound ${minutes} min`, (outcome) => (outcome.applied > 0 ? { kind: 'AddIncidentTime', minutes, reason } : undefined))}
        />
      )}
      {dialog.kind === 'EditEndTime' && <EndTimeForm selected={selected} isBusy={isBusy} onSubmit={(request, what) => run(request, what)} />}
      {dialog.kind === 'message' && <MessageForm examId={examId} candidateIds={dialog.toRoom ? [] : ids} onSent={(count) => onDone({ text: `Message sent to ${dialog.toRoom ? 'the room' : `${count} candidate${count === 1 ? '' : 's'}`}.`, skipped: [] })} />}
      <ErrorBox error={error} />
    </Modal>
  )
}

function ReasonSelect({ value, onChange }: { value: string; onChange: (value: string) => void }) {
  return (
    <label className="st-field">
      Reason
      <select value={value} onChange={(e) => onChange(e.target.value)}>
        {REASONS.map((r) => (
          <option key={r}>{r}</option>
        ))}
      </select>
    </label>
  )
}

function ReasonForm({ submitLabel, isBusy, onSubmit, note }: { submitLabel: string; isBusy: boolean; onSubmit: (reason: string) => void; note?: string }) {
  const [reason, setReason] = useState(REASONS[0])
  const submit = (event: FormEvent) => {
    event.preventDefault()
    onSubmit(reason)
  }

  return (
    <form className="st-form" onSubmit={submit}>
      <ReasonSelect value={reason} onChange={setReason} />
      {note && <p className="st-note">{note}</p>}
      <div className="st-form-actions">
        <button type="submit" className="st-btn st-btn--primary" disabled={isBusy}>
          {submitLabel}
        </button>
      </div>
    </form>
  )
}

function MinutesForm(props: { label: string; submitLabel: string; isBusy: boolean; onSubmit: (minutes: number, reason: string) => void; note?: string; initialMinutes?: number; initialReason?: string }) {
  const { label, submitLabel, isBusy, onSubmit, note, initialMinutes, initialReason } = props
  const [minutes, setMinutes] = useState(initialMinutes ?? 5)
  const [reason, setReason] = useState(initialReason ?? REASONS[0])
  const submit = (event: FormEvent) => {
    event.preventDefault()
    onSubmit(minutes, reason)
  }

  return (
    <form className="st-form" onSubmit={submit}>
      <label className="st-field">
        {label}
        <input type="number" min={1} max={180} value={minutes} onChange={(e) => setMinutes(Number(e.target.value))} required />
      </label>
      <ReasonSelect value={reason} onChange={setReason} />
      {note && <p className="st-note">{note}</p>}
      <div className="st-form-actions">
        <button type="submit" className="st-btn st-btn--primary" disabled={isBusy || minutes < 1}>
          {submitLabel}
        </button>
      </div>
    </form>
  )
}

type EndChoice = 'EndNow' | 'EndAt' | 'EndIn' | 'UseTestEndTime'

function EndTimeForm({ selected, isBusy, onSubmit }: { selected: MonitorRow[]; isBusy: boolean; onSubmit: (request: Omit<MonitorActionRequest, 'candidateIds'>, what: string) => void }) {
  const [choice, setChoice] = useState<EndChoice>('EndIn')
  const [minutes, setMinutes] = useState(10)
  const [time, setTime] = useState('')
  const [disregard, setDisregard] = useState(false)
  const hasExtraTime = selected.some((r) => r.plannedExtraSeconds > 0 || r.incidentExtraSeconds > 0)

  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (choice === 'EndNow') {
      onSubmit({ action: 'EndNow', disregardExtraTime: disregard }, 'Ended')
    } else if (choice === 'EndIn') {
      onSubmit({ action: 'EndIn', minutes }, `Ends in ${minutes} min`)
    } else if (choice === 'EndAt') {
      const [h, m] = time.split(':').map(Number)
      const at = new Date()
      at.setHours(h, m, 0, 0)
      onSubmit({ action: 'EndAt', endAt: at.toISOString() }, `Ends at ${time}`)
    } else {
      onSubmit({ action: 'UseTestEndTime' }, 'Test end time restored')
    }
  }

  const option = (value: EndChoice, label: string) => (
    <label className="st-radio">
      <input type="radio" name="end" checked={choice === value} onChange={() => setChoice(value)} /> {label}
    </label>
  )

  return (
    <form className="st-form" onSubmit={submit}>
      {option('EndIn', 'End in')}
      {choice === 'EndIn' && (
        <label className="st-field st-indent">
          Minutes from now
          <input type="number" min={1} max={180} value={minutes} onChange={(e) => setMinutes(Number(e.target.value))} />
        </label>
      )}
      {option('EndAt', 'End at')}
      {choice === 'EndAt' && (
        <label className="st-field st-indent">
          Time today
          <input type="time" value={time} onChange={(e) => setTime(e.target.value)} required />
        </label>
      )}
      {option('EndNow', 'End now')}
      {choice === 'EndNow' && (
        <div className="st-indent">
          <p className="st-note st-note--warn">End now cannot be undone at section level.</p>
          {hasExtraTime && (
            <label className="st-radio">
              <input type="checkbox" checked={disregard} onChange={(e) => setDisregard(e.target.checked)} /> Disregard extra time (otherwise candidates keep their extra and incident time)
            </label>
          )}
        </div>
      )}
      {option('UseTestEndTime', 'Use test end time (undo End at / End in)')}
      <div className="st-form-actions">
        {choice === 'EndNow' ? (
          <ConfirmButton type="submit" label="End now" confirmLabel="Click again to end now" disabled={isBusy} onConfirm={() => undefined} />
        ) : (
          <button type="submit" className="st-btn st-btn--primary" disabled={isBusy}>
            Apply
          </button>
        )}
      </div>
    </form>
  )
}

function MessageForm({ examId, candidateIds, onSent }: { examId: number; candidateIds: number[]; onSent: (count: number) => void }) {
  const [text, setText] = useState('')
  const [error, setError] = useState<unknown>(null)
  const submit = async (event: FormEvent) => {
    event.preventDefault()
    try {
      await staffApi.sendMessage(examId, text, candidateIds)
      onSent(candidateIds.length)
    } catch (e) {
      setError(e)
    }
  }

  return (
    <form className="st-form" onSubmit={submit}>
      <label className="st-field">
        Message
        <textarea value={text} onChange={(e) => setText(e.target.value)} rows={3} maxLength={500} required placeholder="You have 10 minutes left." />
      </label>
      <p className="st-faint">Candidates see it under the bell icon at the top of their screen.</p>
      <ErrorBox error={error} />
      <div className="st-form-actions">
        <button type="submit" className="st-btn st-btn--primary" disabled={text.trim() === ''}>
          Send
        </button>
      </div>
    </form>
  )
}
