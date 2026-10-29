import { fireEvent, render, screen, within } from '@testing-library/react'
import { adminApi, type PaperListItem, type PaperSection } from '../api'
import { PapersPage } from './PapersPage'

const sections: PaperSection[] = [
  { sectionTemplateId: 1, examTypeCode: 'A2_KEY_FS', examType: 'A2 Key for Schools', section: 'Reading & Writing', skill: 'ReadingWriting', parts: 7 },
  { sectionTemplateId: 2, examTypeCode: 'A2_KEY_FS', examType: 'A2 Key for Schools', section: 'Listening', skill: 'Listening', parts: 5 },
  { sectionTemplateId: 3, examTypeCode: 'B1_PRELIM_FS', examType: 'B1 Preliminary for Schools', section: 'Reading', skill: 'Reading', parts: 6 },
  { sectionTemplateId: 4, examTypeCode: 'B1_PRELIM_FS', examType: 'B1 Preliminary for Schools', section: 'Writing', skill: 'Writing', parts: 2 },
  { sectionTemplateId: 5, examTypeCode: 'B1_PRELIM_FS', examType: 'B1 Preliminary for Schools', section: 'Listening', skill: 'Listening', parts: 4 },
]

const paper = (id: number, sectionTemplateId: number, title: string): PaperListItem => {
  const s = sections.find((x) => x.sectionTemplateId === sectionTemplateId)!
  return { id, title, examType: s.examType, sectionTemplateId, section: s.section, status: 'Published', parts: s.parts, questions: 20, createdAt: '2026-10-29T00:00:00Z', usedByExams: 0 }
}

describe('PapersPage filters and PDF import (VE-71)', () => {
  beforeEach(() => {
    vi.spyOn(adminApi, 'papers').mockResolvedValue([paper(1, 1, 'A2 Test 1 R&W'), paper(2, 3, 'B1 Test 1 Reading'), paper(3, 4, 'B1 Test 1 Writing')])
    vi.spyOn(adminApi, 'paperMixes').mockResolvedValue([])
    vi.spyOn(adminApi, 'paperSections').mockResolvedValue(sections)
  })

  afterEach(() => vi.restoreAllMocks())

  it('filters the papers by level and section', async () => {
    render(<PapersPage />)
    await screen.findByText('A2 Test 1 R&W')

    fireEvent.change(screen.getByLabelText('Level'), { target: { value: 'B1' } })
    expect(screen.queryByText('A2 Test 1 R&W')).toBeNull()
    expect(screen.getByText('B1 Test 1 Reading')).toBeInTheDocument()
    const sectionChoices = [...(screen.getByLabelText('Section') as HTMLSelectElement).options].map((o) => o.textContent)
    expect(sectionChoices).toEqual(['All sections', 'Reading', 'Writing', 'Listening'])

    fireEvent.change(screen.getByLabelText('Section'), { target: { value: '4' } })
    expect(screen.queryByText('B1 Test 1 Reading')).toBeNull()
    expect(screen.getByText('B1 Test 1 Writing')).toBeInTheDocument()
  })

  it('asks for the level and paper first, then only for what that paper needs', async () => {
    render(<PapersPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Import from PDF' }))
    const dialog = screen.getByRole('dialog')

    fireEvent.change(within(dialog).getByLabelText('Level'), { target: { value: 'B1' } })
    fireEvent.change(within(dialog).getByLabelText('Paper'), { target: { value: '4' } })
    expect(within(dialog).queryByLabelText('Answer key (PDF)')).toBeNull()
    expect(within(dialog).getByText('No answer key: examiners mark Writing.')).toBeInTheDocument()

    fireEvent.change(within(dialog).getByLabelText('Paper'), { target: { value: '5' } })
    expect(within(dialog).getByLabelText('Answer key (PDF)')).toBeInTheDocument()
    expect(within(dialog).getAllByLabelText(/^Part \d$/)).toHaveLength(4)
  })
})
