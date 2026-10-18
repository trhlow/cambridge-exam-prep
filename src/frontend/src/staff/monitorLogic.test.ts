import type { MonitorRow } from './api'
import { actionAvailability, clockText, extraTimeText, rowAt } from './monitorLogic'

const row = (overrides: Partial<MonitorRow>): MonitorRow => ({
  candidateId: 1,
  candidateNumber: '0001',
  fullName: 'An Nguyen',
  username: 'nguyea0001',
  attemptId: 10,
  status: 'InProgress',
  isOnline: true,
  startedAt: '2026-10-11T08:00:00Z',
  endsAt: '2026-10-11T09:00:00Z',
  remainingSeconds: 1800,
  plannedExtraSeconds: 0,
  incidentExtraSeconds: 0,
  endTimeEdited: false,
  submitReason: null,
  audioPositionSeconds: null,
  answered: 3,
  warnings: 0,
  ...overrides,
})

describe('actionAvailability', () => {
  it('pauses only online candidates who are working, and resumes only paused ones', () => {
    const selected = [row({ candidateId: 1 }), row({ candidateId: 2, isOnline: false }), row({ candidateId: 3, status: 'Paused' })]

    const availability = actionAvailability(selected, false)

    expect(availability.Pause).toBe(1)
    expect(availability.Resume).toBe(1)
    expect(availability.AddIncidentTime).toBe(3)
    expect(availability.EditEndTime).toBe(3)
  })

  it('offers nothing for candidates who have not started or have finished', () => {
    const availability = actionAvailability([row({ status: 'NotStarted', attemptId: null }), row({ status: 'Submitted' })], true)

    expect(Object.values(availability).every((count) => count === 0)).toBe(true)
  })

  it('rewinds only Listening candidates whose recording has started and who are not paused', () => {
    const selected = [row({ audioPositionSeconds: 180 }), row({ audioPositionSeconds: null }), row({ status: 'Paused', audioPositionSeconds: 60 })]

    expect(actionAvailability(selected, true).RewindAudio).toBe(1)
    expect(actionAvailability(selected, false).RewindAudio).toBe(0)
  })
})

describe('clockText', () => {
  it('shows minutes and seconds, with hours when needed', () => {
    expect(clockText(0)).toBe('0:00')
    expect(clockText(65)).toBe('1:05')
    expect(clockText(3725)).toBe('1:02:05')
    expect(clockText(null)).toBe('–')
  })
})

describe('extraTimeText', () => {
  it('rounds seconds to whole minutes and hides zero', () => {
    expect(extraTimeText(0)).toBe('')
    expect(extraTimeText(300)).toBe('+5 min')
    expect(extraTimeText(450)).toBe('+8 min')
  })
})

describe('rowAt', () => {
  const running = { status: 'InProgress', remainingSeconds: 100, audioPositionSeconds: 30 } as MonitorRow

  it('runs a working candidate’s clock and recording on between refreshes', () => {
    expect(rowAt(running, 7)).toMatchObject({ remainingSeconds: 93, audioPositionSeconds: 37 })
    expect(rowAt(running, 500).remainingSeconds).toBe(0)
  })

  it('keeps a paused candidate still', () => {
    const paused = { ...running, status: 'Paused' } as MonitorRow
    expect(rowAt(paused, 7)).toBe(paused)
  })
})
