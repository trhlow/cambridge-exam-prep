import type { CandidateResult } from './api'
import { resultsCsv } from './resultsCsv'

const result = (candidateNumber: string, fullName: string, rw: [string, number | null, number | null], listening: [string, number | null, number | null]): CandidateResult => ({
  candidateId: Number(candidateNumber),
  candidateNumber,
  fullName,
  sections: [
    { sectionId: 1, title: 'Reading & Writing', status: rw[0], gradingStatus: 'NeedsManualMarking', rawScore: rw[1], maxScore: rw[2], attemptId: 1 },
    { sectionId: 2, title: 'Listening', status: listening[0], gradingStatus: 'AutoGraded', rawScore: listening[1], maxScore: listening[2], attemptId: 2 },
  ],
})

describe('resultsCsv', () => {
  it('writes one row per candidate with each section score, for Excel (BOM, quoted text)', () => {
    const csv = resultsCsv([result('0001', 'An Nguyễn', ['Submitted', 21, 60], ['Submitted', 18, 25]), result('0002', 'Bình "B" Trần', ['NotStarted', null, null], ['NotStarted', null, null])])

    expect(csv.startsWith(String.fromCharCode(0xfeff))).toBe(true)
    expect(csv.slice(1).split('\r\n')).toEqual([
      '"Candidate number","Name","Reading & Writing","Reading & Writing max","Listening","Listening max"',
      '"0001","An Nguyễn",21,60,18,25',
      '"0002","Bình ""B"" Trần",,,,',
    ])
  })

  it('adds the version each candidate sat for a section with mixed papers (VE-67)', () => {
    const mixed = result('0004', 'Chi Lê', ['Submitted', 30, 60], ['Submitted', 20, 25])
    mixed.sections[0].paperVersion = 3

    expect(resultsCsv([mixed]).slice(1).split(String.fromCharCode(13, 10))).toEqual([
      '"Candidate number","Name","Reading & Writing","Reading & Writing max","Reading & Writing version","Listening","Listening max"',
      '"0004","Chi Lê",30,60,3,20,25',
    ])
  })

  it('neutralises text that Excel would run as a formula', () => {
    const csv = resultsCsv([result('0003', '=HYPERLINK("http://evil")', ['NotStarted', null, null], ['NotStarted', null, null])])

    expect(csv).toContain(`"'=HYPERLINK(""http://evil"")"`)
  })
})
