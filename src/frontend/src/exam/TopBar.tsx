import type { CandidateProfile } from '../api/types'
import { BellIcon, MenuIcon, NotesIcon, WifiIcon } from '../ui/icons'
import { VictoryLogo } from '../ui/VictoryLogo'

type Props = {
  profile: CandidateProfile
}

/** Inspera header: logo, candidate name and number, then connection, messages, options and notes. */
export function TopBar({ profile }: Props) {
  return (
    <header className="top-bar">
      <VictoryLogo />
      <div className="candidate">
        <div className="candidate-name">{profile.fullName}</div>
        <div className="candidate-number">{profile.candidateNumber}</div>
      </div>
      <div className="top-icons">
        <span className="top-icon" role="img" aria-label="Connected">
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
