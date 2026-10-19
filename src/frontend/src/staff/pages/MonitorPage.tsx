import { useEffect, useMemo, useState } from 'react'
import { useLive } from '../../api/live'
import { staffApi, type ActionOutcome, type MonitorRow, type MonitorView } from '../api'
import { actionAvailability, clockText, extraTimeText, rowAt, type ActionButton } from '../monitorLogic'
import { Link } from '../Link'
import { Badge, ConfirmButton, ErrorBox, PageHeader } from '../ui'
import { formatTime } from '../format'
import { ActionDialog, type DialogKind } from './MonitorDialogs'
import { WarningsDialog } from './WarningsDialog'

/** Without the live connection the page polls a little faster than the candidates' 5 s heartbeat. */
const REFRESH_MS = 3_000

/** With it (VE-13) the server says when something changed; a slow poll only guards against a missed message. */
const LIVE_REFRESH_MS = 20_000

const BUTTON_LABELS: Record<ActionButton, string> = {
  Pause: 'Pause',
  Resume: 'Resume',
  AddIncidentTime: 'Incident extra time',
  EditEndTime: 'Edit end time',
  RewindAudio: 'Rewind audio',
}

/**
 * VE-14…VE-17: Inspera's child-test monitoring page. Every candidate of one section with status, connection and
 * time left; select candidates, then pause/resume, add incident time, edit the end time or rewind the recording.
 */
