import { useEffect, useState, type FormEvent } from 'react'
import { VictoryLogo } from '../ui/VictoryLogo'
import { accessOf, type Access } from './access'
import { SESSION_ENDED_EVENT, staffApi, type StaffProfile } from './api'
import { ApprovalsPage } from './pages/ApprovalsPage'
import { CentrePage } from './pages/CentrePage'
import { ExamFormPage } from './pages/ExamFormPage'
import { ExamPage } from './pages/ExamPage'
import { ExamsPage } from './pages/ExamsPage'
import { LoginSlipsPage } from './pages/LoginSlipsPage'
import { MarkingPage } from './pages/MarkingPage'
import { MonitorPage } from './pages/MonitorPage'
import { PaperPage } from './pages/PaperPage'
import { PapersPage } from './pages/PapersPage'
import { RoomPlanPage } from './pages/RoomPlanPage'
import { ResultsPage } from './pages/ResultsPage'
import { StaffUsersPage } from './pages/StaffUsersPage'
import { Link } from './Link'
import { navigate, useRoute, type Route } from './router'
import { messageOf } from './format'
import { Badge } from './ui'
import './staff.css'

/**
 * Staff portal at /staff (Inspera Admin Portal): administrators and managers prepare papers, sessions and candidates
 * (deletions and account changes wait for a second administrator); invigilators monitor each section; examiners
 * review finished work and mark Writing. The API enforces every permission; the menu only hides what a role cannot use.
 */
export default function StaffApp() {
  const [user, setUser] = useState<StaffProfile | null | undefined>(undefined)
  const [notice, setNotice] = useState<string | null>(null)

  useEffect(() => {
    document.title = 'Victory English – Staff portal'
    staffApi.me().then(setUser, () => setUser(null))
  }, [])

  // Any call answered 401 means the session is over: back to the sign-in, keeping the page address.
  useEffect(() => {
    const ended = () => {
      setNotice('Your session has ended. Please sign in again.')
      setUser(null)
    }
    window.addEventListener(SESSION_ENDED_EVENT, ended)
    return () => window.removeEventListener(SESSION_ENDED_EVENT, ended)
  }, [])

  if (user === undefined) {
    return <div className="screen-message">Loading…</div>
  }

  if (user === null) {
    return (
      <StaffLogin
        notice={notice}
        onSignedIn={(signedIn) => {
          setNotice(null)
          setUser(signedIn)
        }}
      />
    )
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
  const access = accessOf(user)
  const section = route.page === 'papers' || route.page === 'paper' ? 'papers' : route.page === 'users' || route.page === 'approvals' || route.page === 'centre' ? route.page : 'exams'
  // Login slips print on their own, without the portal around them.
  if (route.page === 'slips') {
    return <LoginSlipsPage examId={route.examId} />
  }

  if (route.page === 'room-plan') {
    return <RoomPlanPage examId={route.examId} />
  }

  return (
    <div className="st">
      <header className="st-top">
        <VictoryLogo />
        <nav className="st-nav" aria-label="Staff portal">
          {access.seesSessions && (
            <Link to="/staff" className={section === 'exams' ? 'is-current' : undefined}>
              Exams
            </Link>
          )}
          {access.worksIn('Papers') && (
            <Link to="/staff/papers" className={section === 'papers' ? 'is-current' : undefined}>
              Papers
            </Link>
          )}
          {access.worksIn('Centre') && (
            <Link to="/staff/centre" className={section === 'centre' ? 'is-current' : undefined}>
              Centre
            </Link>
          )}
          {access.isAdmin && (
            <Link to="/staff/users" className={section === 'users' ? 'is-current' : undefined}>
              Staff
            </Link>
          )}
          {access.requestsApprovals && (
            <Link to="/staff/approvals" className={section === 'approvals' ? 'is-current' : undefined}>
              Approvals
            </Link>
          )}
        </nav>
        <div className="st-user">
          <span className="st-user-name">
            {user.fullName} <Badge value={user.role} />
          </span>
          <button type="button" className="st-link" onClick={onSignOut}>
            Log out
          </button>
        </div>
      </header>
      <main className="st-main">
        <Page route={route} access={access} />
      </main>
    </div>
  )
}

function Page({ route, access }: { route: Route; access: Access }) {
  switch (route.page) {
    case 'exams':
      if (access.seesSessions) {
        return <ExamsPage access={access} />
      }

      // A manager without sessions starts on their own area.
      return access.worksIn('Papers') ? <PapersPage /> : access.worksIn('Centre') ? <CentrePage /> : <NotAllowed />
    case 'new-exam':
      return access.worksIn('Exams') ? <ExamFormPage /> : <NotAllowed />
    case 'edit-exam':
      return access.worksIn('Exams') ? <ExamFormPage examId={route.examId} /> : <NotAllowed />
    case 'exam':
      return access.seesSessions ? <ExamPage examId={route.examId} access={access} /> : <NotAllowed />
    case 'monitor':
      return access.monitors ? <MonitorPage examId={route.examId} sectionId={route.sectionId} /> : <NotAllowed />
    case 'results':
      return access.seesResults ? <ResultsPage examId={route.examId} canMark={access.marks} /> : <NotAllowed />
    case 'marking':
      return access.marks ? <MarkingPage examId={route.examId} attemptId={route.attemptId} /> : <NotAllowed />
    case 'papers':
      return access.worksIn('Papers') ? <PapersPage /> : <NotAllowed />
    case 'paper':
      return access.worksIn('Papers') ? <PaperPage paperId={route.paperId} /> : <NotAllowed />
    case 'users':
      return access.isAdmin ? <StaffUsersPage currentUserId={access.userId} /> : <NotAllowed />
    case 'approvals':
      return access.requestsApprovals ? <ApprovalsPage access={access} /> : <NotAllowed />
    case 'centre':
      return access.worksIn('Centre') ? <CentrePage /> : <NotAllowed />
    case 'slips':
    case 'room-plan':
    case 'not-found':
      return <NotAllowed />
  }
}

function NotAllowed() {
  return (
    <p className="st-empty">
      This page does not exist or is not part of your role. <Link to="/staff">Back to the start</Link>
    </p>
  )
}

function StaffLogin({ notice, onSignedIn }: { notice: string | null; onSignedIn: (user: StaffProfile) => void }) {
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
        <p className="st-faint">Staff of the centre</p>
        {notice && (
          <p className="st-note" role="status">
            {notice}
          </p>
        )}
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
