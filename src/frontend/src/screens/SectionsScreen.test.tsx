import { render, screen } from '@testing-library/react'
import { candidateApi } from '../api/client'
import type { CandidateProfile } from '../api/types'
import { SectionsScreen } from './SignInScreens'

const profile: CandidateProfile = { candidateNumber: '0001', fullName: 'An Nguyen', examName: 'A2 Key for Schools', pinVerified: true, textSize: 'Regular', contrast: 'BlackOnWhite' }

describe('SectionsScreen', () => {
  it('keeps Listening closed until Reading & Writing has finished', async () => {
    vi.spyOn(candidateApi, 'sections').mockResolvedValue([
      { id: 1, orderNo: 1, title: 'Reading & Writing', durationMinutes: 60, status: 'NotStarted' },
      { id: 2, orderNo: 2, title: 'Listening', durationMinutes: 30, status: 'Locked' },
    ])

    render(<SectionsScreen profile={profile} onStart={() => {}} />)

    expect(await screen.findByRole('button', { name: 'Open Reading & Writing' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Listening is locked' })).toBeDisabled()
    expect(screen.getByText('Opens when Reading & Writing has finished')).toBeInTheDocument()
  })
})
