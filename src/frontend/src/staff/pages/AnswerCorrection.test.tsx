import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { adminApi, type PaperGroupDetail } from '../api'
import { AnswerCorrection } from './AnswerCorrection'

const choiceGroup: PaperGroupDetail = {
  orderNo: 1,
  interactionType: 'SingleChoice',
  instructions: 'Choose.',
  bodyHtml: null,
  optionPool: null,
  questions: [{ number: 2, promptHtml: '<p>Q</p>', stimulusHtml: null, options: [{ key: 'A', text: 'x', imageUrl: null }, { key: 'B', text: 'y', imageUrl: null }], answers: ['A'], points: 1, minWords: null }],
}

const typedGroup: PaperGroupDetail = {
  ...choiceGroup,
  interactionType: 'InlineText',
  questions: [{ number: 25, promptHtml: null, stimulusHtml: null, options: null, answers: ['for'], points: 1, minWords: null }],
}

describe('AnswerCorrection (A6 repair)', () => {
  afterEach(() => vi.restoreAllMocks())

  it('needs a second click before it corrects a published answer, then reports the re-marking', async () => {
    const correct = vi.spyOn(adminApi, 'correctAnswers').mockResolvedValue({ number: 2, answers: ['B'], attemptsRemarked: 3, scoresChanged: 2, skipped: [] })
    const onDone = vi.fn()
    render(<AnswerCorrection paperId={5} group={choiceGroup} question={choiceGroup.questions[0]} onClose={() => {}} onDone={onDone} />)

    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Correct answer' }), 'B')
    await userEvent.click(screen.getByRole('button', { name: 'Correct the answer' }))
    expect(correct).not.toHaveBeenCalled()
    await userEvent.click(screen.getByRole('button', { name: 'Confirm: mark the attempts again' }))

    expect(correct).toHaveBeenCalledWith(5, 2, ['B'])
    expect(onDone).toHaveBeenCalledWith({ number: 2, answers: ['B'], attemptsRemarked: 3, scoresChanged: 2, skipped: [] })
  })

  it('takes several accepted answers for a typed gap, one per line', async () => {
    const correct = vi.spyOn(adminApi, 'correctAnswers').mockResolvedValue({ number: 25, answers: ['for', 'from'], attemptsRemarked: 0, scoresChanged: 0, skipped: [] })
    render(<AnswerCorrection paperId={5} group={typedGroup} question={typedGroup.questions[0]} onClose={() => {}} onDone={() => {}} />)

    const box = screen.getByRole('textbox', { name: /Correct answers/ })
    await userEvent.clear(box)
    await userEvent.type(box, 'for{Enter}from')
    await userEvent.click(screen.getByRole('button', { name: 'Correct the answer' }))
    await userEvent.click(screen.getByRole('button', { name: 'Confirm: mark the attempts again' }))

    expect(correct).toHaveBeenCalledWith(5, 25, ['for', 'from'])
  })
})
