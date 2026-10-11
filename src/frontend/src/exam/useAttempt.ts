import { useCallback, useEffect, useRef, useState } from 'react'
import { candidateApi } from '../api/client'
import type { Answers, AttemptState } from '../api/types'
import { AutosaveQueue, type SaveStatus } from './autosave'
import { remainingSecondsNow } from './timing'

/** Re-read the server clock this often (the invigilator may pause, add or end time). */
const SYNC_INTERVAL_MS = 30_000
/** After the countdown reaches zero, give the last save a moment before asking the server to end. */
const TIME_UP_CHECK_DELAY_MS = 1_500

export interface AttemptView {
  isLoaded: boolean
  error: string | null
  status: AttemptState['status'] | null
  remainingSeconds: number
  answers: Answers
  saveStatus: SaveStatus
  setAnswer: (number: number, value: string) => void
}

type Clock = { status: AttemptState['status']; remaining: number; receivedAt: number }

/**
 * The candidate's attempt at one section: starts (or resumes) it on the server, counts down from the server's
 * figure, saves every answer, and follows the server when the section is paused or ends.
 */
export function useAttempt(sectionId: number): AttemptView {
  const [clock, setClock] = useState<Clock | null>(null)
  const [answers, setAnswers] = useState<Answers>({})
  const [error, setError] = useState<string | null>(null)
  const [saveStatus, setSaveStatus] = useState<SaveStatus>('saved')
  const [now, setNow] = useState(() => Date.now())
  const queueRef = useRef<AutosaveQueue | null>(null)

  const apply = useCallback((state: AttemptState) => {
    setClock({ status: state.status, remaining: state.remainingSeconds, receivedAt: Date.now() })
  }, [])

  const sync = useCallback(() => {
    candidateApi.attempt(sectionId).then(apply, () => setSaveStatus('offline'))
  }, [sectionId, apply])

  useEffect(() => {
    const queue = new AutosaveQueue((items) => candidateApi.saveResponses(sectionId, items), {
      delayMs: 800,
      retryMs: 3_000,
      onStatus: setSaveStatus,
      onClosed: sync,
    })
    queueRef.current = queue

    candidateApi.startAttempt(sectionId).then(
      (state) => {
        apply(state)
        setAnswers(state.answers)
      },
      (e: Error) => setError(e.message),
    )

    const tick = setInterval(() => setNow(Date.now()), 1_000)
    const poll = setInterval(sync, SYNC_INTERVAL_MS)
    return () => {
      queue.dispose()
      clearInterval(tick)
      clearInterval(poll)
    }
  }, [sectionId, apply, sync])

  const remainingSeconds = clock ? (clock.status === 'InProgress' ? remainingSecondsNow(clock.remaining, clock.receivedAt, now) : clock.remaining) : 0
  const isTimeUp = clock?.status === 'InProgress' && remainingSeconds === 0

  useEffect(() => {
    if (!isTimeUp) {
      return
    }

    const check = setTimeout(sync, TIME_UP_CHECK_DELAY_MS)
    return () => clearTimeout(check)
  }, [isTimeUp, sync])

  const setAnswer = useCallback((number: number, value: string) => {
    setAnswers((current) => ({ ...current, [number]: value }))
    queueRef.current?.push(number, value)
  }, [])

  return { isLoaded: clock !== null, error, status: clock?.status ?? null, remainingSeconds, answers, saveStatus, setAnswer }
}
