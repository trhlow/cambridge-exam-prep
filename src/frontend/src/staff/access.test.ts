import { accessOf } from './access'
import type { ManagerArea, Role, StaffProfile } from './api'

const staff = (role: Role, areas: ManagerArea[] = []): StaffProfile => ({ id: 7, username: 'u', fullName: 'U', email: null, role, isActive: true, areas })

describe('accessOf', () => {
  it('lets administrators work everywhere but never mark', () => {
    const admin = accessOf(staff('Admin'))
    expect(['Papers', 'Exams', 'Candidates', 'Centre'].every((a) => admin.worksIn(a as ManagerArea))).toBe(true)
    expect(admin.seesResults).toBe(true)
    expect(admin.marks).toBe(false)
    expect(admin.requestsApprovals).toBe(true)
  })

  it('keeps managers to their areas', () => {
    const papers = accessOf(staff('Manager', ['Papers']))
    expect(papers.worksIn('Papers')).toBe(true)
    expect(papers.worksIn('Exams')).toBe(false)
    expect(papers.seesSessions).toBe(false)
    expect(papers.seesResults).toBe(false)

    const candidates = accessOf(staff('Manager', ['Candidates']))
    expect(candidates.seesSessions).toBe(true)
    expect(candidates.seesCandidates).toBe(true)
    expect(candidates.monitors).toBe(false)
    expect(accessOf(staff('Manager', ['Exams'])).monitors).toBe(true)
  })

  it('gives invigilators the room and examiners the marking', () => {
    const invigilator = accessOf(staff('Invigilator'))
    expect(invigilator.monitors && invigilator.printsSlips).toBe(true)
    expect(invigilator.seesResults || invigilator.requestsApprovals).toBe(false)

    const examiner = accessOf(staff('Examiner'))
    expect(examiner.marks && examiner.seesResults && examiner.seesSessions).toBe(true)
    expect(examiner.monitors || examiner.printsSlips || examiner.seesCandidates).toBe(false)
  })
})
