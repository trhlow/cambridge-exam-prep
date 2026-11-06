import { fireEvent, render, screen, within } from '@testing-library/react'
import { adminApi, type ApprovalView, type PaperListItem, type PaperMixView, type PaperSection } from '../api'
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

  it('asks for an unused mix to be deleted, and says a source paper stays until then (B15)', async () => {
    const mix: PaperMixView = {
      id: 7,
      title: 'Mock 1',
      examType: 'A2 Key for Schools',
      sectionTemplateId: 1,
      section: 'Reading & Writing',
      sources: [{ letter: 'A', id: 1, title: 'A2 Test 1 R&W' }, { letter: 'B', id: 4, title: 'A2 Test 2 R&W' }],
      versionCount: 4,
      seed: 1,
      createdAt: '2026-10-29T00:00:00Z',
      versions: [{ version: 1, paperId: 11, title: 'Mock 1 – Version 1', pattern: 'ABABABA' }],
    }
    vi.spyOn(adminApi, 'papers').mockResolvedValue([{ ...paper(1, 1, 'A2 Test 1 R&W'), isMixSource: true }, paper(4, 1, 'A2 Test 2 R&W')])
    vi.spyOn(adminApi, 'paperMixes').mockResolvedValue([mix])
    const request: ApprovalView = { id: 9, kind: 'DeleteMix', targetId: 7, summary: 'Delete mix “Mock 1”', status: 'Pending', requestedById: 1, requestedBy: 'admin', requestedAt: '2026-11-02T00:00:00Z', decidedById: null, decidedBy: null, decidedAt: null, decisionNote: null, outcome: null }
    const deleteMix = vi.spyOn(adminApi, 'deleteMix').mockResolvedValue(request)
    render(<PapersPage />)

    await screen.findByText('Mixed papers')
    expect(screen.getByText('Mixed into a mix: delete the mix first')).toBeInTheDocument()
    const row = screen.getByText('Mock 1').closest('tr')!
    fireEvent.click(within(row).getByRole('button', { name: 'Request deletion' }))
    expect(deleteMix).not.toHaveBeenCalled()
    fireEvent.click(within(row).getByRole('button', { name: 'Ask an administrator to delete' }))

    expect(deleteMix).toHaveBeenCalledWith(7)
  })
})
