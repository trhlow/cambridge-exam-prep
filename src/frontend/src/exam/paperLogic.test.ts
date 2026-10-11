import type { Paper, Part, Question, QuestionGroup } from '../api/types'
import { answeredCount, buildPages, countWords, gapNumbers, questionRange, withGapSlots } from './paperLogic'

const question = (number: number, extra: Partial<Question> = {}): Question => ({
  id: number * 10,
  number,
  promptHtml: null,
  stimulusHtml: null,
  options: null,
  minWords: null,
  ...extra,
})

const group = (interactionType: QuestionGroup['interactionType'], questions: Question[], extra: Partial<QuestionGroup> = {}): QuestionGroup => ({
  interactionType,
  instructions: 'For each question, choose the correct answer.',
  bodyHtml: null,
  optionPool: null,
  allowOptionReuse: false,
  maxWordsPerAnswer: null,
  questions,
  ...extra,
})

const part = (partNo: number, groups: QuestionGroup[], extra: Partial<Part> = {}): Part => ({
  partNo,
  title: null,
  layout: 'Single',
  stimulusHtml: null,
  audioUrl: null,
  groups,
  ...extra,
})

const paper = (parts: Part[]): Paper => ({ sectionId: 1, title: 'Reading & Writing', durationMinutes: 60, parts })

describe('buildPages', () => {
  it('gives each question its own page when every question carries its own picture (A2 Reading Part 1)', () => {
    const notices = part(1, [group('SingleChoice', [1, 2, 3].map((n) => question(n, { stimulusHtml: '<img src="/api/assets/1">' })))])

    const pages = buildPages(paper([notices]))

    expect(pages).toEqual([
      { partIndex: 0, questionNumbers: [1] },
      { partIndex: 0, questionNumbers: [2] },
      { partIndex: 0, questionNumbers: [3] },
    ])
  })

  it('gives each picture question its own page (A2 Listening Part 1)', () => {
    const pictures = part(1, [group('ImageChoice', [question(1), question(2)])])

    expect(buildPages(paper([pictures]))).toEqual([
      { partIndex: 0, questionNumbers: [1] },
      { partIndex: 0, questionNumbers: [2] },
    ])
  })

  it('shows a whole part on one page otherwise', () => {
    const split = part(2, [group('SingleChoice', [question(7), question(8)])], { layout: 'Split', stimulusHtml: '<p>Text</p>' })
    const gaps = part(4, [group('InlineChoice', [question(19), question(20)], { bodyHtml: '{{q:19}} {{q:20}}' })])

    const pages = buildPages(paper([split, gaps]))

    expect(pages).toEqual([
      { partIndex: 0, questionNumbers: [7, 8] },
      { partIndex: 1, questionNumbers: [19, 20] },
    ])
  })
})

describe('gaps', () => {
  it('finds the question numbers of the gaps in a text', () => {
    expect(gapNumbers('<p>Thanks {{q:25}} your email, talk {{q:26}} late.</p>')).toEqual([25, 26])
  })

  it('turns each gap into an empty slot the answer box is rendered into', () => {
    expect(withGapSlots('<p>a {{q:19}} b</p>')).toBe('<p>a <span data-gap="19"></span> b</p>')
  })
})

describe('countWords', () => {
  it.each([
    ['', 0],
    ['   ', 0],
    ['Hi Jo', 2],
    ['Hi Jo,\nlet\'s go   to the cinema on Saturday!', 9],
    ['well-known e-mail', 2],
  ])('counts %j as %i words', (text, expected) => {
    expect(countWords(text)).toBe(expected)
  })
})

describe('answeredCount', () => {
  const twoQuestions = part(5, [group('InlineText', [question(25), question(26)], { bodyHtml: '{{q:25}} {{q:26}}' })])

  it('counts questions with a non-blank answer', () => {
    expect(answeredCount(twoQuestions, { 25: 'for', 26: '  ' })).toBe(1)
  })

  it('gives the question range shown in the instruction box', () => {
    expect(questionRange([twoQuestions.groups[0].questions[0], twoQuestions.groups[0].questions[1]])).toBe('Questions 25–26')
    expect(questionRange([question(31)])).toBe('Question 31')
  })
})
