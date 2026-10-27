import type { CandidateProfile } from '../api/types'

const readinessKey = (profile: CandidateProfile) => `victory.readiness.${profile.examName}/${profile.candidateNumber}`

/** Whether this candidate has confirmed the readiness check for this exam on this computer. */
export function hasDoneReadiness(profile: CandidateProfile): boolean {
  try {
    return localStorage.getItem(readinessKey(profile)) === 'done'
  } catch {
    return false
  }
}

export function rememberReadiness(profile: CandidateProfile) {
  try {
    localStorage.setItem(readinessKey(profile), 'done')
  } catch {
    // Storage blocked: the readiness check is offered again after a reload.
  }
}
