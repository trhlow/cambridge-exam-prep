import type { ActionLogRow } from './api'
import { formatTime } from './format'
import { quote } from './resultsCsv'

/** The invigilator's words for each logged action, as on the monitoring page. */
const LABELS: Record<string, string> = {
  Pause: 'Paused',
  Resume: 'Resumed',
  AddIncidentTime: 'Incident extra time',
  AddIncidentExtraTime: 'Incident extra time',
  EndNow: 'End now',
  EndAt: 'End at',
  EndIn: 'End in',
  EditEndTime: 'Edit end time',
  UseTestEndTime: 'Use test end time',
  RewindAudio: 'Audio rewound',
  SendMessage: 'Message',
  AnnounceStart: 'Start announced',
  EndAll: 'Whole test ended',
}

export const actionLabel = (row: ActionLogRow) => LABELS[row.action] ?? row.action

/** What the action changed, in a few words: minutes, the new end, the message and who it went to. */
export function actionDetail(row: ActionLogRow): string {
  const parts: string[] = []
  if (row.minutes !== null) {
    parts.push(row.action === 'RewindAudio' ? `${row.minutes} min back` : row.action === 'EndIn' ? `in ${row.minutes} min` : `+${row.minutes} min`)
  }
  if (row.endAt) parts.push(formatTime(row.endAt))
  if (row.disregardExtraTime) parts.push('extra time disregarded')
  if (row.wholeTest && row.action !== 'EndAll') parts.push('whole test')
  if (row.message !== null) {
    parts.push(`“${row.message}” to ${row.recipients ? `${row.recipients} candidate${row.recipients === 1 ? '' : 's'}` : 'the whole room'}`)
  }
  return parts.join(' · ')
}

/** Date and time to the second, on the computer's clock. */
export const logTime = (iso: string) =>
  new Date(iso).toLocaleString('en-GB', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', second: '2-digit' })

/** The log as CSV for Excel, with a byte-order mark for Vietnamese names and formula-like text neutralised. */
export function actionLogCsv(rows: ActionLogRow[]): string {
  const header = ['Time', 'Action', 'Details', 'Reason', 'Candidate number', 'Candidate', 'Section', 'By'].map(quote)
  const lines = rows.map((r) =>
    [logTime(r.at), actionLabel(r), actionDetail(r), r.reason ?? '', r.candidateNumber ?? '', r.candidate ?? '', r.section ?? '', r.by].map(quote),
  )
  return String.fromCharCode(0xfeff) + [header, ...lines].map((row) => row.join(',')).join('\r\n')
}
