import { useEffect, useState, type FormEvent } from 'react'
import { VictoryLogo } from '../ui/VictoryLogo'
import { staffApi, type StaffProfile } from './api'
import { ExamFormPage } from './pages/ExamFormPage'
import { ExamPage } from './pages/ExamPage'
import { ExamsPage } from './pages/ExamsPage'
import { LoginSlipsPage } from './pages/LoginSlipsPage'
import { MonitorPage } from './pages/MonitorPage'
import { PapersPage } from './pages/PapersPage'
import { ResultsPage } from './pages/ResultsPage'
import { StaffUsersPage } from './pages/StaffUsersPage'
import { Link } from './Link'
import { navigate, useRoute, type Route } from './router'
import { messageOf } from './format'
import './staff.css'

/**
 * Staff portal at /staff (Inspera Admin Portal): administrators prepare papers, sessions, candidates and staff;
 * invigilators open their sessions and monitor each section. The API enforces every permission; the menu only hides
 * what a role cannot use.
 */
export default function StaffApp() {
  const [user, setUser] = useState<StaffProfile | null | undefined>(undefined)

  useEffect(() => {
    staffApi.me().then(setUser, () => setUser(null))
  }, [])

  if (user === undefined) {
    return <div className="screen-message">Loading…</div>
  }

  if (user === null) {
    return <StaffLogin onSignedIn={setUser} />
  }

  const signOut = async () => {
    await staffApi.logout().catch(() => undefined)
    setUser(null)
    navigate('/staff')
  }

  return <Shell user={user} onSignOut={() => void signOut()} />
}

function Shell({ user, onSignOut }: { user: StaffProfile; onSignOut: () => void }) {
  const route = useRoute()
  const isAdmin = user.role === 'Admin'
  const section = route.page === 'papers' ? 'papers' : route.page === 'users' ? 'users' : 'exams'
  // Login slips print on their own, without the portal around them.
  if (route.page === 'slips') {
    return <LoginSlipsPage examId={route.examId} />
  }

  return (
    <div className="st">
      <header className="st-top">
        <VictoryLogo />
        <nav className="st-nav" aria-label="Staff portal">
          <Link to="/staff" className={section === 'exams' ? 'is-current' : undefined}>
            Exams
          </Link>
          {isAdmin && (
            <Link to="/staff/papers" className={section === 'papers' ? 'is-current' : undefined}>
              Papers
            </Link>
          )}
          {isAdmin && (
            <Link to="/staff/users" className={section === 'users' ? 'is-current' : undefined}>
              Staff
            </Link>
          )}
        </nav>
        <div className="st-user">
          <span>
            {user.fullName} <span className="st-faint">· {user.role === 'Admin' ? 'Administrator' : 'Invigilator'}</span>
          </span>
          <button type="button" className="st-link" onClick={onSignOut}>
            Log out
          </button>
        </div>
      </header>
      <main className="st-main">
        <Page route={route} isAdmin={isAdmin} currentUserId={user.id} />
      </main>
    </div>
  )
}

function Page({ route, isAdmin, currentUserId }: { route: Route; isAdmin: boolean; currentUserId: number }) {
  switch (route.page) {
    case 'exams':
      return <ExamsPage isAdmin={isAdmin} />
    case 'new-exam':
      return isAdmin ? <ExamFormPage /> : <NotAllowed />
    case 'edit-exam':
      return isAdmin ? <ExamFormPage examId={route.examId} /> : <NotAllowed />
    case 'exam':
      return <ExamPage examId={route.examId} isAdmin={isAdmin} />
    case 'monitor':
      return <MonitorPage examId={route.examId} sectionId={route.sectionId} />
    case 'results':
      return <ResultsPage examId={route.examId} />
    case 'papers':
      return isAdmin ? <PapersPage /> : <NotAllowed />
    case 'users':
      return isAdmin ? <StaffUsersPage currentUserId={currentUserId} /> : <NotAllowed />
    case 'slips':
    case 'not-found':
      return <NotAllowed />
  }
}

function NotAllowed() {
  return (
    <p className="st-empty">
      This page does not exist or is for administrators only. <Link to="/staff">Back to exams</Link>
    </p>
  )
}

function StaffLogin({ onSignedIn }: { onSignedIn: (user: StaffProfile) => void }) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isBusy, setIsBusy] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setIsBusy(true)
    setError(null)
    try {
      onSignedIn(await staffApi.login(username.trim(), password))
    } catch (e) {
      setError(messageOf(e))
    } finally {
      setIsBusy(false)
    }
  }

  return (
    <div className="st-login">
      <form className="st-card st-login-card" onSubmit={submit}>
        <VictoryLogo />
        <h1>Staff sign in</h1>
        <p className="st-faint">Administrators and invigilators</p>
        <label className="st-field">
          Username
          <input value={username} onChange={(e) => setUsername(e.target.value)} autoComplete="username" required />
        </label>
        <label className="st-field">
          Password
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" required />
        </label>
        {error && (
          <p className="st-error" role="alert">
            {error}
          </p>
        )}
        <button type="submit" className="st-btn st-btn--primary" disabled={isBusy}>
          Sign in
        </button>
      </form>
    </div>
  )
}
