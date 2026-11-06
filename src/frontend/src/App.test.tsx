import { fireEvent, render, screen } from '@testing-library/react'
import App from './App'
import { candidateApi } from './api/client'
import type { CandidateProfile } from './api/types'

const profile: CandidateProfile = { candidateNumber: '0001', fullName: 'An Nguyen', examName: 'A2 Key for Schools', pinVerified: true, textSize: 'Regular', contrast: 'BlackOnWhite' }

describe('App', () => {
  beforeEach(() => sessionStorage.clear())
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
})
