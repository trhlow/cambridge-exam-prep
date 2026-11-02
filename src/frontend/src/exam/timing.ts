/**
 * The countdown shown in the header: whole minutes, rounded up so the clock never reads "0 minutes" while time
 * remains, then minutes and seconds from the last 5 minutes, as the Cambridge exam day booklet describes ("When there
 * are 5 minutes remaining, the on-screen time starts showing minutes and seconds", 2026, p. 16).
 */
export function timeLeftText(seconds: number): string {
  if (seconds <= 0) {
    return 'Time is up'
  }

  if (seconds <= 5 * 60) {
    const minutes = Math.floor(seconds / 60)
    return `${minutes}:${String(seconds % 60).padStart(2, '0')} left`
  }

  const minutes = Math.ceil(seconds / 60)
  return `${minutes} minutes left`
}

/**
 * Seconds left now, from the server's figure and the time elapsed on this computer since the reply arrived.
 * The computer's own clock may be wrong; only the elapsed time is trusted. `nowMs` comes from a once-a-second tick, so
 * right after a reply it can still be older than the reply: that never counts as time gained.
 */
export function remainingSecondsNow(remainingAtReply: number, replyReceivedAtMs: number, nowMs: number): number {
  const elapsedMs = Math.max(0, nowMs - replyReceivedAtMs)
  return Math.max(0, remainingAtReply - Math.floor(elapsedMs / 1000))
}
