import type { PaperSection, PaperStatus } from './api'

/**
 * VE-71 (Victory 29/10): the paper library filtered by level (A2 Key or B1 Preliminary), section, status and title.
 * A2 Key has one Reading & Writing paper; B1 Preliminary has Reading and Writing apart, so the section list depends on
 * the level.
 */
export interface PaperFilter {
  /** "A2", "B1" or "" for every level. */
  level: string
  sectionId: number | null
  status: '' | PaperStatus
  text: string
}

export const NO_FILTER: PaperFilter = { level: '', sectionId: null, status: '', text: '' }

/** "A2" from "A2 Key for Schools", "B1" from "B1 Preliminary for Schools". */
export function levelOf(examType: string): string {
  return examType.split(' ')[0]
}

interface Filterable {
  title: string
  examType: string
  sectionTemplateId: number
  status?: PaperStatus
}

/** The rows that match every part of the filter; the text matches when each typed word is in the title. */
export function filterPapers<T extends Filterable>(rows: T[], filter: PaperFilter): T[] {
  const words = filter.text.toLowerCase().split(/\s+/).filter(Boolean)
  return rows.filter(
    (row) =>
      (!filter.level || levelOf(row.examType) === filter.level) &&
      (filter.sectionId === null || row.sectionTemplateId === filter.sectionId) &&
      (!filter.status || !row.status || row.status === filter.status) &&
      words.every((word) => row.title.toLowerCase().includes(word)),
  )
}

/** What the PDF import asks for: an answer key except for Writing (examiners mark it), one track per Listening part. */
export function importNeeds(section: PaperSection): { answerKey: boolean; tracks: number } {
  return { answerKey: section.skill !== 'Writing', tracks: section.skill === 'Listening' ? section.parts : 0 }
}
