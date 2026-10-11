import { useEffect, useMemo, useState } from 'react'
import { candidateApi } from '../api/client'
import type { CandidateProfile, CandidateSection, Paper } from '../api/types'
import { ArrowIcon } from '../ui/icons'
import { BottomBar } from './BottomBar'
import { buildPages } from './paperLogic'
import { PartView } from './PartView'
import { ReviewPage } from './ReviewPage'
import { TopBar } from './TopBar'
import { useAttempt } from './useAttempt'
import { timeLeftText } from './timing'

type Props = {
  profile: CandidateProfile
  section: CandidateSection
  /** The section has ended; go back to the list, where the next section is now open. */
  onFinished: () => void
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

export function ExamScreen({ profile, section, onFinished }: Props) {
  const [paper, setPaper] = useState<Paper | null>(null)
  const [paperError, setPaperError] = useState<string | null>(null)
  const [pageIndex, setPageIndex] = useState(0)
  const [isReviewing, setIsReviewing] = useState(false)
  const [activeNumber, setActiveNumber] = useState<number | undefined>(undefined)
  const [flagged, toggleFlag] = useTabFlags(section.id)
  const attempt = useAttempt(section.id)
  const pages = useMemo(() => (paper ? buildPages(paper) : []), [paper])

  useEffect(() => {
    candidateApi.paper(section.id).then(setPaper, (e: Error) => setPaperError(e.message))
  }, [section.id])

  const error = paperError ?? attempt.error
  if (error) {
    return <div className="screen-message">The test could not be loaded: {error}</div>
  }

  if (!paper || pages.length === 0 || !attempt.isLoaded) {
    return <div className="screen-message">Loading the test…</div>
  }

  if (attempt.status === 'Submitted') {
    return <FinishedScreen profile={profile} title={paper.title} onContinue={onFinished} />
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

  return (
    <div className="exam">
      <TopBar profile={profile} timeLeft={timeLeftText(attempt.remainingSeconds)} saveStatus={attempt.saveStatus} />
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

function FinishedScreen({ profile, title, onContinue }: { profile: CandidateProfile; title: string; onContinue: () => void }) {
  return (
    <div className="exam exam--finished">
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
