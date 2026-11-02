/**
 * B6: the monitoring page loads its view one request at a time. A change reported while a load is under way (a
 * SignalR `sectionChanged`) used to cancel that load and start another, so in a busy room none ever finished. Now it
 * only marks the view dirty, and exactly one more load follows the one in flight. A response is shown unless a newer
 * one already was.
 */
export interface MonitorLoader {
  /** Load now, or once the load in flight is done. */
  request: () => void
  /** Show nothing more (the page was left or the section changed). */
  dispose: () => void
}

export function createMonitorLoader<T>(fetch: () => Promise<T>, onView: (view: T) => void, onError: (error: unknown) => void): MonitorLoader {
  let isLoading = false
  let isDirty = false
  let isDisposed = false
  let started = 0
  let shown = 0

  const request = () => {
    if (isLoading) {
      isDirty = true
      return
    }

    isLoading = true
    const order = ++started
    fetch()
      .then(
        (view) => {
          if (!isDisposed && order > shown) {
            shown = order
            onView(view)
          }
        },
        (error: unknown) => {
          if (!isDisposed) {
            onError(error)
          }
        },
      )
      .finally(() => {
        isLoading = false
        if (isDirty && !isDisposed) {
          isDirty = false
          request()
        }
      })
  }

  return { request, dispose: () => { isDisposed = true } }
}
