import { render, screen } from '@testing-library/react'
import { staffApi, type CandidateResult } from '../api'
import { ResultsPage } from './ResultsPage'

const candidate = (id: number, examiner: string | null, grading: string): CandidateResult => ({
  candidateId: id,
  candidateNumber: String(id).padStart(4, '0'),
  fullName: `Candidate ${id}`,
  markingCode: examiner ? `CODE0${id}` : null,
  examiner,
  sections: [{ sectionId: 1, title: 'Reading & Writing', status: 'Submitted', gradingStatus: grading, rawScore: 10, maxScore: 60, attemptId: id }],
})

describe('ResultsPage warns about scripts without an examiner (B11)', () => {
  beforeEach(() => {
    vi.spyOn(staffApi, 'exam').mockResolvedValue({ id: 1, name: 'Mock' } as never)
  })

  afterEach(() => vi.restoreAllMocks())

  it('counts the finished Writing scripts nobody holds', async () => {
    vi.spyOn(staffApi, 'results').mockResolvedValue([candidate(1, 'Ms Hoa', 'NeedsManualMarking'), candidate(2, null, 'NeedsManualMarking'), candidate(3, null, 'NeedsManualMarking')])

    render(<ResultsPage examId={1} />)

    expect(await screen.findByRole('alert')).toHaveTextContent('2 Writing scripts have no examiner')
  })

  it('also counts scripts whose examiner can no longer mark them (switched off, not an examiner, off the session)', async () => {
    vi.spyOn(staffApi, 'results').mockResolvedValue([
      { ...candidate(1, 'Ms Hoa', 'NeedsManualMarking'), examinerUnavailable: true },
      candidate(2, null, 'NeedsManualMarking'),
      { ...candidate(3, 'Ms Hoa', 'Final'), examinerUnavailable: true },
      candidate(4, 'Mr Nam', 'NeedsManualMarking'),
    ])

    render(<ResultsPage examId={1} />)

    expect(await screen.findByRole('alert')).toHaveTextContent('2 Writing scripts have no examiner who can mark them')
    // Candidate 3 is Final: nothing is left to mark, so no note beside the examiner.
    expect(screen.getAllByText(/(can no longer mark)/)).toHaveLength(1)
  })

  it('shows no warning when every script has an examiner', async () => {
    vi.spyOn(staffApi, 'results').mockResolvedValue([candidate(1, 'Ms Hoa', 'NeedsManualMarking'), candidate(2, 'Ms Hoa', 'Final')])

    render(<ResultsPage examId={1} />)

    await screen.findByText('Candidate 1')
    expect(screen.queryByRole('alert')).toBeNull()
  })
})
