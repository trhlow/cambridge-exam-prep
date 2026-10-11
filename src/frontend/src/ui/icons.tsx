// Line icons drawn to match the Inspera candidate header (connection, messages, menu, notes) and bookmark.

type IconProps = { className?: string }

const stroke = { fill: 'none', stroke: 'currentColor', strokeWidth: 2, strokeLinecap: 'round', strokeLinejoin: 'round' } as const

export function WifiIcon({ className }: IconProps) {
  return (
    <svg className={className} viewBox="0 0 24 24" width="26" height="26" aria-hidden="true">
      <path {...stroke} strokeWidth={2.6} d="M2.5 9a14 14 0 0 1 19 0M5.8 12.6a9 9 0 0 1 12.4 0M9.2 16.1a4.2 4.2 0 0 1 5.6 0" />
      <circle cx="12" cy="19.4" r="1.6" fill="currentColor" />
    </svg>
  )
}

export function BellIcon({ className }: IconProps) {
  return (
    <svg className={className} viewBox="0 0 24 24" width="26" height="26" aria-hidden="true">
      <path {...stroke} d="M6 17V11a6 6 0 0 1 12 0v6l1.5 2h-15L6 17zM10 21h4M12 3v2" />
    </svg>
  )
}

export function MenuIcon({ className }: IconProps) {
  return (
    <svg className={className} viewBox="0 0 24 24" width="26" height="26" aria-hidden="true">
      <path {...stroke} strokeWidth={3} d="M3.5 6h17M3.5 12h17M3.5 18h17" />
    </svg>
  )
}

export function NotesIcon({ className }: IconProps) {
  return (
    <svg className={className} viewBox="0 0 24 24" width="26" height="26" aria-hidden="true">
      <path {...stroke} d="M13 4H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-7" />
      <path {...stroke} d="M18.5 2.8a2 2 0 0 1 2.7 2.7L12 14.7l-3.6.9.9-3.6 9.2-9.2z" />
    </svg>
  )
}

export function BookmarkIcon({ className, filled }: IconProps & { filled: boolean }) {
  return (
    <svg className={className} viewBox="0 0 24 24" width="20" height="20" aria-hidden="true">
      <path d="M6 3h12v18l-6-4.5L6 21V3z" fill={filled ? 'currentColor' : 'none'} stroke="currentColor" strokeWidth="1.6" strokeLinejoin="round" />
    </svg>
  )
}

export function ArrowIcon({ direction }: { direction: 'left' | 'right' }) {
  const d = direction === 'right' ? 'M4 12h14M12 5l7 7-7 7' : 'M20 12H6M12 5l-7 7 7 7'
  return (
    <svg viewBox="0 0 24 24" width="30" height="30" aria-hidden="true">
      <path d={d} fill="none" stroke="currentColor" strokeWidth="3.4" strokeLinecap="square" />
    </svg>
  )
}

export function CheckIcon() {
  return (
    <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true">
      <path d="M4 12.5l5 5L20 6.5" fill="none" stroke="currentColor" strokeWidth="3.2" />
    </svg>
  )
}
