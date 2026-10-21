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
  // Sections with mixed papers (VE-67) also get the version each candidate sat.
  const mixed = sections.map((_, i) => results.some((r) => r.sections[i]?.paperVersion))
  const header = [quote('Candidate number'), quote('Name'), ...sections.flatMap((s, i) => [quote(s.title), quote(`${s.title} max`), ...(mixed[i] ? [quote(`${s.title} version`)] : [])])]
  const rows = results.map((r) => [
    quote(r.candidateNumber),
    quote(r.fullName),
    ...r.sections.flatMap((s, i) => [cell(s.rawScore), cell(s.maxScore), ...(mixed[i] ? [cell(s.paperVersion ?? null)] : [])]),
  ])
  return String.fromCharCode(0xfeff) + [header, ...rows].map((row) => row.join(',')).join('\r\n')
}
