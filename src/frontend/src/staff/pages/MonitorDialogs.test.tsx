import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { staffApi, type MonitorRow } from '../api'
import { ActionDialog } from './MonitorDialogs'

const row = (candidateId: number, overrides: Partial<MonitorRow> = {}): MonitorRow => ({
  candidateId,
  candidateNumber: `000${candidateId}`,
  fullName: `Candidate ${candidateId}`,
  username: `cand${candidateId}`,
  attemptId: 10 + candidateId,
  status: 'InProgress',
  isOnline: true,
  startedAt: '2026-11-20T08:00:00Z',
  endsAt: '2026-11-20T09:00:00Z',
  remainingSeconds: 1800,
  plannedExtraSeconds: 0,
  incidentExtraSeconds: 0,
  endTimeEdited: false,
  submitReason: null,
  audioPositionSeconds: 120,
  answered: 3,
  warnings: 0,
  ...overrides,
})

afterEach(() => vi.restoreAllMocks())

describe('ActionDialog', () => {
  it('sends a message once however many times Send is pressed (double click)', async () => {
    let finish: () => void = () => undefined
    const send = vi.spyOn(staffApi, 'sendMessage').mockImplementation(() => new Promise((resolve) => { finish = () => resolve([]) }))
    render(<ActionDialog dialog={{ kind: 'message', toRoom: true }} examId={4} sectionId={9} selected={[]} onClose={() => undefined} onDone={() => undefined} />)

    await userEvent.type(screen.getByLabelText('Message'), 'Ten minutes left.')
    await userEvent.dblClick(screen.getByRole('button', { name: 'Send' }))
    finish()

    expect(send).toHaveBeenCalledTimes(1)
  })

  it('offers incident time after a rewind only to the candidates that were rewound', async () => {
    vi.spyOn(staffApi, 'act').mockResolvedValue({ applied: 1, skipped: [{ candidateId: 2, fullName: 'Candidate 2', reason: 'cannot be rewound further than the recording has played (1 min)' }] })
    const onDone = vi.fn()
    const selected = [row(1), row(2)]
    render(<ActionDialog dialog={{ kind: 'RewindAudio' }} examId={4} sectionId={9} selected={selected} onClose={() => undefined} onDone={onDone} />)

    await userEvent.click(screen.getByRole('button', { name: 'Rewind now' }))

    expect(onDone).toHaveBeenCalledTimes(1)
    expect(onDone.mock.calls[0][1]).toMatchObject({ kind: 'AddIncidentTime', minutes: 5, candidateIds: [1] })
  })

  it('applies that incident time to the rewound candidates alone', async () => {
    const act = vi.spyOn(staffApi, 'act').mockResolvedValue({ applied: 1, skipped: [] })
    render(<ActionDialog dialog={{ kind: 'AddIncidentTime', minutes: 5, reason: 'Headset or audio problem', candidateIds: [1] }} examId={4} sectionId={9} selected={[row(1), row(2)]} onClose={() => undefined} onDone={() => undefined} />)

    expect(screen.getByText('For 1 selected candidate.')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Update' }))

    expect(act).toHaveBeenCalledWith(4, 9, expect.objectContaining({ action: 'AddIncidentTime', candidateIds: [1] }))
  })

  it('says how many ended and how many keep working on extra time after End now', async () => {
    vi.spyOn(staffApi, 'act').mockResolvedValue({ applied: 2, skipped: [] })
    const onDone = vi.fn()
    render(<ActionDialog dialog={{ kind: 'EditEndTime' }} examId={4} sectionId={9} selected={[row(1), row(2, { plannedExtraSeconds: 900, status: 'Paused' })]} onClose={() => undefined} onDone={onDone} />)

    await userEvent.click(screen.getByLabelText('End now'))
    await userEvent.click(screen.getByRole('button', { name: 'End now' }))
    await userEvent.click(screen.getByRole('button', { name: 'Click again to end now' }))

    expect(onDone.mock.calls[0][0].text).toBe('End now: 1 candidate ended; 1 keeps working on their extra time (1 was paused and has been resumed).')
  })
})
