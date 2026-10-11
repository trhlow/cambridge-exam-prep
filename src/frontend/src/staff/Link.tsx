import type { MouseEvent, ReactNode } from 'react'
import { navigate } from './router'

/** An ordinary link that changes page without reloading (Ctrl/middle click still opens a new tab). */
export function Link({ to, className, children }: { to: string; className?: string; children: ReactNode }) {
  const click = (event: MouseEvent<HTMLAnchorElement>) => {
    if (event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey) {
      return
    }

    event.preventDefault()
    navigate(to)
  }

  return (
    <a href={to} className={className} onClick={click}>
      {children}
    </a>
  )
}
