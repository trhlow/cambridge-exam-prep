import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import type { Answers, QuestionGroup } from '../../api/types'
import { DragDropMatch } from './DragDropMatch'

const question = (number: number, extra: Partial<QuestionGroup['questions'][number]> = {}) => ({ id: number, number, promptHtml: null, stimulusHtml: null, options: null, minWords: null, ...extra })

const people: QuestionGroup = {
  interactionType: 'DragDropMatch',
  instructions: 'For each question, choose the correct answer.',
  bodyHtml: null,
  allowOptionReuse: false,
  maxWordsPerAnswer: null,
  itemsHeading: 'Young People',
  poolHeading: 'Summer Clubs',
  optionPool: [
    { key: 'A', title: 'Riverside Rowing', text: 'Beginners on Tuesdays.', imageUrl: null },
    { key: 'B', title: 'Stage Nights', text: 'Bands play in August.', imageUrl: null },
  ],
  questions: [
    question(6, { stimulusHtml: '<img src="/x.png" alt="Dina">', promptHtml: '<p>Dina wants a new sport.</p>' }),
    question(7, { stimulusHtml: '<img src="/y.png" alt="Omar">', promptHtml: '<p>Omar plays the guitar.</p>' }),
  ],
}

const article: QuestionGroup = {
  interactionType: 'DragDropMatch',
  instructions: 'Five sentences have been removed from the text below.',
  bodyHtml: '<p>I wanted to cook. {{q:16}} Until then I only made toast.</p><p>The videos were hard. {{q:17}}</p>',
  allowOptionReuse: false,
  maxWordsPerAnswer: null,
  optionPool: [
    { key: 'A', text: 'My parents were surprised.', imageUrl: null },
    { key: 'B', text: 'So I asked my grandfather.', imageUrl: null },
    { key: 'C', text: 'The weather was perfect.', imageUrl: null },
  ],
  questions: [question(16), question(17)],
}

function Harness({ group }: { group: QuestionGroup }) {
  const [answers, setAnswers] = useState<Answers>({})
  return <DragDropMatch group={group} answers={answers} onAnswer={(n, v) => setAnswers((a) => ({ ...a, [n]: v }))} />
}

describe('DragDropMatch with long cards (B1 Reading, VE-34)', () => {
  it('shows each person with picture, text and a slot, and cards with their title in bold', async () => {
    const user = userEvent.setup()
    render(<Harness group={people} />)

    expect(screen.getByRole('heading', { name: 'Young People' })).toBeInTheDocument()
    expect(screen.getByRole('img', { name: 'Dina' })).toBeInTheDocument()
    expect(screen.getByText('Dina wants a new sport.')).toBeInTheDocument()
    const pool = screen.getByRole('list', { name: 'Summer Clubs' })
    expect(within(pool).getByText('Riverside Rowing').tagName).toBe('STRONG')

    await user.click(within(pool).getByRole('button', { name: /Riverside Rowing/ }))
    await user.click(screen.getByRole('button', { name: 'Question 6, empty' }))

    expect(screen.getByRole('button', { name: 'Question 6, Riverside Rowing' })).toHaveTextContent('Riverside Rowing Beginners on Tuesdays.')
    expect(within(pool).queryByText('Riverside Rowing')).not.toBeInTheDocument()
  })

  it('puts the slots inside the text and fills a gap with the chosen sentence', async () => {
    const user = userEvent.setup()
    const { container } = render(<Harness group={article} />)

    expect(screen.queryByRole('heading', { name: 'People' })).not.toBeInTheDocument()
    const paragraph = container.querySelector('.match-text p')!
    expect(within(paragraph as HTMLElement).getByRole('button', { name: 'Question 16, empty' })).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'So I asked my grandfather.' }))
    await user.click(screen.getByRole('button', { name: 'Question 17, empty' }))

    expect(screen.getByRole('button', { name: 'Question 17, So I asked my grandfather.' })).toBeInTheDocument()
    expect(screen.getAllByRole('listitem')).toHaveLength(2)
  })
})
