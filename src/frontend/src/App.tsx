import { useEffect, useState } from 'react'
import { candidateApi } from './api/client'
import type { CandidateProfile, CandidateSection } from './api/types'
import { ExamScreen } from './exam/ExamScreen'
import { LoginScreen, PinScreen, SectionsScreen } from './screens/SignInScreens'

type Stage =
  | { kind: 'loading' }
  | { kind: 'login'; notice?: string }
  | { kind: 'pin'; profile: CandidateProfile }
  | { kind: 'sections'; profile: CandidateProfile }
  | { kind: 'exam'; profile: CandidateProfile; section: CandidateSection }

const afterSignIn = (profile: CandidateProfile): Stage => (profile.pinVerified ? { kind: 'sections', profile } : { kind: 'pin', profile })

/** VE-43: the candidate logged in on another computer (or the session ended); this one stops at once. */
const SIGNED_OUT_NOTICE = 'You have been signed out on this computer. If you moved to another computer, continue there. Otherwise, log in again or ask your invigilator.'

/** The section open in this tab, so a reload (F5) goes straight back into the test, as in Inspera. */
const OPEN_SECTION_KEY = 'victory.openSection'

function rememberOpenSection(sectionId: number | null) {
  try {
    if (sectionId === null) {
      sessionStorage.removeItem(OPEN_SECTION_KEY)
    } else {
      sessionStorage.setItem(OPEN_SECTION_KEY, String(sectionId))
    }
  } catch {
    // Storage blocked: a reload then shows the list of sections instead.
  }
}

/** After a reload: back into the section that was open, if it is still running. */
async function resume(profile: CandidateProfile): Promise<Stage> {
  const stored = (() => {
    try {
      return Number(sessionStorage.getItem(OPEN_SECTION_KEY))
    } catch {
      return 0
    }
  })()
  if (!profile.pinVerified || !stored) {
    return afterSignIn(profile)
  }

  const sections = await candidateApi.sections().catch(() => [])
  const section = sections.find((s) => s.id === stored && (s.status === 'InProgress' || s.status === 'Paused'))
  return section ? { kind: 'exam', profile, section } : afterSignIn(profile)
}

/** Candidate flow: printed login → invigilator's PIN → sections → test. A reload resumes from the cookie. */
export default function App() {
  const [stage, setStage] = useState<Stage>({ kind: 'loading' })

  useEffect(() => {
    candidateApi.me().then(
      (profile) => resume(profile).then(setStage),
      // Not signed in (401) or the session ended: start from the login slip again.
      () => setStage({ kind: 'login' }),
    )
  }, [])

  switch (stage.kind) {
    case 'loading':
      return <div className="screen-message">Loading…</div>
    case 'login':
      return <LoginScreen notice={stage.notice} onSignedIn={(profile) => setStage(afterSignIn(profile))} />
    case 'pin':
      return <PinScreen profile={stage.profile} onVerified={(profile) => setStage(afterSignIn(profile))} />
    case 'sections':
      return (
        <SectionsScreen
          profile={stage.profile}
          onStart={(section) => {
            rememberOpenSection(section.id)
            setStage({ kind: 'exam', profile: stage.profile, section })
          }}
        />
      )
    case 'exam':
      return (
        <ExamScreen
          profile={stage.profile}
          section={stage.section}
          onProfileChange={(profile) => setStage({ ...stage, profile })}
          onFinished={() => {
            rememberOpenSection(null)
            setStage({ kind: 'sections', profile: stage.profile })
          }}
          onSignedOut={() => setStage({ kind: 'login', notice: SIGNED_OUT_NOTICE })}
        />
      )
  }
}
