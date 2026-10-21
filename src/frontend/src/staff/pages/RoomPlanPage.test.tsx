import { render, screen, within } from '@testing-library/react'
import { staffApi, type RoomPlanView } from '../api'
import { RoomPlanPage } from './RoomPlanPage'

const plan: RoomPlanView = {
  examId: 5,
  examName: 'A2 Key – 20/11',
  scheduledDate: '2026-11-20',
  room: 'Room 1',
  seatsPerRow: 2,
  seats: [
    { seat: 1, candidateId: 1, candidateNumber: '0001', fullName: 'An Nguyễn', seatTyped: true, paperVersion: 1 },
    { seat: 2, candidateId: 2, candidateNumber: '0002', fullName: 'Bình Trần', seatTyped: true, paperVersion: 2 },
    { seat: 4, candidateId: 3, candidateNumber: '0003', fullName: 'Chi Lê', seatTyped: false, paperVersion: 4 },
  ],
  sections: [
    {
      sectionId: 9,
      title: 'Reading & Writing',
      paperTitle: 'Mix – Version 1',
      paperMixTitle: 'Mix',
      versions: [1, 2, 3, 4].map((version) => ({ version, paperId: 20 + version, title: `Mix – Version ${version}`, pattern: version % 2 ? 'ABA' : 'BAB' })),
    },
    { sectionId: 10, title: 'Listening', paperTitle: 'Listening 1', paperMixTitle: null, versions: [] },
  ],
}

describe('RoomPlanPage (VE-67)', () => {
  afterEach(() => vi.restoreAllMocks())

  it('lays the computers out row by row with each candidate and version, and explains the versions', async () => {
    vi.spyOn(staffApi, 'roomPlan').mockResolvedValue(plan)
    render(<RoomPlanPage examId={5} />)

    const computers = await screen.findByRole('list', { name: 'Computers' })
    expect(computers.style.gridTemplateColumns).toBe('repeat(2, minmax(120px, 1fr))')
    expect(within(screen.getByRole('listitem', { name: 'Computer 2' })).getByText('V2')).toBeInTheDocument()
    expect(within(screen.getByRole('listitem', { name: 'Computer 3' })).getByText('empty')).toBeInTheDocument()
    expect(within(screen.getByRole('listitem', { name: 'Computer 4' })).getByText('Chi Lê')).toBeInTheDocument()
    expect(screen.getAllByText('Part 1 A · Part 2 B · Part 3 A')).toHaveLength(2)
    expect(screen.getByText(/1 candidate has no seat number/)).toBeInTheDocument()
  })
})
