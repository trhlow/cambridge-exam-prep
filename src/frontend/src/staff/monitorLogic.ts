import type { ActionOutcome, MonitorRow } from './api'

/** The buttons of the action bar; Edit End Time covers End now / End at / End in / Use test end time. */
export type ActionButton = 'Pause' | 'Resume' | 'AddIncidentTime' | 'EditEndTime' | 'RewindAudio'

const isActive = (row: MonitorRow) => row.status === 'InProgress' || row.status === 'Paused'

/**
 * How many of the selected candidates each action would apply to, under the same rules as the server (and Inspera):
 * pause only online candidates who are working, resume only online paused ones, rewind only a started recording while not paused.
 */
export function actionAvailability(selected: MonitorRow[], hasAudio: boolean): Record<ActionButton, number> {
  const count = (test: (row: MonitorRow) => boolean) => selected.filter(test).length
  return {
    Pause: count((r) => r.status === 'InProgress' && r.isOnline),
    Resume: count((r) => r.status === 'Paused' && r.isOnline),
    AddIncidentTime: count(isActive),
    EditEndTime: count(isActive),
    RewindAudio: hasAudio ? count((r) => r.status === 'InProgress' && r.audioPositionSeconds !== null) : 0,
  }
}

/** 1:05, 1:02:05; a dash when there is no clock. */
export function clockText(seconds: number | null): string {
  if (seconds === null) {
    return '–'
  }

  const h = Math.floor(seconds / 3600)
  const m = Math.floor((seconds % 3600) / 60)
  const s = seconds % 60
  const mm = h > 0 ? String(m).padStart(2, '0') : String(m)
  return `${h > 0 ? `${h}:` : ''}${mm}:${String(s).padStart(2, '0')}`
}

/** "+5 min", or empty when there is none. */
export function extraTimeText(seconds: number): string {
  return seconds > 0 ? `+${Math.round(seconds / 60)} min` : ''
}

/**
 * Time left and recording position `elapsedSeconds` after the page last heard from the server: a working candidate's
 * clock runs on between refreshes (VE-13 refreshes on changes, not every few seconds); a paused one stands still.
 */
export function rowAt(row: MonitorRow, elapsedSeconds: number): MonitorRow {
  if (row.status !== 'InProgress' || elapsedSeconds <= 0) {
    return row
  }

  return {
    ...row,
    remainingSeconds: row.remainingSeconds === null ? null : Math.max(0, row.remainingSeconds - elapsedSeconds),
    audioPositionSeconds: row.audioPositionSeconds === null ? null : row.audioPositionSeconds + elapsedSeconds,
  }
}

/** The candidates an action really changed: the ones asked for, less the ones the server skipped. */
export function appliedIds(ids: number[], outcome: ActionOutcome): number[] {
  const skipped = new Set(outcome.skipped.map((s) => s.candidateId))
  return ids.filter((id) => !skipped.has(id))
}

/**
 * What End now did. Standard time ends at once, but a candidate with planned or incident extra time keeps working on
 * it unless the invigilator disregarded it; a paused candidate with extra time is resumed to do so.
 */
export function endNowText(selected: MonitorRow[], outcome: ActionOutcome, disregardExtraTime: boolean): string {
  const skipped = new Set(outcome.skipped.map((s) => s.candidateId))
  const changed = selected.filter((r) => !skipped.has(r.candidateId))
  const keeps = disregardExtraTime ? [] : changed.filter((r) => r.plannedExtraSeconds + r.incidentExtraSeconds > 0)
  const ended = changed.length - keeps.length
  const people = (n: number) => `${n} candidate${n === 1 ? '' : 's'}`
  const parts = [`End now: ${people(ended)} ended`]
  if (keeps.length > 0) {
    const resumed = keeps.filter((r) => r.status === 'Paused').length
    const tail = resumed > 0 ? ` (${resumed} ${resumed === 1 ? 'was' : 'were'} paused and ${resumed === 1 ? 'has' : 'have'} been resumed)` : ''
    parts.push(`${keeps.length} ${keeps.length === 1 ? 'keeps' : 'keep'} working on their extra time${tail}`)
  }

  return `${parts.join('; ')}.`
}