export function MonitorPage({ examId, sectionId }: { examId: number; sectionId: number }) {
  const [view, setView] = useState<MonitorView | null>(null)
  const [error, setError] = useState<unknown>(null)
  const [selected, setSelected] = useState<ReadonlySet<number>>(new Set())
  const [dialog, setDialog] = useState<DialogKind | null>(null)
  const [outcome, setOutcome] = useState<{ text: string; skipped: ActionOutcome['skipped'] } | null>(null)
  const [version, setVersion] = useState(0)
  const [warningsFor, setWarningsFor] = useState<{ candidateId: number | null; title: string } | null>(null)
  const [receivedAt, setReceivedAt] = useState(() => Date.now())
  const [now, setNow] = useState(() => Date.now())
  const isLive = useLive(
    '/hubs/staff',
    { sectionChanged: () => setVersion((v) => v + 1) },
    (connection) => connection.invoke('WatchSection', examId, sectionId),
    `${examId}/${sectionId}`,
  )

  useEffect(() => {
    let isCurrent = true
    const load = () =>
      staffApi.monitor(examId, sectionId).then(
        (next) => {
          if (isCurrent) {
            setView(next)
            setReceivedAt(Date.now())
            setError(null)
          }
        },
        (e: unknown) => isCurrent && setError(e),
      )
    void load()
    const timer = setInterval(load, isLive ? LIVE_REFRESH_MS : REFRESH_MS)
    return () => {
      isCurrent = false
      clearInterval(timer)
    }
  }, [examId, sectionId, version, isLive])

  useEffect(() => {
    const tick = setInterval(() => setNow(Date.now()), 1_000)
    return () => clearInterval(tick)
  }, [])

  const elapsed = Math.max(0, Math.floor((now - receivedAt) / 1_000))
  const rows = useMemo(() => (view?.candidates ?? []).map((r) => rowAt(r, elapsed)), [view, elapsed])
  const selectedRows = rows.filter((r) => selected.has(r.candidateId))
  const availability = actionAvailability(selectedRows, view?.hasAudio ?? false)

  if (!view) {
    return error ? <ErrorBox error={error} /> : <p className="st-empty">Loading…</p>
  }

  const toggle = (id: number) => setSelected((current) => {
    const next = new Set(current)
    if (next.has(id)) {
      next.delete(id)
    } else {
      next.add(id)
    }

    return next
  })
  const allSelected = rows.length > 0 && rows.every((r) => selected.has(r.candidateId))
  const toggleAll = () => setSelected(allSelected ? new Set() : new Set(rows.map((r) => r.candidateId)))
  const counts = countByStatus(rows)
  const totalWarnings = rows.reduce((sum, r) => sum + r.warnings, 0)
  const done = (result: { text: string; skipped: ActionOutcome['skipped'] }, next?: DialogKind) => {
    setOutcome(result)
    setDialog(next ?? null)
    setVersion((v) => v + 1)
  }
  const buttons: ActionButton[] = view.hasAudio ? ['Pause', 'Resume', 'AddIncidentTime', 'EditEndTime', 'RewindAudio'] : ['Pause', 'Resume', 'AddIncidentTime', 'EditEndTime']

  return (
    <>
      <PageHeader
        title={view.sectionTitle}
        meta={
          <>
            <Link to={`/staff/exams/${examId}`}>{view.examName}</Link> · {view.durationMinutes} minutes · {isLive ? 'live' : `refreshed ${formatTime(view.serverNow)}`}
          </>
        }
        actions={
          <>
            {view.startAnnouncedAt ? (
              <span className="st-faint">Start announced {formatTime(view.startAnnouncedAt)}</span>
            ) : (
              <ConfirmButton
                className="st-btn"
                label="Announce start"
                confirmLabel="Click again: the room may begin"
                onConfirm={() =>
                  void staffApi.announceStart(examId, sectionId).then(
                    (next) => {
                      setView(next)
                      setReceivedAt(Date.now())
                    },
                    setError,
                  )
                }
              />
            )}
            <button type="button" className="st-btn" onClick={() => setWarningsFor({ candidateId: null, title: 'Warnings in this section' })}>
              Warnings{totalWarnings > 0 ? ` (${totalWarnings})` : ''}
            </button>
            <button type="button" className="st-btn" onClick={() => setDialog({ kind: 'message', toRoom: true })}>
              Message the room
            </button>
          </>
        }
      />
      <div className="st-counts" aria-label="Candidates by status">
        <span>{rows.length} candidates</span>
        <span>{counts.InProgress} in progress</span>
        <span>{counts.Paused} paused</span>
        <span>{counts.Submitted} submitted</span>
        <span>{counts.NotStarted} not started</span>
        <span>{rows.filter((r) => r.status !== 'Submitted' && r.status !== 'NotStarted' && !r.isOnline).length} offline</span>
      </div>
      <ErrorBox error={error} />
      {outcome && <Outcome text={outcome.text} skipped={outcome.skipped} onClose={() => setOutcome(null)} />}

      <table className="st-table st-monitor">
        <thead>
          <tr>
            <th className="st-check">
              <input type="checkbox" checked={allSelected} onChange={toggleAll} aria-label="Select all candidates" />
            </th>
            <th>Candidate</th>
            <th>Status</th>
            <th>Connection</th>
            <th className="st-num">Time left</th>
            <th>Started</th>
            <th>Extra time</th>
            <th>Incident time</th>
            {view.hasAudio && <th className="st-num">Audio</th>}
            <th className="st-num">Answered</th>
            <th>Warnings</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <Row
              key={row.candidateId}
              row={row}
              hasAudio={view.hasAudio}
              isSelected={selected.has(row.candidateId)}
              onToggle={() => toggle(row.candidateId)}
              onWarnings={() => setWarningsFor({ candidateId: row.candidateId, title: `Warnings — ${row.fullName}` })}
            />
          ))}
        </tbody>
      </table>

      {selectedRows.length > 0 && (
        <div className="st-action-bar" role="toolbar" aria-label="Actions for the selected candidates">
          <span>
            <strong>{selectedRows.length}</strong> selected
          </span>
          {buttons.map((button) => (
            <button
              key={button}
              type="button"
              className="st-btn"
              disabled={availability[button] === 0}
              title={availability[button] === 0 ? 'Not possible for the selected candidates' : `Applies to ${availability[button]} of ${selectedRows.length}`}
              onClick={() => setDialog({ kind: button })}
            >
              {BUTTON_LABELS[button]}
            </button>
          ))}
          <button type="button" className="st-btn" onClick={() => setDialog({ kind: 'message', toRoom: false })}>
            Message
          </button>
          <button type="button" className="st-link" onClick={() => setSelected(new Set())}>
            Clear
          </button>
        </div>
      )}

      {warningsFor && (
        <WarningsDialog
          examId={examId}
          sectionId={sectionId}
          candidateId={warningsFor.candidateId}
          title={warningsFor.title}
          onClose={() => setWarningsFor(null)}
          onChanged={() => setVersion((v) => v + 1)}
        />
      )}
      {dialog && <ActionDialog key={JSON.stringify(dialog)} dialog={dialog} examId={examId} sectionId={sectionId} selected={selectedRows} onClose={() => setDialog(null)} onDone={done} />}
    </>
  )
}

