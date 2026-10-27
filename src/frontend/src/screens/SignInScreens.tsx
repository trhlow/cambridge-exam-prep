import { useEffect, useRef, useState, type FormEvent } from 'react'
import { candidateApi } from '../api/client'
import type { CandidateProfile, CandidateSection } from '../api/types'
import { CandidateIcon } from '../ui/CandidateIcon'
import { ArrowIcon } from '../ui/icons'
import { VictoryLogo } from '../ui/VictoryLogo'
import { hasDoneReadiness, rememberReadiness } from './readiness'

// The steps before a test follow the candidate instructions of the Cambridge Exam Day Booklet for Digital exams
// (2026, pp. 11–13): log in and click Next, enter the PIN and click "Unlock and Select" then Next, check the name
// under the candidate icon, watch the readiness check and click Confirm and the forward arrow, then for each
// component read the instructions, click "I confirm" and "Start (name of the test)".

function messageOf(error: unknown): string {
  return error instanceof Error ? error.message : 'Something went wrong. Please ask your invigilator.'
}

function Frame({ title, profile, children }: { title: string; profile?: CandidateProfile; children: React.ReactNode }) {
  return (
    <div className="signin">
      <header className="signin-header">
        <VictoryLogo />
        {profile && <CandidateIcon fullName={profile.fullName} candidateNumber={profile.candidateNumber} />}
      </header>
      <main className="signin-card">
        <h1>{title}</h1>
        {children}
      </main>
    </div>
  )
}

/** Step 1: the username and password printed on the candidate's login slip; `notice` says why the test closed. */
export function LoginScreen({ onSignedIn, notice }: { onSignedIn: (profile: CandidateProfile) => void; notice?: string }) {
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
      {notice && (
        <p className="signin-notice" role="status">
          {notice}
        </p>
      )}
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
          Login
        </button>
      </form>
    </Frame>
  )
}

/** After Login: the candidate clicks Next and waits for the invigilator to read out the PIN. */
export function LoggedInScreen({ profile, onNext }: { profile: CandidateProfile; onNext: () => void }) {
  return (
    <Frame title={profile.examName} profile={profile}>
      <p className="signin-who">
        {profile.fullName} · {profile.candidateNumber}
      </p>
      <p>You are logged in. Click Next and wait for your invigilator&apos;s instructions.</p>
      <button type="button" className="btn btn--primary" onClick={onNext}>
        Next
      </button>
    </Frame>
  )
}

/** Step 2: the PIN code the invigilator reads out once the whole room has logged in; then Next. */
export function PinScreen({ profile, onVerified }: { profile: CandidateProfile; onVerified: (profile: CandidateProfile) => void }) {
  const [pin, setPin] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [unlocked, setUnlocked] = useState<CandidateProfile | null>(null)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)
    try {
      setUnlocked(await candidateApi.enterPin(pin.trim()))
    } catch (e) {
      setError(messageOf(e))
    }
  }

  if (unlocked) {
    return (
      <Frame title={unlocked.examName} profile={unlocked}>
        <p className="signin-notice" role="status">
          {unlocked.examName} is unlocked.
        </p>
        <button type="button" className="btn btn--primary" onClick={() => onVerified(unlocked)}>
          Next
        </button>
      </Frame>
    )
  }

  return (
    <Frame title={profile.examName} profile={profile}>
      <p className="signin-who">
        {profile.fullName} · {profile.candidateNumber}
      </p>
      <form onSubmit={submit} className="signin-form">
        <label>
          PIN code
          <input value={pin} onChange={(e) => setPin(e.target.value)} inputMode="numeric" autoComplete="off" required />
        </label>
        {error && <p className="form-error" role="alert">{error}</p>}
        <button type="submit" className="btn btn--primary">
          Unlock and Select
        </button>
      </form>
    </Frame>
  )
}

