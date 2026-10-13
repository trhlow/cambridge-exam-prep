import type { ReactNode } from 'react'
import type { CandidateProfile } from '../api/types'
import { BellIcon, MenuIcon, NotesIcon, WifiIcon } from '../ui/icons'
import { VictoryLogo } from '../ui/VictoryLogo'
import type { SaveStatus } from './autosave'

type Props = {
  profile: CandidateProfile
  /** Countdown text; null once the section has ended. */
  timeLeft: string | null
  saveStatus: SaveStatus
  /** Listening: shown under the candidate's name while the recording plays, as in Inspera. */
  isAudioPlaying?: boolean
  /** The invigilator's messages (bell icon); a plain bell when the section has none to show. */
  bell?: ReactNode
  /** Opens the Options screen (☰). */
  onOpenOptions?: () => void
}

/**
 * Inspera header: logo, candidate name and number, the countdown, then connection, messages, options and
 * notes. The connection icon turns red while answers cannot reach the server (they are kept and retried).
 */
export function TopBar({ profile, timeLeft, saveStatus, isAudioPlaying = false, bell, onOpenOptions }: Props) {
  const isOffline = saveStatus === 'offline'

  return (
    <header className="top-bar">
      <VictoryLogo />
      <div className="candidate">
        <div className="candidate-name">{profile.fullName}</div>
        <div className="candidate-number">{profile.candidateNumber}</div>
        {isAudioPlaying && (
          <div className="audio-playing" role="status">
            <span aria-hidden="true">🔊</span> Audio is Playing
          </div>
        )}
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
        {bell ?? (
          <button type="button" className="top-icon" aria-label="Messages">
            <BellIcon />
          </button>
        )}
        <button type="button" className="top-icon" aria-label="Options" onClick={onOpenOptions} disabled={!onOpenOptions}>
          <MenuIcon />
        </button>
        <button type="button" className="top-icon" aria-label="Show notes">
          <NotesIcon />
        </button>
      </div>
    </header>
  )
}
