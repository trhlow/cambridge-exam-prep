import { useEffect, useMemo, useState } from 'react'
import { candidateApi } from '../api/client'
import type { CandidateProfile, CandidateSection, Paper } from '../api/types'
import { ArrowIcon } from '../ui/icons'
import { BottomBar } from './BottomBar'
import { ListeningIntro } from './listening/ListeningIntro'
import { MessageCentre } from './MessageCentre'
import { OptionsScreen, type DisplayOptions } from './OptionsScreen'
import { useRecordingPlayer } from './listening/useRecordingPlayer'
import { buildPages } from './paperLogic'
import { PartView } from './PartView'
import { ReviewPage } from './ReviewPage'
import { TopBar } from './TopBar'
import { useAttempt } from './useAttempt'
import { timeLeftText } from './timing'

/** Further apart than this, the recording follows the server: the invigilator has rewound it. */
const AUDIO_DRIFT_MS = 4_000

type Props = {
  profile: CandidateProfile
  section: CandidateSection
  /** The server saved new Options (text size, contrast). */
  onProfileChange?: (profile: CandidateProfile) => void
  /** The section has ended; go back to the list, where the next section is now open. */
  onFinished: () => void
}

const HIDE_TIMER_KEY = 'victory.hideTimer'

/** "Hide remaining time" lasts for this tab, like the other per-candidate screen choices. */
function useHiddenTimer(): [boolean, () => void] {
  const [isHidden, setIsHidden] = useState(() => {
    try {
      return sessionStorage.getItem(HIDE_TIMER_KEY) === '1'
    } catch {
      return false
    }
  })
  const toggle = () =>
    setIsHidden((current) => {
      try {
        sessionStorage.setItem(HIDE_TIMER_KEY, current ? '0' : '1')
      } catch {
        // Storage blocked: the choice simply does not survive a reload.
      }
      return !current
    })
  return [isHidden, toggle]
}

/** Flags are a reminder for the candidate only; they survive a reload of this tab. */
function useTabFlags(sectionId: number): [ReadonlySet<number>, (number: number) => void] {
  const key = `victory.flags.${sectionId}`
  const [list, setList] = useState<number[]>(() => {
    try {
      return JSON.parse(sessionStorage.getItem(key) ?? '[]') as number[]
    } catch {
      return []
    }
  })

  useEffect(() => {
    sessionStorage.setItem(key, JSON.stringify(list))
  }, [key, list])

  const flagged = useMemo<ReadonlySet<number>>(() => new Set(list), [list])
  const toggle = (number: number) => setList((current) => (current.includes(number) ? current.filter((n) => n !== number) : [...current, number]))
  return [flagged, toggle]
}

