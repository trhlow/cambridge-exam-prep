import type { MonitorRow } from './api'
import { actionAvailability, appliedIds, clockText, endNowText, extraTimeText, rowAt } from './monitorLogic'

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
    const selected = [row({ candidateId: 1 }), row({ candidateId: 2, isOnline: false }), row({ candidateId: 3, status: 'Paused' }), row({ candidateId: 4, status: 'Paused', isOnline: false })]

    const availability = actionAvailability(selected, false)

    expect(availability.Pause).toBe(1)
    expect(availability.Resume).toBe(1)
    expect(availability.AddIncidentTime).toBe(4)
    expect(availability.EditEndTime).toBe(4)
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

describe('appliedIds', () => {
  it('leaves out the candidates the server skipped', () => {
    const outcome = { applied: 2, skipped: [{ candidateId: 2, fullName: 'Binh Tran', reason: 'cannot be rewound further than the recording has played (1 min)' }] }

    expect(appliedIds([1, 2, 3], outcome)).toEqual([1, 3])
    expect(appliedIds([2], outcome)).toEqual([])
  })
})

describe('endNowText', () => {
  const outcome = (skippedIds: number[] = []) => ({
    applied: 0,
    skipped: skippedIds.map((candidateId) => ({ candidateId, fullName: 'x', reason: 'has already finished this section' })),
  })

  it('says how many ended and how many keep working on extra time', () => {
    const selected = [row({ candidateId: 1 }), row({ candidateId: 2, plannedExtraSeconds: 900 }), row({ candidateId: 3, incidentExtraSeconds: 300 })]

    expect(endNowText(selected, outcome(), false)).toBe('End now: 1 candidate ended; 2 keep working on their extra time.')
  })

  it('says paused candidates with extra time were resumed', () => {
    const selected = [row({ candidateId: 1, status: 'Paused', plannedExtraSeconds: 900 }), row({ candidateId: 2, status: 'Paused' })]

    expect(endNowText(selected, outcome(), false)).toBe('End now: 1 candidate ended; 1 keeps working on their extra time (1 was paused and has been resumed).')
  })

  it('ends everyone when extra time is disregarded, and ignores skipped candidates', () => {
    const selected = [row({ candidateId: 1, plannedExtraSeconds: 900 }), row({ candidateId: 2, plannedExtraSeconds: 900 }), row({ candidateId: 3 })]

    expect(endNowText(selected, outcome([3]), true)).toBe('End now: 2 candidates ended.')
  })
})