function Row(props: { row: MonitorRow; hasAudio: boolean; isSelected: boolean; onToggle: () => void; onWarnings: () => void }) {
  const { row, hasAudio, isSelected, onToggle, onWarnings } = props
  const isRunning = row.status === 'InProgress' || row.status === 'Paused'
  return (
    <tr className={isSelected ? 'is-selected' : undefined}>
      <td className="st-check">
        <input type="checkbox" checked={isSelected} onChange={onToggle} aria-label={`Select ${row.fullName}`} />
      </td>
      <td>
        <strong>{row.fullName}</strong>
        <div className="st-faint st-mono">
          {row.candidateNumber} · {row.username}
        </div>
      </td>
      <td>
        <Badge value={row.status} />
        {row.submitReason === 'InvigilatorEndNow' && <div className="st-faint">Ended by invigilator</div>}
      </td>
      <td>{isRunning ? <span className={row.isOnline ? 'st-dot st-dot--on' : 'st-dot st-dot--off'}>{row.isOnline ? 'Online' : 'Offline'}</span> : '–'}</td>
      <td className="st-num st-mono">
        {isRunning ? clockText(row.remainingSeconds) : '–'}
        {row.endTimeEdited && isRunning && <div className="st-faint">end edited</div>}
      </td>
      <td>{formatTime(row.startedAt)}</td>
      <td>{extraTimeText(row.plannedExtraSeconds)}</td>
      <td>{extraTimeText(row.incidentExtraSeconds)}</td>
      {hasAudio && <td className="st-num st-mono">{row.audioPositionSeconds === null ? '–' : clockText(row.audioPositionSeconds)}</td>}
      <td className="st-num">{row.answered}</td>
      <td>
        {row.warnings > 0 && (
          <button type="button" className="st-warning" onClick={onWarnings} aria-label={`${row.warnings} warning${row.warnings === 1 ? '' : 's'} for ${row.fullName}`}>
            ⚠ {row.warnings}
          </button>
        )}
      </td>
    </tr>
  )
}

function Outcome({ text, skipped, onClose }: { text: string; skipped: ActionOutcome['skipped']; onClose: () => void }) {
  return (
    <div className={skipped.length > 0 ? 'st-outcome st-outcome--warn' : 'st-outcome'} role="status">
      <div>
        <strong>{text}</strong>
        {skipped.length > 0 && (
          <ul>
            {skipped.map((s) => (
              <li key={s.candidateId}>
                {s.fullName}: {s.reason}
              </li>
            ))}
          </ul>
        )}
      </div>
      <button type="button" className="st-link" onClick={onClose}>
        Dismiss
      </button>
    </div>
  )
}

function countByStatus(rows: MonitorRow[]) {
  const counts = { NotStarted: 0, InProgress: 0, Paused: 0, Submitted: 0 }
  rows.forEach((r) => {
    counts[r.status] += 1
  })
  return counts
}
