import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { adminApi, type ExamDetail, type PaperListItem, type PaperMixView } from '../api'
import { ExamFormPage } from './ExamFormPage'

const paper = (id: number, sectionTemplateId: number, title: string, extra: Partial<PaperListItem> = {}): PaperListItem => ({
  id,
  title,
  examType: 'A2 Key for Schools',
  sectionTemplateId,
  section: sectionTemplateId === 1 ? 'Reading & Writing' : 'Listening',
  status: 'Published',
  parts: 7,
  questions: 32,
  createdAt: '2026-10-21T00:00:00Z',
  usedByExams: 0,
  ...extra,
})

const mix: PaperMixView = {
  id: 7,
  title: 'Mock 1 mix',
  examType: 'A2 Key for Schools',
  sectionTemplateId: 1,
  section: 'Reading & Writing',
  sourceA: { id: 1, title: 'RW A' },
  sourceB: { id: 2, title: 'RW B' },
  seed: 1,
  createdAt: '2026-10-21T00:00:00Z',
  versions: [1, 2, 3, 4].map((version) => ({ version, paperId: 10 + version, title: `Mock 1 mix – Version ${version}`, pattern: 'ABABABA' })),
}

describe('ExamFormPage with mixed papers (VE-67)', () => {
  afterEach(() => vi.restoreAllMocks())

  it('gives a section the mix and sends the computers per row', async () => {
    vi.spyOn(adminApi, 'examTypes').mockResolvedValue([
      { id: 1, code: 'A2_KEY_FS', name: 'A2 Key for Schools', sections: [{ id: 1, orderNo: 1, title: 'Reading & Writing', durationMinutes: 60 }, { id: 2, orderNo: 2, title: 'Listening', durationMinutes: 35 }] },
    ])
    // The four mixed papers are chosen through their mix, never one by one.
    vi.spyOn(adminApi, 'examPapers').mockResolvedValue([paper(1, 1, 'RW A'), paper(11, 1, 'Mock 1 mix – Version 1', { paperMixId: 7, mixVersion: 1 }), paper(3, 2, 'Listening A')])
    vi.spyOn(adminApi, 'examPaperMixes').mockResolvedValue([mix])
    const create = vi.spyOn(adminApi, 'createExam').mockResolvedValue({ id: 99 } as ExamDetail)
    render(<ExamFormPage />)

    fireEvent.change(await screen.findByLabelText('Name'), { target: { value: 'Mock exam' } })
    const readingWriting = screen.getByLabelText(/Reading & Writing/) as HTMLSelectElement
    expect([...readingWriting.options].map((o) => o.textContent)).toEqual(['Choose a published paper…', 'RW A', 'Mock 1 mix'])
    fireEvent.change(readingWriting, { target: { value: 'm:7' } })
    fireEvent.change(screen.getByLabelText(/Listening/), { target: { value: 'p:3' } })
    fireEvent.change(screen.getByLabelText(/Computers per row/), { target: { value: '4' } })
    expect(screen.getByText(/neighbours never share one/)).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Create exam' }))

    await waitFor(() => expect(create).toHaveBeenCalled())
    expect(create.mock.calls[0][0]).toMatchObject({
      papers: [
        { sectionTemplateId: 1, testPaperId: 0, paperMixId: 7 },
        { sectionTemplateId: 2, testPaperId: 3 },
      ],
      seatsPerRow: 4,
    })
  })
})
