import { useCallback, useEffect, useRef, useState } from 'react'
import { candidateApi } from '../api/client'
import { useLive } from '../api/live'
import type { Answers, AttemptState, CandidateMessage, Selection, Selections } from '../api/types'
import { AutosaveQueue, type SaveStatus } from './autosave'
import { remainingSecondsNow } from './timing'

/**
 * Heartbeat: tells the server the candidate is online and re-reads the clock, so an invigilator's pause, extra
 * time, end time, rewind or message reaches this screen within seconds.
 */
const SYNC_INTERVAL_MS = 5_000
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
  /** VE-35: "Answering this question?" choices other than Undecided. */
  selections: Selections
  /** Saves a choice at once; resolves to the server's refusal (shown to the candidate) or null. */
  setSelection: (number: number, selection: Selection) => Promise<string | null>
  /** Listening: where the recording should be now (server figure plus time elapsed here); null before Play. */
  audioPositionNow: () => number | null
  /** Listening: tells the server the candidate pressed Play; resolves to the recording position. */
  playAudio: () => Promise<number>
  /** From the invigilator, oldest first. */
  messages: CandidateMessage[]
  /** When the server's figures were last received (changes on every heartbeat). */
  syncedAt: number | null
}

type Clock = { status: AttemptState['status']; remaining: number; audioPositionMs: number | null; receivedAt: number }

/**
 * The candidate's attempt at one section: starts (or resumes) it on the server, counts down from the server's
 * figure, saves every answer, and follows the server when the section is paused or ends.
 */
export function useAttempt(sectionId: number): AttemptView {
  const [clock, setClock] = useState<Clock | null>(null)
  const [messages, setMessages] = useState<CandidateMessage[]>([])
  const [answers, setAnswers] = useState<Answers>({})
  const [selections, setSelections] = useState<Selections>({})
  const selectionsRef = useRef<Selections>({})
  const [error, setError] = useState<string | null>(null)
  const [saveStatus, setSaveStatus] = useState<SaveStatus>('saved')
  const [now, setNow] = useState(() => Date.now())
  const queueRef = useRef<AutosaveQueue | null>(null)

  const apply = useCallback((state: AttemptState) => {
    setClock({ status: state.status, remaining: state.remainingSeconds, audioPositionMs: state.audioPositionMs, receivedAt: Date.now() })
    setMessages((current) => (sameMessages(current, state.messages ?? []) ? current : (state.messages ?? [])))
  }, [])

  const sync = useCallback(() => {
    candidateApi.heartbeat(sectionId).then(apply, () => setSaveStatus('offline'))
  }, [sectionId, apply])

  // VE-13: the invigilator's pause, time change, rewind or message arrives now instead of at the next heartbeat.
  useLive('/hubs/candidate', { refresh: sync })

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
        selectionsRef.current = state.selections ?? {}
        setSelections(selectionsRef.current)
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

  const setSelection = useCallback(
    async (number: number, selection: Selection) => {
      const before = selectionsRef.current
      const show = (next: Selections) => {
        selectionsRef.current = next
        setSelections(next)
      }

      show({ ...before, [number]: selection })
      try {
        await candidateApi.saveSelection(sectionId, number, selection)
        return null
      } catch (e) {
        show(before)
        return e instanceof Error ? e.message : 'Your choice was not saved.'
      }
    },
    [sectionId],
  )

  const audioPositionNow = useCallback(() => {
    if (!clock || clock.audioPositionMs === null) {
      return null
    }

    return clock.status === 'InProgress' ? clock.audioPositionMs + (Date.now() - clock.receivedAt) : clock.audioPositionMs
  }, [clock])

  const playAudio = useCallback(async () => {
    const state = await candidateApi.playAudio(sectionId)
    apply(state)
    return state.audioPositionMs ?? 0
  }, [sectionId, apply])

  return {
    isLoaded: clock !== null,
    error,
    status: clock?.status ?? null,
    remainingSeconds,
    answers,
    saveStatus,
    setAnswer,
    selections,
    setSelection,
    audioPositionNow,
    playAudio,
    messages,
    syncedAt: clock?.receivedAt ?? null,
  }
}

const sameMessages = (a: CandidateMessage[], b: CandidateMessage[]) => a.length === b.length && a.every((m, i) => m.id === b[i].id)
