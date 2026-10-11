import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import type { Answers, Paper, Question, QuestionGroup } from '../api/types'
import { BottomBar } from './BottomBar'
import { ChoiceQuestion } from './questions/ChoiceQuestion'
import { EssayQuestion } from './questions/EssayQuestion'
import { GapText } from './questions/GapText'
import { buildPages } from './paperLogic'

const q = (number: number, extra: Partial<Question> = {}): Question => ({
  id: number,
  number,
  promptHtml: null,
  stimulusHtml: null,
  options: null,
  minWords: null,
  ...extra,
})

const words = (...texts: string[]) => texts.map((text, i) => ({ key: 'ABC'[i], text, imageUrl: null }))

/** Holds answers like the exam screen does, so tests see what the candidate sees after each action. */
function WithAnswers({ children }: { children: (answers: Answers, set: (n: number, v: string) => void) => React.ReactNode }) {
  const [answers, setAnswers] = useState<Answers>({})
  return <>{children(answers, (n, v) => setAnswers((a) => ({ ...a, [n]: v })))}</>
}

describe('GapText — choose a word (A2 Reading Part 4)', () => {
  const group: QuestionGroup = {
    interactionType: 'InlineChoice',
    instructions: 'For each question, choose the correct answer.',
    bodyHtml: '<p>There is a big {{q:19}} between hotels.</p>',
    optionPool: null,
    allowOptionReuse: false,
    maxWordsPerAnswer: null,
    questions: [q(19, { options: words('change', 'variety', 'difference') })],
  }

  it('shows the gap numbered and opens the word picker when clicked', async () => {
    render(<WithAnswers>{(answers, set) => <GapText group={group} answers={answers} onAnswer={set} />}</WithAnswers>)

    await userEvent.click(screen.getByRole('button', { name: 'Question 19' }))

    const picker = screen.getByRole('listbox', { name: 'Choices for question 19' })
    expect(within(picker).getAllByRole('option').map((o) => o.textContent)).toEqual(['change', 'variety', 'difference'])
  })

  it('puts the chosen word in the gap and stores its key', async () => {
    const onAnswer = vi.fn()
    render(<GapText group={group} answers={{}} onAnswer={onAnswer} />)

    await userEvent.click(screen.getByRole('button', { name: 'Question 19' }))
    await userEvent.click(screen.getByRole('option', { name: 'difference' }))

    expect(onAnswer).toHaveBeenCalledWith(19, 'C')
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument()
  })

  it('shows the chosen word and clears it with ×', async () => {
    render(<WithAnswers>{(answers, set) => <GapText group={group} answers={answers} onAnswer={set} />}</WithAnswers>)
    await userEvent.click(screen.getByRole('button', { name: 'Question 19' }))
    await userEvent.click(screen.getByRole('option', { name: 'variety' }))
    expect(screen.getByRole('button', { name: 'Question 19' })).toHaveTextContent('variety')

    await userEvent.click(screen.getByRole('button', { name: 'Question 19' }))
    await userEvent.click(screen.getByRole('button', { name: 'Clear answer' }))

    expect(screen.getByRole('button', { name: 'Question 19' })).toHaveTextContent('19')
  })
})

describe('GapText — type a word (A2 Reading Part 5)', () => {
  it('stores what the candidate types', async () => {
    const group: QuestionGroup = {
      interactionType: 'InlineText',
      instructions: 'Write ONE word for each gap.',
      bodyHtml: '<p>Thanks {{q:25}} your email.</p>',
      optionPool: null,
      allowOptionReuse: false,
      maxWordsPerAnswer: 1,
      questions: [q(25)],
    }
    render(<WithAnswers>{(answers, set) => <GapText group={group} answers={answers} onAnswer={set} />}</WithAnswers>)

    await userEvent.type(screen.getByRole('textbox', { name: 'Question 25' }), 'for')

    expect(screen.getByRole('textbox', { name: 'Question 25' })).toHaveValue('for')
  })
})

describe('EssayQuestion (A2 Writing Part 6–7)', () => {
  it('counts words as the candidate writes', async () => {
    render(<WithAnswers>{(answers, set) => <EssayQuestion question={q(31, { minWords: 25 })} answer={answers[31] ?? ''} onAnswer={set} />}</WithAnswers>)
    expect(screen.getByText('Words: 0')).toBeInTheDocument()

    await userEvent.type(screen.getByRole('textbox', { name: 'Question 31' }), 'Hi Jo, see you at six')

    expect(screen.getByText('Words: 6')).toBeInTheDocument()
  })
})

