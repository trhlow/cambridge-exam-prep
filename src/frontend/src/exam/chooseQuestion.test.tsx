import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import type { Part, Question, Selection, Selections } from '../api/types'
import { PartView } from './PartView'

const essay = (number: number, task: string): Question => ({
  id: number,
  number,
  promptHtml: null,
  stimulusHtml: `<p>${task}</p>`,
  options: null,
  minWords: 100,
})

/** B1 Writing Part 2: two questions, one page each, the candidate answers one. */
const part2: Part = {
  partNo: 2,
  title: null,
  layout: 'Split',
  stimulusHtml: null,
  audioUrl: null,
  questionPerPage: true,
  groups: [
    {
      interactionType: 'Essay',
      instructions: 'Choose one of these questions. Write your answer in about 100 words.',
      bodyHtml: null,
      optionPool: null,
      allowOptionReuse: false,
      maxWordsPerAnswer: null,
      chooseCount: 1,
      questions: [essay(2, 'Write your article.'), essay(3, 'Write your story.')],
    },
  ],
}

/** The exam screen's part of the work: keeps the choices and asks the "server", which may refuse. */
function Page({ number, initial = {}, refuse }: { number: number; initial?: Selections; refuse?: string }) {
  const [selections, setSelections] = useState<Selections>(initial)
  const select = async (n: number, selection: Selection) => {
    if (refuse) {
      return refuse
    }

    setSelections((current) => ({ ...current, [n]: selection }))
    return null
  }

  return (
    <PartView
      part={part2}
      questionNumbers={[number]}
      answers={{}}
      onAnswer={() => {}}
      selections={selections}
      onSelect={select}
      flagged={new Set()}
      onToggleFlag={() => {}}
    />
  )
}

describe('B1 Writing Part 2: choose one of the questions (VE-35)', () => {
  it('shows the question’s own task beside the editor and nothing chosen yet', () => {
    render(<Page number={2} />)

    expect(screen.getByText('Write your article.')).toBeInTheDocument()
    expect(screen.queryByText('Write your story.')).not.toBeInTheDocument()
    expect(screen.getByRole('combobox', { name: 'Answering question 2?' })).toHaveValue('Undecided')
    expect(screen.getByText('0 of 1 question selected')).toBeInTheDocument()
  })

  it('counts the question once the candidate answers Yes', async () => {
    render(<Page number={2} />)

    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Answering question 2?' }), 'Yes')

    expect(screen.getByRole('combobox', { name: 'Answering question 2?' })).toHaveValue('Yes')
    expect(screen.getByText('1 of 1 question selected')).toBeInTheDocument()
  })

  it('does not offer Yes on the other question once one is chosen', () => {
    render(<Page number={3} initial={{ 2: 'Yes' }} />)

    expect(screen.getByRole('option', { name: 'Yes' })).toBeDisabled()
    expect(screen.getByText('1 of 1 question selected')).toBeInTheDocument()
  })

  it('shows the server’s refusal', async () => {
    render(<Page number={3} refuse="You can answer only 1 of these questions. Change your other choice first." />)

    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Answering question 3?' }), 'Yes')

    expect(await screen.findByRole('alert')).toHaveTextContent('You can answer only 1 of these questions.')
  })
})
