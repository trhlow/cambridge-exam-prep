import { useState, type ReactNode } from 'react'
import type { CandidateProfile } from '../api/types'
import { CandidateIcon } from '../ui/CandidateIcon'
import { BellIcon, MenuIcon, NotesIcon, VolumeIcon, WifiIcon } from '../ui/icons'
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
  /** Opens or closes the Notes panel (pen); off with large text, as in the Cambridge booklet. */
  onToggleNotes?: () => void
  isNotesDisabled?: boolean
  /** Listening: the volume control of the headphones (0–1). */
  volume?: { value: number; onChange: (value: number) => void }
}

/**
 * Inspera header: logo, candidate name and number, the countdown, then connection, messages, options and
 * notes. The connection icon turns red while answers cannot reach the server (they are kept and retried).
 */
export function TopBar({ profile, timeLeft, saveStatus, isAudioPlaying = false, bell, onOpenOptions, onToggleNotes, isNotesDisabled = false, volume }: Props) {
  const isOffline = saveStatus === 'offline'
  const [isVolumeOpen, setIsVolumeOpen] = useState(false)

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
        {volume && (
          <div className="volume">
            <button type="button" className="top-icon" aria-label="Volume" aria-expanded={isVolumeOpen} onClick={() => setIsVolumeOpen(!isVolumeOpen)}>
              <VolumeIcon />
            </button>
            {isVolumeOpen && (
              <div className="volume-panel">
                <input type="range" min={0} max={1} step={0.05} value={volume.value} onChange={(e) => volume.onChange(Number(e.target.value))} aria-label="Volume level" />
              </div>
            )}
          </div>
        )}
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
        <button
          type="button"
          className="top-icon"
          aria-label="Show notes"
          title={isNotesDisabled ? 'Notes are not available with large text sizes' : 'Notes'}
          onClick={onToggleNotes}
          disabled={!onToggleNotes || isNotesDisabled}
        >
          <NotesIcon />
        </button>
        <CandidateIcon fullName={profile.fullName} candidateNumber={profile.candidateNumber} />
      </div>
    </header>
  )
}
