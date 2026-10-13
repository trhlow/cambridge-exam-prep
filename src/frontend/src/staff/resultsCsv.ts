import type { CandidateResult } from './api'

/** A leading = + - @ (or tab / carriage return) makes Excel run a cell as a formula. */
const FORMULA_START = /^[=+\-@\t\r]/

/** Quoted, with a formula-like start neutralised by a leading apostrophe. */
const quote = (text: string) => `"${(FORMULA_START.test(text) ? `'${text}` : text).replace(/"/g, '""')}"`
const cell = (value: number | null) => (value === null ? '' : String(value))

/**
 * Results as CSV for Excel: a byte-order mark so Vietnamese names open correctly, text quoted, one score and
 * maximum column per section. Writing scores appear once a teacher has marked them.
 */
export function resultsCsv(results: CandidateResult[]): string {
  const sections = results[0]?.sections ?? []
  const header = [quote('Candidate number'), quote('Name'), ...sections.flatMap((s) => [quote(s.title), quote(`${s.title} max`)])]
  const rows = results.map((r) => [quote(r.candidateNumber), quote(r.fullName), ...r.sections.flatMap((s) => [cell(s.rawScore), cell(s.maxScore)])])
  return String.fromCharCode(0xfeff) + [header, ...rows].map((row) => row.join(',')).join('\r\n')
}
