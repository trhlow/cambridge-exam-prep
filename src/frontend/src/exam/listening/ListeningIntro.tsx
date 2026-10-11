export interface ListeningIntroProps {
  onPlay: () => void
  /** The recording already started (the page was reloaded): it continues from where it is now. */
  isResuming: boolean
}

/** The dark overlay Inspera shows before a Listening test, with the headphones and the Play button. */
export function ListeningIntro({ onPlay, isResuming }: ListeningIntroProps) {
  return (
    <div className="listening-intro" role="dialog" aria-modal="true" aria-label="Listening">
      <HeadphonesIcon />
      {isResuming ? (
        <p>The recording is still playing. Click the button to hear it again from where it is now.</p>
      ) : (
        <>
          <p>You will be listening to an audio clip during this test. You will not be permitted to pause or rewind the audio while answering the questions.</p>
          <p>To continue, click Play.</p>
        </>
      )}
      <button type="button" className="btn btn--primary listening-play" onClick={onPlay}>
        <PlayIcon />
        {isResuming ? 'Continue listening' : 'Play'}
      </button>
    </div>
  )
}

function HeadphonesIcon() {
  return (
    <svg viewBox="0 0 64 64" width="72" height="72" aria-hidden="true">
      <path d="M10 38V32a22 22 0 0 1 44 0v6" fill="none" stroke="#fff" strokeWidth="5" strokeLinecap="round" />
      <rect x="6" y="36" width="13" height="20" rx="5" fill="#fff" />
      <rect x="45" y="36" width="13" height="20" rx="5" fill="#fff" />
    </svg>
  )
}

function PlayIcon() {
  return (
    <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
      <circle cx="12" cy="12" r="11" fill="#fff" />
      <path d="M10 7.5v9l7-4.5z" fill="currentColor" />
    </svg>
  )
}
