/**
 * The countdown shown in the header. Minutes are rounded up so the clock never reads "0 minutes" while time
 * remains; the last minute counts down in seconds.
 */
export function timeLeftText(seconds: number): string {
  if (seconds <= 0) {
    return 'Time is up'
  }

  if (seconds < 60) {
    return `${seconds} second${seconds === 1 ? '' : 's'} left`
  }

  const minutes = Math.ceil(seconds / 60)
  return `${minutes} minute${minutes === 1 ? '' : 's'} left`
}

/**
 * Seconds left now, from the server's figure and the time elapsed on this computer since the reply arrived.
 * The computer's own clock may be wrong; only the elapsed time is trusted.
 */
export function remainingSecondsNow(remainingAtReply: number, replyReceivedAtMs: number, nowMs: number): number {
  return Math.max(0, remainingAtReply - Math.floor((nowMs - replyReceivedAtMs) / 1000))
}
