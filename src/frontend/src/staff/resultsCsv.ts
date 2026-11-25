import type { CandidateResult } from './api'

/** What a section's grading status means for whoever reads the score. */
export const GRADING_TEXT: Record<string, string> = {
  NotGraded: 'Not marked yet',
  AutoGraded: 'Marked',
  NeedsManualMarking: 'Writing to mark',
  Final: 'Final',
}

/** A leading = + - @ (or tab / carriage return) makes Excel run a cell as a formula. */
const FORMULA_START = /^[=+\-@\t\r]/

/** Quoted, with a formula-like start neutralised by a leading apostrophe. */
export const quote = (text: string) => `"${(FORMULA_START.test(text) ? `'${text}` : text).replace(/"/g, '""')}"`
const cell = (value: number | null) => (value === null ? '' : String(value))

/**
 * Results as CSV for Excel: a byte-order mark so Vietnamese names open correctly, text quoted, one score and
 * maximum column per section, and a status column so a score that still lacks its Writing marks is not read as final.
 */
export function resultsCsv(results: CandidateResult[]): string {
  const sections = results[0]?.sections ?? []
  // Sections with mixed papers (VE-67) also get the version each candidate sat.
  const mixed = sections.map((_, i) => results.some((r) => r.sections[i]?.paperVersion))
  const header = [quote('Candidate number'), quote('Name'), quote('Writing script'), quote('Examiner'), ...sections.flatMap((s, i) => [quote(s.title), quote(`${s.title} max`), quote(`${s.title} status`), ...(mixed[i] ? [quote(`${s.title} version`)] : [])])]
  const rows = results.map((r) => [
    quote(r.candidateNumber),
    quote(r.fullName),
    quote(r.markingCode ?? ''),
    quote(r.examiner ?? ''),
    ...r.sections.flatMap((s, i) => [cell(s.rawScore), cell(s.maxScore), quote(s.status === 'NotStarted' ? '' : (GRADING_TEXT[s.gradingStatus] ?? s.gradingStatus)), ...(mixed[i] ? [cell(s.paperVersion ?? null)] : [])]),
  ])
  return String.fromCharCode(0xfeff) + [header, ...rows].map((row) => row.join(',')).join('\r\n')
}

/** Saves CSV text as a file named after the exam, e.g. `results-A2-Key-June.csv`. */
export function downloadCsv(csv: string, prefix: string, examName: string) {
  const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }))
  const link = document.createElement('a')
  link.href = url
  link.download = `${prefix}-${examName.replace(/[^\p{L}\p{N}]+/gu, '-')}.csv`
  link.click()
  URL.revokeObjectURL(url)
}
