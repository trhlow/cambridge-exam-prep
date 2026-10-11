import { useEffect, useState } from 'react'
import { candidateApi } from './api/client'
import type { CandidateProfile, CandidateSection } from './api/types'
import { ExamScreen } from './exam/ExamScreen'
import { LoginScreen, PinScreen, SectionsScreen } from './screens/SignInScreens'

type Stage =
  | { kind: 'loading' }
  | { kind: 'login' }
  | { kind: 'pin'; profile: CandidateProfile }
  | { kind: 'sections'; profile: CandidateProfile }
  | { kind: 'exam'; profile: CandidateProfile; section: CandidateSection }

const afterSignIn = (profile: CandidateProfile): Stage => (profile.pinVerified ? { kind: 'sections', profile } : { kind: 'pin', profile })

/** Candidate flow: printed login → invigilator's PIN → sections → test. A reload resumes from the cookie. */
export default function App() {
  const [stage, setStage] = useState<Stage>({ kind: 'loading' })

  useEffect(() => {
    candidateApi.me().then(
      (profile) => setStage(afterSignIn(profile)),
      // Not signed in (401) or the session ended: start from the login slip again.
      () => setStage({ kind: 'login' }),
    )
  }, [])

  switch (stage.kind) {
    case 'loading':
      return <div className="screen-message">Loading…</div>
    case 'login':
      return <LoginScreen onSignedIn={(profile) => setStage(afterSignIn(profile))} />
    case 'pin':
      return <PinScreen profile={stage.profile} onVerified={(profile) => setStage(afterSignIn(profile))} />
    case 'sections':
      return <SectionsScreen profile={stage.profile} onStart={(section) => setStage({ kind: 'exam', profile: stage.profile, section })} />
    case 'exam':
      return <ExamScreen profile={stage.profile} section={stage.section} onFinished={() => setStage({ kind: 'sections', profile: stage.profile })} />
  }
}
