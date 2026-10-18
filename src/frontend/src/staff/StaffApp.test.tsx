import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { staffApi, type ApprovalView, type ManagerArea, type Role, type StaffProfile } from './api'
import StaffApp from './StaffApp'

const profile = (id: number, role: Role, areas: ManagerArea[] = []): StaffProfile => ({ id, username: 'u', fullName: `Staff ${id}`, email: null, role, isActive: true, areas })

const request = (requestedById: number): ApprovalView => ({
  id: 9,
  kind: 'DeleteExam',
  targetId: 4,
  summary: 'Delete exam “A2 October”',
  status: 'Pending',
  requestedById,
  requestedBy: `Staff ${requestedById}`,
  requestedAt: '2026-10-16T07:00:00Z',
  decidedById: null,
  decidedBy: null,
  decidedAt: null,
  decisionNote: null,
  outcome: null,
})

function signIn(user: StaffProfile, path = '/staff') {
  window.history.pushState(null, '', path)
  vi.spyOn(staffApi, 'me').mockResolvedValue(user)
  vi.spyOn(staffApi, 'exams').mockResolvedValue([])
  render(<StaffApp />)
}

const menu = async () => within(await screen.findByRole('navigation', { name: 'Staff portal' })).getAllByRole('link').map((l) => l.textContent)

describe('StaffApp menu (VE-59, VE-61)', () => {
  afterEach(() => {
    vi.restoreAllMocks()
    vi.unstubAllGlobals()
  })

  it('gives administrators every page', async () => {
    signIn(profile(1, 'Admin'))
    expect(await menu()).toEqual(['Exams', 'Papers', 'Centre', 'Staff', 'Approvals'])
  })

  it('gives a manager only their areas and their requests', async () => {
    signIn(profile(2, 'Manager', ['Exams']))
    expect(await menu()).toEqual(['Exams', 'Approvals'])
  })

  it('starts a manager of the centre on the centre page', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify({ centreName: 'Victory', logoUrl: null, updatedAt: '2026-10-18T00:00:00Z' }), { status: 200 })))
    signIn(profile(4, 'Manager', ['Centre']))
    expect(await menu()).toEqual(['Centre', 'Approvals'])
    expect(await screen.findByRole('heading', { name: 'Centre' })).toBeInTheDocument()
  })

  it('gives examiners and invigilators their sessions only', async () => {
    signIn(profile(3, 'Examiner'))
    expect(await menu()).toEqual(['Exams'])
    expect(await screen.findByText('Sessions you are marking')).toBeInTheDocument()
  })
})

describe('ApprovalsPage (VE-60)', () => {
  afterEach(() => vi.restoreAllMocks())

  it('lets an administrator decide another person’s request', async () => {
    vi.spyOn(staffApi, 'approvals').mockResolvedValue([request(2)])
    const approve = vi.spyOn(staffApi, 'approve').mockResolvedValue({ ...request(2), status: 'Approved' })
    signIn(profile(1, 'Admin'), '/staff/approvals')

    await userEvent.click(await screen.findByRole('button', { name: 'Approve' }))
    await userEvent.click(screen.getByRole('button', { name: 'Click again to approve' }))

    expect(approve).toHaveBeenCalledWith(9)
    expect(screen.getByRole('button', { name: 'Reject' })).toBeInTheDocument()
  })

  it('never offers the requester a decision, only a cancel', async () => {
    vi.spyOn(staffApi, 'approvals').mockResolvedValue([request(1)])
    signIn(profile(1, 'Admin'), '/staff/approvals')

    expect(await screen.findByRole('button', { name: 'Cancel request' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Approve' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Reject' })).not.toBeInTheDocument()
  })
})
