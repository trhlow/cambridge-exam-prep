import { fireEvent, render, screen } from '@testing-library/react'
import { candidateApi } from '../api/client'
import type { CandidateProfile } from '../api/types'
import { InstructionsScreen, LoggedInScreen, PinScreen, ReadinessScreen, SectionsScreen } from './SignInScreens'

const profile: CandidateProfile = { candidateNumber: '0001', fullName: 'An Nguyen', examName: 'A2 Key for Schools', pinVerified: true, textSize: 'Regular', contrast: 'BlackOnWhite' }
const noop = () => {}

// The steps follow the candidate instructions of the Cambridge Exam Day Booklet for Digital exams (2026, pp. 11–13).
describe('candidate steps before a test', () => {
  beforeEach(() => localStorage.clear())
  afterEach(() => vi.restoreAllMocks())

  it('asks for Next after Login, then the PIN with "Unlock and Select", then Next', async () => {
    const onNext = vi.fn()
    render(<LoggedInScreen profile={{ ...profile, pinVerified: false }} onNext={onNext} />)
    fireEvent.click(screen.getByRole('button', { name: 'Next' }))
    expect(onNext).toHaveBeenCalled()

    vi.spyOn(candidateApi, 'enterPin').mockResolvedValue(profile)
    const onVerified = vi.fn()
    render(<PinScreen profile={{ ...profile, pinVerified: false }} onVerified={onVerified} />)
    fireEvent.change(screen.getByLabelText('PIN code'), { target: { value: '4821' } })
    fireEvent.click(screen.getByRole('button', { name: 'Unlock and Select' }))
    expect(await screen.findByText('A2 Key for Schools is unlocked.')).toBeInTheDocument()
    fireEvent.click(screen.getAllByRole('button', { name: 'Next' }).at(-1)!)
    expect(onVerified).toHaveBeenCalledWith(profile)
  })

  it('shows the name and number under the candidate icon, closed with the cross', () => {
    render(<LoggedInScreen profile={profile} onNext={noop} />)
    fireEvent.click(screen.getByRole('button', { name: 'Candidate' }))
    expect(screen.getByRole('dialog', { name: 'Candidate' })).toHaveTextContent('An Nguyen0001')
    fireEvent.click(screen.getByRole('button', { name: 'Close' }))
    expect(screen.queryByRole('dialog', { name: 'Candidate' })).not.toBeInTheDocument()
  })

  it('keeps the components closed until the readiness check is confirmed, and Listening until Reading & Writing ends', async () => {
    vi.spyOn(candidateApi, 'sections').mockResolvedValue([
      { id: 1, orderNo: 1, title: 'Reading & Writing', durationMinutes: 60, status: 'NotStarted' },
      { id: 2, orderNo: 2, title: 'Listening', durationMinutes: 30, status: 'Locked' },
    ])

    const { unmount } = render(<SectionsScreen profile={profile} onStart={noop} onReadiness={noop} onExit={noop} />)
    expect(await screen.findByRole('button', { name: 'Open Reading & Writing' })).toBeDisabled()
    unmount()

    const onDone = vi.fn()
    render(<ReadinessScreen profile={profile} onDone={onDone} />)
    expect(screen.getByRole('button', { name: 'Continue' })).toBeDisabled()
    fireEvent.click(screen.getByRole('button', { name: 'Confirm' }))
    fireEvent.click(screen.getByRole('button', { name: 'Continue' }))
    expect(onDone).toHaveBeenCalled()

    render(<SectionsScreen profile={profile} onStart={noop} onReadiness={noop} onExit={noop} />)
    expect(await screen.findByRole('button', { name: 'Open Reading & Writing' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Listening is locked' })).toBeDisabled()
    expect(screen.getByText('Opens when Reading & Writing has finished')).toBeInTheDocument()
  })

  it('starts a component only after "I confirm"', () => {
    const onStart = vi.fn()
    render(<InstructionsScreen profile={profile} section={{ id: 2, orderNo: 2, title: 'Listening', durationMinutes: 30, status: 'NotStarted' }} onStart={onStart} onBack={noop} />)
    expect(screen.getByText('You cannot leave the exam room at any time during the Listening test.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Start Listening' })).toBeDisabled()
    fireEvent.click(screen.getByRole('button', { name: 'I confirm' }))
    fireEvent.click(screen.getByRole('button', { name: 'Start Listening' }))
    expect(onStart).toHaveBeenCalled()
  })

  it('offers Exit once every component is submitted', async () => {
    localStorage.setItem('victory.readiness.A2 Key for Schools/0001', 'done')
    vi.spyOn(candidateApi, 'sections').mockResolvedValue([
      { id: 1, orderNo: 1, title: 'Reading & Writing', durationMinutes: 60, status: 'Submitted' },
      { id: 2, orderNo: 2, title: 'Listening', durationMinutes: 30, status: 'Submitted' },
    ])
    const onExit = vi.fn()
    render(<SectionsScreen profile={profile} onStart={noop} onReadiness={noop} onExit={onExit} notice="The test has now finished. Your answers have been saved." />)
    fireEvent.click(await screen.findByRole('button', { name: 'Exit' }))
    expect(onExit).toHaveBeenCalled()
  })
})
