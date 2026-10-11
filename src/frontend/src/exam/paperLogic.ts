import type { Answers, Paper, Part, Question } from '../api/types'

/** One screen of the test: Inspera shows A2 Reading Part 1 one notice at a time, every other part whole. */
export interface Page {
  partIndex: number
  questionNumbers: number[]
}

const GAP = /\{\{q:(\d+)\}\}/g

export function questionsOf(part: Part): Question[] {
  return part.groups.flatMap((g) => g.questions)
}

/**
 * Inspera pages question by question when every question brings its own pictures: notices in A2 Reading
 * Part 1, picture options in A2 Listening Part 1.
 */
function isOnePerPage(part: Part): boolean {
  const questions = questionsOf(part)
  if (part.layout !== 'Single' || questions.length < 2) {
    return false
  }

  const isPictureChoice = part.groups.every((g) => g.interactionType === 'ImageChoice')
  const isNoticeChoice = part.groups.every((g) => g.interactionType === 'SingleChoice') && questions.every((q) => q.stimulusHtml !== null)
  return isPictureChoice || isNoticeChoice
}

export function buildPages(paper: Paper): Page[] {
  return paper.parts.flatMap((part, partIndex) => {
    const numbers = questionsOf(part).map((q) => q.number)
    return isOnePerPage(part)
      ? numbers.map((n) => ({ partIndex, questionNumbers: [n] }))
      : [{ partIndex, questionNumbers: numbers }]
  })
}

export function gapNumbers(html: string): number[] {
  return [...html.matchAll(GAP)].map((m) => Number(m[1]))
}

/** Replaces each {{q:n}} with an empty span that the gap control is portalled into. */
export function withGapSlots(html: string): string {
  return html.replace(GAP, (_, n: string) => `<span data-gap="${n}"></span>`)
}

export function countWords(text: string): number {
  const trimmed = text.trim()
  return trimmed === '' ? 0 : trimmed.split(/\s+/).length
}

export function isAnswered(answers: Answers, number: number): boolean {
  return (answers[number] ?? '').trim() !== ''
}

export function answeredCount(part: Part, answers: Answers): number {
  return questionsOf(part).filter((q) => isAnswered(answers, q.number)).length
}

/** "Questions 1–6" or "Question 31", as in the Inspera instruction box. */
export function questionRange(questions: Question[]): string {
  const first = questions[0].number
  const last = questions[questions.length - 1].number
  return first === last ? `Question ${first}` : `Questions ${first}–${last}`
}
