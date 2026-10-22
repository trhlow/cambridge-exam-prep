import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr'
import { useEffect, useRef, useState } from 'react'

/** Retry every few seconds for as long as the page is open: an exam room's network can drop for minutes. */
const RETRY_MS = 3_000

/**
 * The API drops a connection it has not heard from for 12 s and pings every 5 s (Program.cs, AddSignalR). The
 * client must ping at most every 6 s, or a quiet page — the monitor, a candidate between saves — is cut off and
 * reconnects every 12 s, missing updates and showing candidates offline; VE-41 found the default 15 s did that.
 */
export const KEEP_ALIVE_MS = 4_000
/** The server pings every 5 s: two missed pings mean the connection is gone. */
export const SERVER_TIMEOUT_MS = 12_000

/**
 * VE-13: a live connection to one of the API's SignalR hubs (`/hubs/staff`, `/hubs/candidate`), sharing the page's
 * sign-in cookie. `handlers` receive the server's messages; `onConnected` runs after every connect and reconnect
 * (to join a section again). Returns whether the connection is up, so the page can poll slower meanwhile and
 * fall back to polling when it is down.
 */
export function useLive(path: string, handlers: Record<string, (...args: never[]) => void>, onConnected?: (connection: HubConnection) => Promise<unknown>, key: unknown = path): boolean {
  const [isLive, setIsLive] = useState(false)
  const handlersRef = useRef(handlers)
  const connectedRef = useRef(onConnected)

  useEffect(() => {
    handlersRef.current = handlers
    connectedRef.current = onConnected
  })

  useEffect(() => {
    let isStopped = false
    let retry: ReturnType<typeof setTimeout> | undefined
    const connection = new HubConnectionBuilder()
      .withUrl(path)
      .withAutomaticReconnect({ nextRetryDelayInMilliseconds: () => RETRY_MS })
      .withKeepAliveInterval(KEEP_ALIVE_MS)
      .withServerTimeout(SERVER_TIMEOUT_MS)
      .configureLogging(LogLevel.None)
      .build()

    for (const method of Object.keys(handlersRef.current)) {
      connection.on(method, (...args: never[]) => handlersRef.current[method]?.(...args))
    }

    const connected = async () => {
      await connectedRef.current?.(connection)
      if (!isStopped) {
        setIsLive(true)
      }
    }
    const start = () => {
      connection.start().then(connected).catch(() => {
        if (!isStopped) {
          retry = setTimeout(start, RETRY_MS)
        }
      })
    }

    connection.onreconnecting(() => setIsLive(false))
    connection.onreconnected(() => void connected().catch(() => undefined))
    connection.onclose(() => {
      setIsLive(false)
      if (!isStopped) {
        retry = setTimeout(start, RETRY_MS)
      }
    })
    start()

    return () => {
      isStopped = true
      clearTimeout(retry)
      void connection.stop()
    }
  }, [path, key])

  return isLive
}
