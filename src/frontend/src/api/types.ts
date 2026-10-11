// Shapes returned by the ExamPrep API (src/backend/src/ExamPrep.Api). Answer keys never reach the browser.

export type InteractionType =
  | 'SingleChoice'
  | 'ImageChoice'
  | 'InlineChoice'
  | 'InlineText'
  | 'DragDropMatch'
  | 'Essay'

export interface CandidateProfile {
  candidateNumber: string
  fullName: string
  examName: string
  pinVerified: boolean
}

export interface CandidateSection {
  id: number
  orderNo: number
  title: string
  durationMinutes: number
  /** Locked: an earlier section has not ended yet (sections run one after another, Listening last). */
  status: 'NotStarted' | 'InProgress' | 'Paused' | 'Submitted' | 'Locked'
}

export interface Option {
  key: string
  text: string | null
  imageUrl: string | null
}

export interface Question {
  id: number
  number: number
  promptHtml: string | null
  stimulusHtml: string | null
  options: Option[] | null
  minWords: number | null
}

export interface QuestionGroup {
  interactionType: InteractionType
  instructions: string
  bodyHtml: string | null
  optionPool: Option[] | null
  allowOptionReuse: boolean
  maxWordsPerAnswer: number | null
  questions: Question[]
}

export interface Part {
  partNo: number
  title: string | null
  layout: 'Single' | 'Split'
  stimulusHtml: string | null
  audioUrl: string | null
  groups: QuestionGroup[]
}

export interface Paper {
  sectionId: number
  title: string
  durationMinutes: number
  parts: Part[]
}

/** The candidate's answer per question number: an option key, a typed word or a piece of writing. */
export type Answers = Record<number, string>

/** The server's view of the candidate's section: its clock and every saved answer. */
export interface AttemptState {
  sectionId: number
  status: 'InProgress' | 'Paused' | 'Submitted'
  endsAt: string | null
  remainingSeconds: number
  serverNow: string
  answers: Answers
}
