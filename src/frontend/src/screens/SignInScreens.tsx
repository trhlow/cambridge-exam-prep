import { useEffect, useState, type FormEvent } from 'react'
import { candidateApi } from '../api/client'
import type { CandidateProfile, CandidateSection } from '../api/types'
import { VictoryLogo } from '../ui/VictoryLogo'

function messageOf(error: unknown): string {
  return error instanceof Error ? error.message : 'Something went wrong. Please ask your invigilator.'
}

function Frame({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="signin">
      <header className="signin-header">
        <VictoryLogo />
      </header>
      <main className="signin-card">
        <h1>{title}</h1>
        {children}
      </main>
    </div>
  )
}

/** Step 1: the username and password printed on the candidate's login slip. */
export function LoginScreen({ onSignedIn }: { onSignedIn: (profile: CandidateProfile) => void }) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isBusy, setIsBusy] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setIsBusy(true)
    setError(null)
    try {
      onSignedIn(await candidateApi.login(username.trim(), password))
    } catch (e) {
      setError(messageOf(e))
    } finally {
      setIsBusy(false)
    }
  }

  return (
    <Frame title="Log in">
      <form onSubmit={submit} className="signin-form">
        <label>
          Username
          <input value={username} onChange={(e) => setUsername(e.target.value)} autoComplete="off" autoCapitalize="off" spellCheck={false} required />
        </label>
        <label>
          Password
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="off" required />
        </label>
        {error && <p className="form-error" role="alert">{error}</p>}
        <button type="submit" className="btn btn--primary" disabled={isBusy}>
          Log in
        </button>
      </form>
    </Frame>
  )
}

/** Step 2: the session PIN the invigilator reads out once the whole room has logged in. */
export function PinScreen({ profile, onVerified }: { profile: CandidateProfile; onVerified: (profile: CandidateProfile) => void }) {
  const [pin, setPin] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)
    try {
      onVerified(await candidateApi.enterPin(pin.trim()))
    } catch (e) {
      setError(messageOf(e))
    }
  }

  return (
    <Frame title={profile.examName}>
      <p className="signin-who">
        {profile.fullName} · {profile.candidateNumber}
      </p>
      <form onSubmit={submit} className="signin-form">
        <label>
          Enter the PIN your invigilator gives you
          <input value={pin} onChange={(e) => setPin(e.target.value)} inputMode="numeric" autoComplete="off" required />
        </label>
        {error && <p className="form-error" role="alert">{error}</p>}
        <button type="submit" className="btn btn--primary">
          Start
        </button>
      </form>
    </Frame>
  )
}

/** Step 3: the sections of the exam in order (A2: Reading & Writing, then Listening). */
export function SectionsScreen({ profile, onStart }: { profile: CandidateProfile; onStart: (section: CandidateSection) => void }) {
  const [sections, setSections] = useState<CandidateSection[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    candidateApi.sections().then(setSections, (e: unknown) => setError(messageOf(e)))
  }, [])

  return (
    <Frame title={profile.examName}>
      <p className="signin-who">
        {profile.fullName} · {profile.candidateNumber}
      </p>
      {error && <p className="form-error" role="alert">{error}</p>}
      <ul className="section-list">
        {(sections ?? []).map((section, i) => {
          const isLocked = section.status === 'Locked'
          const isDone = section.status === 'Submitted'
          const previous = sections?.[i - 1]
          const label = isLocked ? `${section.title} is locked` : isDone ? `${section.title} submitted` : `Open ${section.title}`
          return (
            <li key={section.id} className={isLocked ? 'section-item is-locked' : 'section-item'}>
              <div>
                <div className="section-title">{section.title}</div>
                <div className="section-meta">
                  {isLocked && previous ? `Opens when ${previous.title} has finished` : `${section.durationMinutes} minutes`}
                </div>
              </div>
              <button type="button" className="btn btn--primary" aria-label={label} onClick={() => onStart(section)} disabled={isLocked || isDone}>
                {isLocked ? 'Locked' : isDone ? 'Submitted' : 'Open'}
              </button>
            </li>
          )
        })}
      </ul>
    </Frame>
  )
}
