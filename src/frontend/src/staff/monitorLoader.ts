/**
 * B6: the monitoring page loads its view one request at a time. A change reported while a load is under way (a
 * SignalR `sectionChanged`) used to cancel that load and start another, so in a busy room none ever finished. Now it
 * only marks the view dirty, and exactly one more load follows the one in flight. A response is shown unless a newer
 * one already was. A load that does not answer within `LOAD_TIMEOUT_MS` counts as failed (a hung request would otherwise
 * stop every later load), and its late answer is dropped.
 */
export interface MonitorLoader {
  /** Load now, or once the load in flight is done. */
  request: () => void
  /** Show nothing more (the page was left or the section changed). */
  dispose: () => void
}

export const LOAD_TIMEOUT_MS = 15_000

export function createMonitorLoader<T>(fetch: () => Promise<T>, onView: (view: T) => void, onError: (error: unknown) => void): MonitorLoader {
  let isLoading = false
  let isDirty = false
  let isDisposed = false

  const request = () => {
    if (isLoading) {
      isDirty = true
      return
    }

    isLoading = true
    // Loads never overlap, so a response is always the newest unless its load timed out: then it is dropped.
    let isSettled = false
    const settle = (apply: () => void) => {
      if (isSettled) {
        return
      }
      isSettled = true
      clearTimeout(timer)
      if (!isDisposed) {
        apply()
      }
      isLoading = false
      if (isDirty && !isDisposed) {
        isDirty = false
        request()
      }
    }
    const timer = setTimeout(() => settle(() => onError(new Error('The monitor did not answer in time.'))), LOAD_TIMEOUT_MS)

    fetch().then(
      (view) => settle(() => onView(view)),
      (error: unknown) => settle(() => onError(error)),
    )
  }

  return { request, dispose: () => { isDisposed = true } }
}
