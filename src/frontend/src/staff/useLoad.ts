import { useCallback, useEffect, useRef, useState } from 'react'

/** Data for a page: loads on mount and when `key` changes; `reload` after a change. A late answer is ignored. */
export function useLoad<T>(load: () => Promise<T>, key: string | number) {
  const [data, setData] = useState<T | null>(null)
  const [error, setError] = useState<unknown>(null)
  const [version, setVersion] = useState(0)
  const loadRef = useRef(load)

  useEffect(() => {
    loadRef.current = load
  })

  useEffect(() => {
    let isCurrent = true
    loadRef.current().then(
      (value) => {
        if (isCurrent) {
          setData(value)
          setError(null)
        }
      },
      (e: unknown) => isCurrent && setError(e),
    )
    return () => {
      isCurrent = false
    }
  }, [key, version])

  const reload = useCallback(() => setVersion((v) => v + 1), [])
  return { data, error, reload, setData }
}