/** The candidate's test dashboard: the readiness check, then the components in their fixed order, then Exit. */
export function SectionsScreen(props: {
  profile: CandidateProfile
  onStart: (section: CandidateSection) => void
  onReadiness: () => void
  onExit: () => void
  /** Said once when a component has just closed (time up or ended by the invigilator). */
  notice?: string
}) {
  const { profile, onStart, onReadiness, onExit, notice } = props
  const [sections, setSections] = useState<CandidateSection[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const isReady = hasDoneReadiness(profile)

  useEffect(() => {
    candidateApi.sections().then(setSections, (e: unknown) => setError(messageOf(e)))
  }, [])

  const allSubmitted = sections !== null && sections.length > 0 && sections.every((s) => s.status === 'Submitted')

  return (
    <Frame title={profile.examName} profile={profile}>
      {notice && (
        <p className="signin-notice" role="status">
          {notice}
        </p>
      )}
      {error && <p className="form-error" role="alert">{error}</p>}
      <ul className="section-list">
        <li className="section-item">
          <div>
            <div className="section-title">Readiness check</div>
            <div className="section-meta">{isReady ? 'Done' : 'Watch this before you start the test'}</div>
          </div>
          <button type="button" className={isReady ? 'btn' : 'btn btn--primary'} onClick={onReadiness}>
            {isReady ? 'Watch again' : 'Start'}
          </button>
        </li>
        {(sections ?? []).map((section, i) => {
          const isLocked = section.status === 'Locked'
          const isDone = section.status === 'Submitted'
          const previous = sections?.[i - 1]
          const label = isLocked ? `${section.title} is locked` : isDone ? `${section.title} submitted` : `Open ${section.title}`
          return (
            <li key={section.id} className={isLocked || !isReady ? 'section-item is-locked' : 'section-item'}>
              <div>
                <div className="section-title">{section.title}</div>
                <div className="section-meta">
                  {isLocked && previous ? `Opens when ${previous.title} has finished` : isDone ? 'Submitted' : `${section.durationMinutes} minutes`}
                </div>
              </div>
              <button type="button" className="btn btn--primary" aria-label={label} onClick={() => onStart(section)} disabled={isLocked || isDone || !isReady}>
                {isLocked ? 'Locked' : isDone ? 'Submitted' : 'Open'}
              </button>
            </li>
          )
        })}
      </ul>
      {allSubmitted && (
        <div className="signin-finish">
          <p>The test is over. Click Exit, take off your headphones and wait for your invigilator.</p>
          <button type="button" className="btn btn--primary" onClick={onExit}>
            Exit
          </button>
        </div>
      )}
    </Frame>
  )
}

/** A short beep to check the headphones, made in the browser so no recording is needed. */
function playTestSound(volume: number) {
  const context = new AudioContext()
  const oscillator = context.createOscillator()
  const gain = context.createGain()
  oscillator.frequency.value = 660
  gain.gain.value = volume * 0.4
  oscillator.connect(gain).connect(context.destination)
  oscillator.start()
  oscillator.stop(context.currentTime + 1.2)
  oscillator.onended = () => void context.close()
}

/**
 * The readiness check before the first component. Inspera shows a video; this page covers the same ground in text
 * (how to move around the test, mark questions, take notes, change the display, hide the screen) and lets the
 * candidate check the headphones and the volume. Confirm, then the forward arrow, back to the dashboard.
 */
export function ReadinessScreen({ profile, onDone }: { profile: CandidateProfile; onDone: () => void }) {
  const [volume, setVolume] = useState(0.8)
  const [isConfirmed, setIsConfirmed] = useState(false)
  const arrow = useRef<HTMLButtonElement>(null)

  return (
    <Frame title="Readiness check" profile={profile}>
      <ul className="readiness-list">
        <li>Each part of the test is a tab at the bottom of the screen. It shows how many questions you have answered, for example “Part 2 0 of 7”.</li>
        <li>Use the arrows at the bottom right, or click a question number, to move between questions. Click the flag next to a question to come back to it later.</li>
        <li>Select some text to highlight it or add a note. The pen at the top right shows your notes. Notes are private and are deleted at the end of the test.</li>
        <li>The menu (☰) at the top right changes the contrast and the text size and hides the time. It also hides your answers: open it before you leave your desk.</li>
        <li>The test stops by itself when the time runs out. You cannot submit the test early.</li>
        <li>In the Listening test the recording plays once, without stopping. Put your headphones on now and check the sound.</li>
      </ul>
      <div className="readiness-sound">
        <label>
          Volume
          <input type="range" min={0} max={1} step={0.05} value={volume} onChange={(e) => setVolume(Number(e.target.value))} aria-label="Volume" />
        </label>
        <button type="button" className="btn" onClick={() => playTestSound(volume)}>
          Play test sound
        </button>
      </div>
      <div className="readiness-actions">
        <button
          type="button"
          className="btn btn--primary"
          aria-pressed={isConfirmed}
          onClick={() => {
            setIsConfirmed(true)
            rememberReadiness(profile)
            requestAnimationFrame(() => arrow.current?.focus())
          }}
        >
          {isConfirmed ? 'Confirmed' : 'Confirm'}
        </button>
        <button ref={arrow} type="button" className="nav-arrow" aria-label="Continue" disabled={!isConfirmed} onClick={onDone}>
          <ArrowIcon direction="right" />
        </button>
      </div>
    </Frame>
  )
}

/** The instructions before a component: the candidate reads them, clicks "I confirm", then "Start (name of the test)". */
export function InstructionsScreen(props: { profile: CandidateProfile; section: CandidateSection; onStart: () => void; onBack: () => void }) {
  const { profile, section, onStart, onBack } = props
  const [isConfirmed, setIsConfirmed] = useState(false)
  const isListening = section.title.toLowerCase().includes('listening')
  const choosesInPart2 = section.title === 'Writing' && !profile.examName.toLowerCase().includes('key')

  return (
    <Frame title={section.title} profile={profile}>
      <p className="section-meta">{isListening ? `About ${section.durationMinutes} minutes` : `${section.durationMinutes} minutes`}</p>
      <ul className="readiness-list">
        {isListening ? (
          <>
            <li>You cannot leave the exam room at any time during the Listening test.</li>
            <li>When you click Start Listening, put your headphones on. The recording plays once and does not stop.</li>
            <li>When you finish, stay in your seat quietly and keep your headphones on until everyone has finished.</li>
          </>
        ) : (
          <>
            <li>You cannot leave the exam room in the first 30 minutes and the last 10 minutes.</li>
            <li>If you need a toilet break or finish early, hide your screen with the menu (☰), put your hand up and wait in your seat.</li>
          </>
        )}
        {choosesInPart2 && <li>In Part 2, answer one question and select “Yes” for the question you choose before the end of the test.</li>}
        <li>The test will stop automatically once the time has run out.</li>
        <li>Do not switch off or restart your computer. If you have a problem, put your hand up.</li>
      </ul>
      <div className="readiness-actions">
        <button type="button" className="btn" onClick={onBack}>
          Back
        </button>
        <button type="button" className={isConfirmed ? 'btn' : 'btn btn--primary'} aria-pressed={isConfirmed} onClick={() => setIsConfirmed(true)}>
          {isConfirmed ? 'Confirmed' : 'I confirm'}
        </button>
        <button type="button" className="btn btn--primary" disabled={!isConfirmed} onClick={onStart}>
          Start {section.title}
        </button>
      </div>
    </Frame>
  )
}
