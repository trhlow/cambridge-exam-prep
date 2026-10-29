import { describe, expect, it } from 'vitest'
import { filterPapers, importNeeds, levelOf, NO_FILTER } from './paperFilters'
import type { PaperListItem, PaperSection } from './api'

const paper = (id: number, examType: string, sectionTemplateId: number, title: string, status: PaperListItem['status'] = 'Published'): PaperListItem => ({
  id,
  title,
  examType,
  sectionTemplateId,
  section: 'x',
  status,
  parts: 5,
  questions: 25,
  createdAt: '2026-10-29T00:00:00Z',
  usedByExams: 0,
})

const papers = [
  paper(1, 'A2 Key for Schools', 1, 'A2 Test 1 – Reading & Writing'),
  paper(2, 'A2 Key for Schools', 2, 'A2 Test 1 – Listening', 'Draft'),
  paper(3, 'B1 Preliminary for Schools', 3, 'B1 Test 1 – Reading'),
  paper(4, 'B1 Preliminary for Schools', 4, 'B1 Test 1 – Writing', 'Draft'),
]

const section = (skill: string, parts: number): PaperSection => ({ sectionTemplateId: 1, examTypeCode: 'X', examType: 'X', section: 'X', skill, parts })

describe('paper filters (VE-71)', () => {
  it('reads the level from the exam name', () => {
    expect(levelOf('A2 Key for Schools')).toBe('A2')
    expect(levelOf('B1 Preliminary for Schools')).toBe('B1')
  })

  it('shows everything without a filter', () => {
    expect(filterPapers(papers, NO_FILTER).map((p) => p.id)).toEqual([1, 2, 3, 4])
  })

  it('keeps one level, one section, one status or the titles with the words typed', () => {
    expect(filterPapers(papers, { ...NO_FILTER, level: 'B1' }).map((p) => p.id)).toEqual([3, 4])
    expect(filterPapers(papers, { ...NO_FILTER, level: 'A2', sectionId: 2 }).map((p) => p.id)).toEqual([2])
    expect(filterPapers(papers, { ...NO_FILTER, status: 'Draft' }).map((p) => p.id)).toEqual([2, 4])
    expect(filterPapers(papers, { ...NO_FILTER, text: 'test 1 writing' }).map((p) => p.id)).toEqual([1, 4])
  })

  it('asks for an answer key except for Writing, and one track per part for Listening', () => {
    expect(importNeeds(section('ReadingWriting', 7))).toEqual({ answerKey: true, tracks: 0 })
    expect(importNeeds(section('Writing', 2))).toEqual({ answerKey: false, tracks: 0 })
    expect(importNeeds(section('Listening', 4))).toEqual({ answerKey: true, tracks: 4 })
  })
})
