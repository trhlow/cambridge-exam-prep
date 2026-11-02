import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { accessOf } from '../access'
import { adminApi, staffApi, type ExamDetail } from '../api'
import { ExamPage } from './ExamPage'

const exam: ExamDetail = {
  id: 4,
  name: 'A2 Key – 20/11',
  examType: 'A2 Key for Schools',
  examTypeId: 1,
  scheduledDate: '2026-11-20',
  room: 'Room 1',
  status: 'Active',
  candidates: 3,
  sections: [],
  sessionPin: '4821',
  invigilatorPassword: 'invig',
  hasStarted: true,
  invigilators: [],
  examiners: [],
  sebConfigKey: null,
  canOrganise: true,
  canMonitor: true,
}

const invigilator = accessOf({ id: 7, username: 'giamthi', fullName: 'Invigilator', email: null, role: 'Invigilator', isActive: true, areas: [] })
const examiner = accessOf({ id: 8, username: 'giamkhao', fullName: 'Examiner', email: null, role: 'Examiner', isActive: true, areas: [] })

describe('ExamPage end of the whole test (B4)', () => {
  beforeEach(() => {
    vi.spyOn(staffApi, 'exam').mockResolvedValue(exam)
    vi.spyOn(adminApi, 'assignableStaff').mockResolvedValue([])
  })
  afterEach(() => vi.restoreAllMocks())

  it('ends the test for everyone only after a second click', async () => {
    const endAll = vi.spyOn(staffApi, 'endAll').mockResolvedValue({ applied: 3, skipped: [] })
    render(<ExamPage examId={4} access={invigilator} />)

    await userEvent.click(await screen.findByRole('button', { name: 'End the whole test' }))
    expect(endAll).not.toHaveBeenCalled()
    await userEvent.click(screen.getByRole('button', { name: 'Click again: the test ends now for all 3 candidates' }))

    expect(endAll).toHaveBeenCalledWith(4, [])
    expect(await screen.findByText(/ended for 3 candidates/)).toBeInTheDocument()
  })

  it('is not offered to people who do not run the room', async () => {
    render(<ExamPage examId={4} access={examiner} />)

    await screen.findByRole('heading', { name: 'A2 Key – 20/11' })
    expect(screen.queryByRole('button', { name: 'End the whole test' })).not.toBeInTheDocument()
  })
})