describe('ChoiceQuestion', () => {
  it('selects one option as a radio group', async () => {
    const onAnswer = vi.fn()
    const question = q(1, { promptHtml: 'Go upstairs if you want to', options: words('buy a dress.', 'pay less for a book.', 'find a game.') })
    render(<ChoiceQuestion question={question} answer="A" onAnswer={onAnswer} flagged={false} onToggleFlag={() => {}} />)

    expect(screen.getByRole('radio', { name: 'buy a dress.' })).toBeChecked()
    await userEvent.click(screen.getByRole('radio', { name: 'pay less for a book.' }))

    expect(onAnswer).toHaveBeenCalledWith(1, 'B')
  })
})

describe('BottomBar', () => {
  const paper: Paper = {
    sectionId: 1,
    title: 'Reading & Writing',
    durationMinutes: 60,
    parts: [
      { partNo: 1, title: null, layout: 'Single', stimulusHtml: null, audioUrl: null, groups: [{ interactionType: 'SingleChoice', instructions: '', bodyHtml: null, optionPool: null, allowOptionReuse: false, maxWordsPerAnswer: null, questions: [q(1, { stimulusHtml: '<img>' }), q(2, { stimulusHtml: '<img>' })] }] },
      { partNo: 2, title: null, layout: 'Split', stimulusHtml: '<p/>', audioUrl: null, groups: [{ interactionType: 'SingleChoice', instructions: '', bodyHtml: null, optionPool: null, allowOptionReuse: false, maxWordsPerAnswer: null, questions: [q(3), q(4), q(5)] }] },
    ],
  }
  const pages = buildPages(paper)

  it('shows "n of m" for the other parts and the question numbers of the open part', () => {
    render(<BottomBar paper={paper} pages={pages} pageIndex={0} answers={{ 3: 'A' }} onGoTo={() => {}} onReview={() => {}} />)

    expect(screen.getByRole('button', { name: /Part 2/ })).toHaveTextContent('Part 21 of 3')
    expect(screen.getByRole('button', { name: 'Question 1' })).toHaveAttribute('aria-current', 'true')
    expect(screen.getByRole('button', { name: 'Question 2' })).toBeInTheDocument()
  })

  it('boxes only the question being worked on when a whole part is on one page', () => {
    render(<BottomBar paper={paper} pages={pages} pageIndex={2} activeNumber={4} answers={{}} onGoTo={() => {}} onReview={() => {}} />)

    expect(screen.getByRole('button', { name: 'Question 4' })).toHaveAttribute('aria-current', 'true')
    expect(screen.getByRole('button', { name: 'Question 3' })).not.toHaveAttribute('aria-current')
  })

  it('reports which question was picked so the screen can scroll to it', async () => {
    const onFocusQuestion = vi.fn()
    render(<BottomBar paper={paper} pages={pages} pageIndex={2} answers={{}} onGoTo={() => {}} onFocusQuestion={onFocusQuestion} onReview={() => {}} />)

    await userEvent.click(screen.getByRole('button', { name: 'Question 5' }))

    expect(onFocusQuestion).toHaveBeenCalledWith(5)
  })

  it('keeps showing flagged questions on the tile of a part that is not open', () => {
    render(<BottomBar paper={paper} pages={pages} pageIndex={0} flagged={new Set([4])} answers={{}} onGoTo={() => {}} onReview={() => {}} />)

    const part2 = screen.getByRole('button', { name: /Part 2/ })
    expect(within(part2).getByRole('img', { name: '1 flagged question' })).toBeInTheDocument()
    expect(screen.getAllByRole('img', { name: /flagged/ })).toHaveLength(1)
  })

  it('marks a flagged question in the open part', () => {
    render(<BottomBar paper={paper} pages={pages} pageIndex={2} flagged={new Set([4])} answers={{}} onGoTo={() => {}} onReview={() => {}} />)

    expect(screen.getByRole('button', { name: 'Question 4, flagged' })).toBeInTheDocument()
  })

  it('goes to the page of a question or the first page of a part', async () => {
    const onGoTo = vi.fn()
    render(<BottomBar paper={paper} pages={pages} pageIndex={0} answers={{}} onGoTo={onGoTo} onReview={() => {}} />)

    await userEvent.click(screen.getByRole('button', { name: 'Question 2' }))
    await userEvent.click(screen.getByRole('button', { name: /Part 2/ }))

    expect(onGoTo.mock.calls).toEqual([[1], [2]])
  })
})

describe('ReviewPage', () => {
  it('has no submit button: the section ends when the time is up, as in the Cambridge exam room', async () => {
    const { ReviewPage } = await import('./ReviewPage')
    const paper: Paper = { sectionId: 1, title: 'Reading & Writing', durationMinutes: 60, parts: [] }

    render(<ReviewPage paper={paper} answers={{}} onBack={() => {}} />)

    expect(screen.queryByRole('button', { name: /submit/i })).not.toBeInTheDocument()
    expect(screen.getByText(/saved automatically/)).toBeInTheDocument()
  })
})
