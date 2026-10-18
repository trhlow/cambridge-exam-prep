import type { MonitorRow } from './api'

/** The buttons of the action bar; Edit End Time covers End now / End at / End in / Use test end time. */
export type ActionButton = 'Pause' | 'Resume' | 'AddIncidentTime' | 'EditEndTime' | 'RewindAudio'

const isActive = (row: MonitorRow) => row.status === 'InProgress' || row.status === 'Paused'

/**
 * How many of the selected candidates each action would apply to, under the same rules as the server (and Inspera):
 * pause only online candidates who are working, rewind only a started recording while not paused.
 */
export function actionAvailability(selected: MonitorRow[], hasAudio: boolean): Record<ActionButton, number> {
  const count = (test: (row: MonitorRow) => boolean) => selected.filter(test).length
  return {
    Pause: count((r) => r.status === 'InProgress' && r.isOnline),
    Resume: count((r) => r.status === 'Paused'),
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