export function ExamScreen({ profile, section, onFinished, onProfileChange }: Props) {
  const [paper, setPaper] = useState<Paper | null>(null)
  const [paperError, setPaperError] = useState<string | null>(null)
  const [pageIndex, setPageIndex] = useState(0)
  const [isReviewing, setIsReviewing] = useState(false)
  const [activeNumber, setActiveNumber] = useState<number | undefined>(undefined)
  const [flagged, toggleFlag] = useTabFlags(section.id)
  const [isOptionsOpen, setIsOptionsOpen] = useState(false)
  const [display, setDisplay] = useState<DisplayOptions>({ textSize: profile.textSize ?? 'Regular', contrast: profile.contrast ?? 'BlackOnWhite' })
  const [isTimerHidden, toggleTimer] = useHiddenTimer()
  const attempt = useAttempt(section.id)
  const pages = useMemo(() => (paper ? buildPages(paper) : []), [paper])
  const audioUrls = useMemo(() => paper?.parts.map((p) => p.audioUrl).filter((url): url is string => url !== null) ?? [], [paper])
  const player = useRecordingPlayer(audioUrls)
  const { stop: stopRecording, start: startRecording, positionMs: recordingPosition, state: playerState } = player
  const { syncedAt, audioPositionNow } = attempt
  const [audioNotice, setAudioNotice] = useState<string | null>(null)

  // An invigilator pause stops the recording; the server moves its position back by the paused time.
  useEffect(() => {
    if (attempt.status !== 'InProgress') {
      stopRecording()
    }
  }, [attempt.status, stopRecording])

  // VE-16: after an invigilator rewind the server's position jumps back; the recording follows it.
  useEffect(() => {
    if (playerState !== 'playing' || syncedAt === null) {
      return
    }

    const server = audioPositionNow()
    const local = recordingPosition()
    if (server === null || local === null || Math.abs(server - local) < AUDIO_DRIFT_MS) {
      return
    }

    const wasRewound = server < local
    void startRecording(server).then(() => wasRewound && setAudioNotice('The invigilator has moved the recording back.'))
  }, [syncedAt, playerState, audioPositionNow, recordingPosition, startRecording])

  useEffect(() => {
    if (!audioNotice) {
      return
    }

    const timer = setTimeout(() => setAudioNotice(null), 8_000)
    return () => clearTimeout(timer)
  }, [audioNotice])

  useEffect(() => {
    candidateApi.paper(section.id).then(setPaper, (e: Error) => setPaperError(e.message))
  }, [section.id])

  const changeDisplay = (next: DisplayOptions) => {
    setDisplay(next)
    candidateApi.savePreferences(next.textSize, next.contrast).then((saved) => onProfileChange?.(saved), () => undefined)
  }

  const error = paperError ?? attempt.error
  if (error) {
    return <div className="screen-message">The test could not be loaded: {error}</div>
  }

  if (!paper || pages.length === 0 || !attempt.isLoaded) {
    return <div className="screen-message">Loading the test…</div>
  }

  if (attempt.status === 'Submitted') {
    return <FinishedScreen profile={profile} title={paper.title} display={display} onContinue={onFinished} />
  }

  const page = pages[pageIndex]
  const goTo = (index: number) => {
    setIsReviewing(false)
    setPageIndex(index)
    setActiveNumber(undefined)
  }
  const focusQuestion = (number: number) => {
    setActiveNumber(number)
    // Wait for the page to render, then bring the question into view as Inspera does.
    requestAnimationFrame(() => document.querySelector(`[data-question="${number}"]`)?.scrollIntoView({ block: 'center', behavior: 'smooth' }))
  }
  const answer = (number: number, value: string) => {
    setActiveNumber(number)
    attempt.setAnswer(number, value)
  }
  const isListening = audioUrls.length > 0
  const needsPlay = isListening && attempt.status === 'InProgress' && player.state === 'idle'
  const isResumingAudio = attempt.audioPositionNow() !== null
  const play = async () => {
    const position = isResumingAudio ? (attempt.audioPositionNow() ?? 0) : await attempt.playAudio()
    await player.start(position)
  }

  return (
    <div className="exam" data-contrast={display.contrast} data-text-size={display.textSize}>
      <TopBar
        profile={profile}
        timeLeft={isTimerHidden ? null : timeLeftText(attempt.remainingSeconds)}
        onOpenOptions={() => setIsOptionsOpen(true)}
        saveStatus={attempt.saveStatus}
        isAudioPlaying={player.state === 'playing'}
        bell={<MessageCentre sectionId={section.id} messages={attempt.messages} />}
      />
      {audioNotice && (
        <div className="audio-notice" role="status">
          {audioNotice}
        </div>
      )}
      <main className="exam-main">
        {isReviewing ? (
          <ReviewPage paper={paper} answers={attempt.answers} onBack={() => setIsReviewing(false)} />
        ) : (
          <PartView
            key={pageIndex}
            part={paper.parts[page.partIndex]}
            questionNumbers={page.questionNumbers}
            answers={attempt.answers}
            onAnswer={answer}
            flagged={flagged}
            onToggleFlag={toggleFlag}
          />
        )}
        {!isReviewing && (
          <div className="nav-arrows">
            <button type="button" className="nav-arrow nav-arrow--prev" aria-label="Previous" disabled={pageIndex === 0} onClick={() => goTo(pageIndex - 1)}>
              <ArrowIcon direction="left" />
            </button>
            <button
              type="button"
              className="nav-arrow nav-arrow--next"
              aria-label="Next"
              onClick={() => (pageIndex === pages.length - 1 ? setIsReviewing(true) : goTo(pageIndex + 1))}
            >
              <ArrowIcon direction="right" />
            </button>
          </div>
        )}
        {needsPlay && <ListeningIntro isResuming={isResumingAudio} onPlay={() => void play()} />}
        {isOptionsOpen && (
          <OptionsScreen
            {...display}
            isTimerHidden={isTimerHidden}
            onChange={changeDisplay}
            onToggleTimer={toggleTimer}
            onSubmissionPage={() => {
              setIsOptionsOpen(false)
              setIsReviewing(true)
            }}
            onClose={() => setIsOptionsOpen(false)}
          />
        )}
        {attempt.status === 'Paused' && (
          <div className="paused" role="alertdialog" aria-label="Test paused">
            <p>Your test has been paused by the invigilator.</p>
            <p>Please wait. Your time will not run while the test is paused.</p>
          </div>
        )}
      </main>
      <BottomBar
        paper={paper}
        pages={pages}
        pageIndex={pageIndex}
        activeNumber={activeNumber}
        answers={attempt.answers}
        onGoTo={goTo}
        onFocusQuestion={focusQuestion}
        onReview={() => setIsReviewing(true)}
        flagged={flagged}
      />
    </div>
  )
}

function FinishedScreen({ profile, title, display, onContinue }: { profile: CandidateProfile; title: string; display: DisplayOptions; onContinue: () => void }) {
  return (
    <div className="exam exam--finished" data-contrast={display.contrast} data-text-size={display.textSize}>
      <TopBar profile={profile} timeLeft={null} saveStatus="saved" />
      <main className="finished">
        <h2>{title}</h2>
        <p>The test has now finished. Your answers have been saved.</p>
        <button type="button" className="btn btn--primary" onClick={onContinue}>
          Continue
        </button>
      </main>
    </div>
  )
}
