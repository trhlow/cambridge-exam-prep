import type { ActionLogRow } from './api'
import { actionDetail, actionLabel, actionLogCsv } from './actionLog'

const row = (over: Partial<ActionLogRow>): ActionLogRow => ({
  id: 1,
  at: '2026-11-25T02:00:00Z',
  action: 'Pause',
  by: 'Giám thị',
  candidateNumber: '0001',
  candidate: 'Trần Hoàng Long',
  section: 'Reading & Writing',
  minutes: null,
  endAt: null,
  disregardExtraTime: false,
  wholeTest: false,
  message: null,
  recipients: null,
  reason: null,
  ...over,
})

describe('invigilator log', () => {
  it('says what each action changed', () => {
    expect(actionLabel(row({ action: 'AddIncidentTime' }))).toBe('Incident extra time')
    expect(actionDetail(row({ action: 'AddIncidentTime', minutes: 5 }))).toBe('+5 min')
    expect(actionDetail(row({ action: 'RewindAudio', minutes: 2 }))).toBe('2 min back')
    expect(actionDetail(row({ action: 'EndIn', minutes: 10 }))).toBe('in 10 min')
    expect(actionDetail(row({ action: 'EndNow', disregardExtraTime: true }))).toBe('extra time disregarded')
    expect(actionDetail(row({ action: 'UseTestEndTime', wholeTest: true }))).toBe('whole test')
    expect(actionDetail(row({ action: 'EndAll', wholeTest: true }))).toBe('')
    expect(actionDetail(row({ action: 'SendMessage', message: 'Ten minutes left.', recipients: 0 }))).toBe('“Ten minutes left.” to the whole room')
    expect(actionDetail(row({ action: 'SendMessage', message: 'Hi', recipients: 2 }))).toBe('“Hi” to 2 candidates')
  })

  it('downloads as CSV for Excel with Vietnamese names and no formulas', () => {
    const csv = actionLogCsv([row({ reason: '=HYPERLINK("x")' })])

    expect(csv.charCodeAt(0)).toBe(0xfeff)
    const [header, line] = csv.slice(1).split('\r\n')
    expect(header).toBe('"Time","Action","Details","Reason","Candidate number","Candidate","Section","By"')
    expect(line).toContain('"Paused"')
    expect(line).toContain(`"'=HYPERLINK(""x"")"`)
    expect(line).toContain('"Trần Hoàng Long"')
  })
})
