import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
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

describe('ResultsPage on the Cambridge English Scale', () => {
  beforeEach(() => {
    vi.spyOn(staffApi, 'exam').mockResolvedValue({ id: 1, name: 'Mock' } as never)
  })

  afterEach(() => vi.restoreAllMocks())

  it('shows each skill, the overall score and grade, and saves the Speaking marks', async () => {
    const scaled: CandidateResult = {
      ...candidate(1, 'Ms Hoa', 'Final'),
      skills: [
        { skill: 'Reading', rawScore: 28, maxScore: 30, scale: 140 },
        { skill: 'Writing', rawScore: 22, maxScore: 30, scale: 130 },
        { skill: 'Listening', rawScore: 5, maxScore: 25, scale: null },
        { skill: 'Speaking', rawScore: null, maxScore: 45, scale: null },
      ],
    }
    const results = vi.spyOn(staffApi, 'results').mockResolvedValue([scaled])
    const setSpeaking = vi.spyOn(staffApi, 'setSpeaking').mockResolvedValue(undefined)

    render(<ResultsPage examId={1} />)

    expect(await screen.findByText('R 140 · W 130 · L < · S –')).toBeInTheDocument()
    const input = screen.getByRole('spinbutton', { name: 'Speaking marks of Candidate 1' })
    await userEvent.type(input, '27.5{Enter}')
    expect(setSpeaking).toHaveBeenCalledWith(1, 1, 27.5)
    await waitFor(() => expect(results).toHaveBeenCalledTimes(2))
  })
})

