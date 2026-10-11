import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import type { Answers, QuestionGroup } from '../../api/types'
import { DragDropMatch } from './DragDropMatch'
import { ListeningIntro } from './ListeningIntro'

const cards = ['cake', 'drinks', 'fruit', 'games'].map((text, i) => ({ key: 'ABCD'[i], text, imageUrl: null }))

const group = (allowOptionReuse = false): QuestionGroup => ({
  interactionType: 'DragDropMatch',
  instructions: 'What will each person bring?',
  bodyHtml: null,
  optionPool: cards,
  allowOptionReuse,
  maxWordsPerAnswer: null,
  questions: [
    { id: 21, number: 21, promptHtml: 'Grandma', stimulusHtml: null, options: null, minWords: null },
    { id: 22, number: 22, promptHtml: 'Uncle Rob', stimulusHtml: null, options: null, minWords: null },
  ],
})

function WithAnswers({ reuse = false }: { reuse?: boolean }) {
  const [answers, setAnswers] = useState<Answers>({})
  return <DragDropMatch group={group(reuse)} answers={answers} onAnswer={(n, v) => setAnswers((a) => ({ ...a, [n]: v }))} />
}

describe('DragDropMatch (A2 Listening Part 5)', () => {
  it('shows the people with numbered slots and the word cards', () => {
    render(<WithAnswers />)

    expect(screen.getByRole('heading', { name: 'People' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Question 21, Grandma, empty' })).toBeInTheDocument()
    expect(within(screen.getByRole('list', { name: 'Word cards' })).getAllByRole('listitem')).toHaveLength(4)
  })

  it('places a card in a slot: pick the card, then the slot', async () => {
    render(<WithAnswers />)

    await userEvent.click(screen.getByRole('button', { name: 'fruit' }))
    await userEvent.click(screen.getByRole('button', { name: 'Question 21, Grandma, empty' }))

    expect(screen.getByRole('button', { name: 'Question 21, Grandma, fruit' })).toBeInTheDocument()
    expect(within(screen.getByRole('list', { name: 'Word cards' })).queryByRole('button', { name: 'fruit' })).not.toBeInTheDocument()
  })

  it('stores the key of the placed card as the answer', async () => {
    const onAnswer = vi.fn()
    render(<DragDropMatch group={group()} answers={{}} onAnswer={onAnswer} />)

    await userEvent.click(screen.getByRole('button', { name: 'cake' }))
    await userEvent.click(screen.getByRole('button', { name: 'Question 22, Uncle Rob, empty' }))

    expect(onAnswer).toHaveBeenCalledWith(22, 'A')
  })

  it('takes a card back out of its slot', async () => {
    render(<WithAnswers />)
    await userEvent.click(screen.getByRole('button', { name: 'fruit' }))
    await userEvent.click(screen.getByRole('button', { name: 'Question 21, Grandma, empty' }))

    await userEvent.click(screen.getByRole('button', { name: 'Remove fruit from question 21' }))

    expect(screen.getByRole('button', { name: 'Question 21, Grandma, empty' })).toBeInTheDocument()
    expect(within(screen.getByRole('list', { name: 'Word cards' })).getByRole('button', { name: 'fruit' })).toBeInTheDocument()
  })
})

describe('ListeningIntro', () => {
  it('explains the audio rule and starts the recording only when Play is pressed', async () => {
    const onPlay = vi.fn()
    render(<ListeningIntro onPlay={onPlay} isResuming={false} />)

    expect(screen.getByText(/not be permitted to pause or rewind the audio/)).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Play' }))

    expect(onPlay).toHaveBeenCalledTimes(1)
  })

  it('asks to continue rather than start when the recording is already running (after a reload)', () => {
    render(<ListeningIntro onPlay={() => {}} isResuming />)

    expect(screen.getByRole('button', { name: 'Continue listening' })).toBeInTheDocument()
  })
})
