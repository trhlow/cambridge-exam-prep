import { useEffect, useState } from 'react'

/** VE-24: the centre's name and logo (GET /api/site, readable before signing in). */
export interface SiteView {
  centreName: string
  /** Null while the centre uses the built-in Victory logo. */
  logoUrl: string | null
  updatedAt: string
}

let current: SiteView | null = null
let loading: Promise<void> | null = null
const listeners = new Set<(site: SiteView | null) => void>()

function publish(site: SiteView | null) {
  current = site
  listeners.forEach((listener) => listener(site))
}

function load() {
  loading ??= fetch('/api/site', { headers: { Accept: 'application/json' } })
    .then((response) => (response.ok ? (response.json() as Promise<SiteView>) : null))
    .catch(() => null)
    .then((site) => {
      // A failed load is tried again by the next screen that needs the centre.
      if (!site) {
        loading = null
      }

      publish(site ?? current)
    })
  return loading
}

/** After the staff portal changes the name or logo, every header shows the new one at once. */
export function setSite(site: SiteView) {
  publish(site)
}

/** The centre's name and logo, loaded once per page; null until loaded or when the API cannot be reached. */
export function useSite(): SiteView | null {
  const [site, setSiteState] = useState(current)

  useEffect(() => {
    listeners.add(setSiteState)
    void load()
    return () => {
      listeners.delete(setSiteState)
    }
  }, [])

  return site
}
