import type { CandidateProfile } from '../api/types'
import { BellIcon, MenuIcon, NotesIcon, WifiIcon } from '../ui/icons'
import { VictoryLogo } from '../ui/VictoryLogo'
import type { SaveStatus } from './autosave'

type Props = {
  profile: CandidateProfile
  /** Countdown text; null once the section has ended. */
  timeLeft: string | null
  saveStatus: SaveStatus
}

/**
 * Inspera header: logo, candidate name and number, the countdown, then connection, messages, options and
 * notes. The connection icon turns red while answers cannot reach the server (they are kept and retried).
 */
export function TopBar({ profile, timeLeft, saveStatus }: Props) {
  const isOffline = saveStatus === 'offline'

  return (
    <header className="top-bar">
      <VictoryLogo />
      <div className="candidate">
        <div className="candidate-name">{profile.fullName}</div>
        <div className="candidate-number">{profile.candidateNumber}</div>
      </div>
      {timeLeft && (
        <div className="time-left" role="timer" aria-live="off">
          {timeLeft}
        </div>
      )}
      <div className="top-icons">
        <span
          className={isOffline ? 'top-icon is-offline' : 'top-icon'}
          role="img"
          aria-label={isOffline ? 'Not connected. Your answers will be saved when the connection returns.' : 'Connected'}
          title={isOffline ? 'Not connected. Your answers will be saved when the connection returns.' : 'Connected'}
        >
          <WifiIcon />
        </span>
        <button type="button" className="top-icon" aria-label="Messages">
          <BellIcon />
        </button>
        <button type="button" className="top-icon" aria-label="Options">
          <MenuIcon />
        </button>
        <button type="button" className="top-icon" aria-label="Show notes">
          <NotesIcon />
        </button>
      </div>
    </header>
  )
}
