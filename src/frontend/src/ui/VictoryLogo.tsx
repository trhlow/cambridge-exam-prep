/**
 * Placeholder centre logo laid out like the Cambridge English lockup (crest + serif name). It will be
 * replaced by the logo the administrator uploads (site settings) once the admin portal exists.
 */
export function VictoryLogo() {
  return (
    <span className="logo" aria-label="Victory English Centre">
      <svg viewBox="0 0 28 32" width="26" height="30" aria-hidden="true">
        <path d="M1 1h26v13c0 9-6 14-13 17C7 28 1 23 1 14V1z" fill="#c8102e" />
        <path d="M1 1h13v15H1z" fill="#f2b705" opacity=".9" />
        <path d="M7 7l7 15 7-15" fill="none" stroke="#fff" strokeWidth="3.2" strokeLinejoin="round" />
      </svg>
      <span className="logo-text">
        <span className="logo-name">VICTORY</span>
        <span className="logo-sub">English</span>
      </span>
    </span>
  )
}
