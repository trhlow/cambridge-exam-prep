import { useEffect, useMemo, useState } from 'react'
import { candidateApi } from '../api/client'
import type { Answers, CandidateProfile, CandidateSection, Paper } from '../api/types'
import { ArrowIcon } from '../ui/icons'
import { BottomBar } from './BottomBar'
import { buildPages } from './paperLogic'
import { PartView } from './PartView'
import { ReviewPage } from './ReviewPage'
import { TopBar } from './TopBar'

type Props = {
  profile: CandidateProfile
  section: CandidateSection
}

/** Until autosave (VE-8) keeps answers on the server, they survive a page reload in this tab only. */
function useTabAnswers(sectionId: number): [Answers, (number: number, value: string) => void] {
  const key = `victory.answers.${sectionId}`
  const [answers, setAnswers] = useState<Answers>(() => {
    try {
      return JSON.parse(sessionStorage.getItem(key) ?? '{}') as Answers
    } catch {
      return {}
    }
  })

  useEffect(() => {
    sessionStorage.setItem(key, JSON.stringify(answers))
  }, [key, answers])

  return [answers, (number, value) => setAnswers((current) => ({ ...current, [number]: value }))]
}

export function ExamScreen({ profile, section }: Props) {
  const [paper, setPaper] = useState<Paper | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [pageIndex, setPageIndex] = useState(0)
  const [isReviewing, setIsReviewing] = useState(false)
  const [flagged, setFlagged] = useState<ReadonlySet<number>>(new Set())
  const [activeNumber, setActiveNumber] = useState<number | undefined>(undefined)
  const [answers, setAnswer] = useTabAnswers(section.id)
  const pages = useMemo(() => (paper ? buildPages(paper) : []), [paper])

  useEffect(() => {
    candidateApi.paper(section.id).then(setPaper, (e: Error) => setError(e.message))
  }, [section.id])

  if (error) {
    return <div className="screen-message">The test could not be loaded: {error}</div>
  }

  if (!paper || pages.length === 0) {
    return <div className="screen-message">Loading the test…</div>
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
    setAnswer(number, value)
  }
  const toggleFlag = (number: number) =>
    setFlagged((current) => {
      const next = new Set(current)
      if (!next.delete(number)) next.add(number)
      return next
    })

  return (
    <div className="exam">
      <TopBar profile={profile} />
      <main className="exam-main">
        {isReviewing ? (
          <ReviewPage paper={paper} answers={answers} onBack={() => setIsReviewing(false)} />
        ) : (
          <PartView
            key={pageIndex}
            part={paper.parts[page.partIndex]}
            questionNumbers={page.questionNumbers}
            answers={answers}
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
      </main>
      <BottomBar
        paper={paper}
        pages={pages}
        pageIndex={pageIndex}
        activeNumber={activeNumber}
        answers={answers}
        onGoTo={goTo}
        onFocusQuestion={focusQuestion}
        onReview={() => setIsReviewing(true)}
        flagged={flagged}
      />
    </div>
  )
}
