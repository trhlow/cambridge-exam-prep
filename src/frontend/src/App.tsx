import { useEffect, useState } from 'react'
import { candidateApi } from './api/client'
import type { CandidateProfile, CandidateSection } from './api/types'
import { ExamScreen } from './exam/ExamScreen'
import { InstructionsScreen, LoggedInScreen, LoginScreen, PinScreen, ReadinessScreen, SectionsScreen } from './screens/SignInScreens'

type Stage =
  | { kind: 'loading' }
  | { kind: 'login'; notice?: string }
  | { kind: 'loggedIn'; profile: CandidateProfile }
  | { kind: 'pin'; profile: CandidateProfile }
  | { kind: 'sections'; profile: CandidateProfile; notice?: string }
  | { kind: 'readiness'; profile: CandidateProfile }
  | { kind: 'instructions'; profile: CandidateProfile; section: CandidateSection }
  | { kind: 'exam'; profile: CandidateProfile; section: CandidateSection }

const afterSignIn = (profile: CandidateProfile): Stage => (profile.pinVerified ? { kind: 'sections', profile } : { kind: 'pin', profile })

/** Said on the dashboard when a component closes, by time or by the invigilator (Inspera returns there). */
const FINISHED_NOTICE = 'The test has now finished. Your answers have been saved.'
const EXIT_NOTICE = 'You have finished the exam. Please wait quietly until your invigilator says you can leave.'

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

/**
 * Candidate flow, as in the Cambridge exam day instructions: printed login → Next → invigilator's PIN ("Unlock and
 * Select") → Next → dashboard → readiness check → for each component its instructions ("I confirm", "Start …") → test
 * → back to the dashboard → Exit after the last component. A reload resumes from the cookie.
 */
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
      return <LoginScreen notice={stage.notice} onSignedIn={(profile) => setStage(profile.pinVerified ? afterSignIn(profile) : { kind: 'loggedIn', profile })} />
    case 'loggedIn':
      return <LoggedInScreen profile={stage.profile} onNext={() => setStage({ kind: 'pin', profile: stage.profile })} />
    case 'pin':
      return <PinScreen profile={stage.profile} onVerified={(profile) => setStage(afterSignIn(profile))} />
    case 'sections':
      return (
        <SectionsScreen
          profile={stage.profile}
          notice={stage.notice}
          onReadiness={() => setStage({ kind: 'readiness', profile: stage.profile })}
          onStart={(section) => {
            if (section.status === 'NotStarted') {
              setStage({ kind: 'instructions', profile: stage.profile, section })
            } else {
              rememberOpenSection(section.id)
              setStage({ kind: 'exam', profile: stage.profile, section })
            }
          }}
          onExit={() => {
            rememberOpenSection(null)
            void candidateApi.logout().finally(() => setStage({ kind: 'login', notice: EXIT_NOTICE }))
          }}
        />
      )
    case 'readiness':
      return <ReadinessScreen profile={stage.profile} onDone={() => setStage({ kind: 'sections', profile: stage.profile })} />
    case 'instructions':
      return (
        <InstructionsScreen
          profile={stage.profile}
          section={stage.section}
          onBack={() => setStage({ kind: 'sections', profile: stage.profile })}
          onStart={() => {
            rememberOpenSection(stage.section.id)
            setStage({ kind: 'exam', profile: stage.profile, section: stage.section })
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
            setStage({ kind: 'sections', profile: stage.profile, notice: FINISHED_NOTICE })
          }}
          onSignedOut={() => setStage({ kind: 'login', notice: SIGNED_OUT_NOTICE })}
        />
      )
  }
}
