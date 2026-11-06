import { render, screen, waitFor } from '@testing-library/react'
import { candidateApi } from '../api/client'
import type { AttemptState, CandidateProfile, CandidateSection, Paper } from '../api/types'
import { ExamScreen } from './ExamScreen'

vi.mock('../api/live', () => ({ useLive: () => false }))

const profile: CandidateProfile = { candidateNumber: '0001', fullName: 'An Nguyen', examName: 'A2 Key for Schools', pinVerified: true, textSize: 'Regular', contrast: 'BlackOnWhite' }
const section: CandidateSection = { id: 6, orderNo: 1, title: 'Reading & Writing', durationMinutes: 60, status: 'NotStarted' }
const state = (status: string) => ({ status, remainingSeconds: 3_000, audioPositionMs: null, answers: {}, selections: {}, messages: [] }) as unknown as AttemptState
const paper = { sectionId: 6, title: 'Reading & Writing', durationMinutes: 60, parts: [] } as unknown as Paper

/** Review C4: the questions are asked for only after the section has been started, so nobody reads them with the clock stopped. */
describe('ExamScreen and the paper', () => {
  afterEach(() => vi.restoreAllMocks())

  it('asks for the paper only once the attempt has started', async () => {
    let start: (value: AttemptState) => void = () => {}
    vi.spyOn(candidateApi, 'startAttempt').mockReturnValue(new Promise<AttemptState>((resolve) => (start = resolve)))
    vi.spyOn(candidateApi, 'heartbeat').mockResolvedValue(state('InProgress'))
    const getPaper = vi.spyOn(candidateApi, 'paper').mockResolvedValue(paper)
    render(<ExamScreen profile={profile} section={section} onFinished={() => {}} onSignedOut={() => {}} />)

    await new Promise((resolve) => setTimeout(resolve, 50))
    expect(getPaper).not.toHaveBeenCalled()

    start(state('InProgress'))
    await waitFor(() => expect(getPaper).toHaveBeenCalledWith(6))
  })

  it('never asks for the paper of a section that has already ended, and closes it', async () => {
    vi.spyOn(candidateApi, 'startAttempt').mockResolvedValue(state('Submitted'))
    const getPaper = vi.spyOn(candidateApi, 'paper').mockResolvedValue(paper)
    const onFinished = vi.fn()
    render(<ExamScreen profile={profile} section={section} onFinished={onFinished} onSignedOut={() => {}} />)

    await waitFor(() => expect(onFinished).toHaveBeenCalled())
    expect(getPaper).not.toHaveBeenCalled()
    expect(screen.queryByText(/could not be loaded/)).not.toBeInTheDocument()
  })
})
