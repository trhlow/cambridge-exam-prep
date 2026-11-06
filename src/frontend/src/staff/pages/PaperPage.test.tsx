import { render, screen } from '@testing-library/react'
import { adminApi, type PaperDetail } from '../api'
import { PaperPage } from './PaperPage'

const paper: PaperDetail = {
  id: 7,
  title: 'A2 Key – Test 1',
  examType: 'A2 Key for Schools',
  section: 'Reading & Writing',
  status: 'Published',
  parts: [
    {
      partNo: 1,
      title: 'Part 1',
      layout: 'Single',
      stimulusHtml: null,
      audioUrl: null,
      groups: [
        {
          orderNo: 1,
          interactionType: 'SingleChoice',
          instructions: 'Choose.',
          bodyHtml: null,
          optionPool: null,
          questions: [{ number: 1, promptHtml: '<p>Q</p>', stimulusHtml: null, options: [{ key: 'A', text: 'x', imageUrl: null }, { key: 'B', text: 'y', imageUrl: null }], answers: ['A'], points: 1, minWords: null }],
        },
      ],
    },
  ],
}

describe('PaperPage correcting an answer (review of Task 6)', () => {
  afterEach(() => vi.restoreAllMocks())

  it('offers Correct answer on an ordinary published paper', async () => {
    vi.spyOn(adminApi, 'paper').mockResolvedValue(paper)
    render(<PaperPage paperId={7} isAdmin />)

    expect(await screen.findByRole('button', { name: 'Correct answer' })).toBeInTheDocument()
  })

  it('shows a note instead of the button on a mix version, pointing to the source papers', async () => {
    vi.spyOn(adminApi, 'paper').mockResolvedValue({ ...paper, paperMixId: 3, mixTitle: 'Mock 1', mixVersion: 2, mixPattern: 'A' })
    render(<PaperPage paperId={7} isAdmin />)

    expect(await screen.findByText(/correct the answer on its source paper/i)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Correct answer' })).toBeNull()
  })
})
