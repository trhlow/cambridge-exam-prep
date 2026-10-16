import type { ManagerArea, StaffProfile } from './api'

/**
 * What the portal offers a staff member, mirroring the API's rules (StaffAuthenticationSetup policies, ExamAccess):
 * administrators work in every area; managers only in theirs and never delete on their own; invigilators run the
 * room; examiners review finished work and mark Writing. The API enforces all of it — this only hides what a role
 * cannot use.
 */
export interface Access {
  userId: number
  isAdmin: boolean
  worksIn: (area: ManagerArea) => boolean
  /** Every session (managers of exams or candidates) or the assigned ones (invigilators, examiners). */
  seesSessions: boolean
  seesCandidates: boolean
  printsSlips: boolean
  monitors: boolean
  seesResults: boolean
  marks: boolean
  /** Files requests that wait for a second administrator, and so has an approvals page. */
  requestsApprovals: boolean
}

export function accessOf(user: StaffProfile): Access {
  const isAdmin = user.role === 'Admin'
  const worksIn = (area: ManagerArea) => isAdmin || (user.role === 'Manager' && user.areas.includes(area))
  const organises = worksIn('Exams') || worksIn('Candidates')
  return {
    userId: user.id,
    isAdmin,
    worksIn,
    seesSessions: organises || user.role === 'Invigilator' || user.role === 'Examiner',
    seesCandidates: organises,
    printsSlips: organises || user.role === 'Invigilator',
    monitors: worksIn('Exams') || user.role === 'Invigilator',
    seesResults: isAdmin || user.role === 'Examiner',
    marks: user.role === 'Examiner',
    requestsApprovals: isAdmin || user.role === 'Manager',
  }
}
