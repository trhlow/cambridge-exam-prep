import { fireEvent, render, screen } from '@testing-library/react'
import App from './App'
import { candidateApi } from './api/client'
import type { CandidateProfile } from './api/types'

const profile: CandidateProfile = { candidateNumber: '0001', fullName: 'An Nguyen', examName: 'A2 Key for Schools', pinVerified: true, textSize: 'Regular', contrast: 'BlackOnWhite' }

describe('App', () => {
  beforeEach(() => {
    sessionStorage.clear()
    localStorage.clear()
  })
  afterEach(() => vi.restoreAllMocks())

  it('Exit signs out and removes the notes and highlights of every section from this computer (B13)', async () => {
    vi.spyOn(candidateApi, 'me').mockResolvedValue(profile)
    vi.spyOn(candidateApi, 'sections').mockResolvedValue([{ id: 1, orderNo: 1, title: 'Reading & Writing', durationMinutes: 60, status: 'Submitted' }])
    const logout = vi.spyOn(candidateApi, 'logout').mockResolvedValue(undefined)
    sessionStorage.setItem('victory.annotations.1', '{"notes":[{"text":"mine"}]}')
    sessionStorage.setItem('victory.annotations.2', '{"highlights":[1]}')

    render(<App />)
    fireEvent.click(await screen.findByRole('button', { name: 'Exit' }))

    expect(await screen.findByText(/You have finished the exam/)).toBeInTheDocument()
    expect(logout).toHaveBeenCalled()
    expect(sessionStorage.getItem('victory.annotations.1')).toBeNull()
    expect(sessionStorage.getItem('victory.annotations.2')).toBeNull()
  })

  it("Exit also removes the unsent answers this candidate left on the computer, and no one else's (final review 6)", async () => {
    vi.spyOn(candidateApi, 'me').mockResolvedValue(profile)
    vi.spyOn(candidateApi, 'sections').mockResolvedValue([{ id: 1, orderNo: 1, title: 'Reading & Writing', durationMinutes: 60, status: 'Submitted' }])
    vi.spyOn(candidateApi, 'logout').mockResolvedValue(undefined)
    localStorage.setItem('exam-pending:A2 Key for Schools/0001/1', '[{"questionNumber":1,"value":"mine"}]')
    localStorage.setItem('exam-pending:A2 Key for Schools/0001/2', '[{"questionNumber":2,"value":"mine"}]')
    localStorage.setItem('exam-pending:A2 Key for Schools/00011/1', '[{"questionNumber":1,"value":"theirs"}]')

    render(<App />)
    fireEvent.click(await screen.findByRole('button', { name: 'Exit' }))

    expect(await screen.findByText(/You have finished the exam/)).toBeInTheDocument()
    expect(localStorage.getItem('exam-pending:A2 Key for Schools/0001/1')).toBeNull()
    expect(localStorage.getItem('exam-pending:A2 Key for Schools/0001/2')).toBeNull()
    expect(localStorage.getItem('exam-pending:A2 Key for Schools/00011/1')).not.toBeNull()
  })
})
