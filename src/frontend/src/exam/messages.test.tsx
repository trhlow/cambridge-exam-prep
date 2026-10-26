import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { CandidateMessage } from '../api/types'
import { MessageCentre } from './MessageCentre'

const message = (id: number, text: string): CandidateMessage => ({ id, text, sentAt: '2026-10-11T08:30:00Z', candidateId: null })

describe('MessageCentre', () => {
  beforeEach(() => sessionStorage.clear())

  it('shows a new invigilator message on screen until the candidate closes it', async () => {
    const { rerender } = render(<MessageCentre reader="A2/0101" messages={[]} />)
    expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument()

    rerender(<MessageCentre reader="A2/0101" messages={[message(7, 'Ten minutes left.')]} />)
    expect(screen.getByRole('alertdialog')).toHaveTextContent('Ten minutes left.')

    await userEvent.click(screen.getByRole('button', { name: 'OK' }))
    expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument()
  })

  it('keeps every message under the bell, with a count of unread ones', async () => {
    render(<MessageCentre reader="A2/0101" messages={[message(1, 'Please sit down.'), message(2, 'Ten minutes left.')]} />)

    expect(screen.getByRole('button', { name: /Messages, 2 new/ })).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'OK' }))
    await userEvent.click(screen.getByRole('button', { name: /Messages/ }))

    const list = screen.getByRole('list', { name: 'Messages from the invigilator' })
    expect(list).toHaveTextContent('Please sit down.')
    expect(list).toHaveTextContent('Ten minutes left.')
    expect(screen.getByRole('button', { name: 'Messages' })).toBeInTheDocument()
  })

  it('does not show a message read in one section again when the next section opens', async () => {
    const roomMessage = [message(3, 'Forty minutes left.')]
    const { unmount } = render(<MessageCentre reader="A2/0101" messages={roomMessage} />)
    await userEvent.click(screen.getByRole('button', { name: 'OK' }))
    unmount()

    // Listening: a new screen, the same exam and candidate.
    render(<MessageCentre reader="A2/0101" messages={roomMessage} />)
    expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Messages' })).toBeInTheDocument()
  })
})
