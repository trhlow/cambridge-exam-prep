/** One candidate read from the pasted list, ready to send as a NewCandidate. */
export interface ParsedRow {
  candidateNumber: string
  lastName: string
  firstName: string
  plannedExtraPercent: number
  seatNo: string | null
}

export interface ParsedCandidates {
  rows: ParsedRow[]
  errors: string[]
}

/** A header row is recognised by a first cell with no digit ("Candidate number", "Số báo danh"…). */
const isHeader = (cells: string[]) => !/\d/.test(cells[0] ?? '')

/**
 * Candidates pasted from a spreadsheet: one per line, columns candidate number, family name, given name, then
 * optionally extra time (%) and seat. Excel copies cells separated by tabs; CSV lines by commas.
 */
export function parseCandidateRows(text: string): ParsedCandidates {
  const rows: ParsedRow[] = []
  const errors: string[] = []
  const lines = text.split(/\r?\n/)

  lines.forEach((line, index) => {
    if (line.trim() === '') {
      return
    }

    const cells = line.split(line.includes('\t') ? '\t' : ',').map((cell) => cell.trim())
    if (index === 0 && isHeader(cells)) {
      return
    }

    const label = `Line ${index + 1}`
    const [candidateNumber, lastName, firstName, extra = '', seat = ''] = cells
    if (!candidateNumber || !lastName || !firstName) {
      errors.push(`${label}: expected candidate number, family name and given name.`)
      return
    }

    const plannedExtraPercent = extra === '' ? 0 : Number(extra)
    if (!Number.isInteger(plannedExtraPercent) || plannedExtraPercent < 0 || plannedExtraPercent > 100) {
      errors.push(`${label}: extra time must be a whole number from 0 to 100.`)
      return
    }

    rows.push({ candidateNumber, lastName, firstName, plannedExtraPercent, seatNo: seat === '' ? null : seat })
  })

  return { rows, errors }
}
