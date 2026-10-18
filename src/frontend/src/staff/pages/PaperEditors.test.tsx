import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { adminApi, type PaperDetail, type PaperGroupDetail } from '../api'
import { PaperEditor } from './PaperEditors'

const group: PaperGroupDetail = {
  orderNo: 1,
  interactionType: 'SingleChoice',
  instructions: 'Choose the correct answer.',
  bodyHtml: null,
  optionPool: null,
  questions: [
    {
      number: 1,
      promptHtml: '<p>What does the notice say?</p>',
      stimulusHtml: null,
      options: [
        { key: 'A', text: 'Closed', imageUrl: null },
        { key: 'B', text: 'Open', imageUrl: null },
      ],
      answers: ['A'],
      points: 1,
      minWords: null,
    },
  ],
}

const paper: PaperDetail = { id: 5, title: 'A2 R&W', examType: 'A2 Key for Schools', section: 'Reading & Writing', status: 'Draft', parts: [] }

describe('PaperEditor (VE-25)', () => {
  afterEach(() => vi.restoreAllMocks())

  it('saves a question with new option texts and a new answer, keeping the keys', async () => {
    const save = vi.spyOn(adminApi, 'editQuestion').mockResolvedValue(paper)
    const onSaved = vi.fn()
    render(<PaperEditor paper={paper} edit={{ kind: 'question', group, question: group.questions[0] }} onClose={() => {}} onSaved={onSaved} />)

    const optionB = screen.getAllByRole('textbox').find((box) => (box as HTMLInputElement).value === 'Open')!
    await userEvent.clear(optionB)
    await userEvent.type(optionB, 'Open late')
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Answer' }), 'B')
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(save).toHaveBeenCalledWith(5, 1, {
      promptHtml: '<p>What does the notice say?</p>',
      stimulusHtml: null,
      options: [
        { key: 'A', text: 'Closed' },
        { key: 'B', text: 'Open late' },
      ],
      answers: ['B'],
      points: 1,
      minWords: null,
    })
    expect(onSaved).toHaveBeenCalledWith(paper)
  })
})
