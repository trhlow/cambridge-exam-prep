import { useSite } from './site'

const DEFAULT_NAME = 'Victory'

/**
 * The centre's logo where Cambridge puts its own (exam header, sign-in screens, staff portal, login slips). Until
 * the centre uploads a PNG (VE-24, staff portal → Centre) it is a crest + serif name laid out like the Cambridge
 * English lockup; a renamed centre keeps the crest with its new name.
 */
export function VictoryLogo() {
  const site = useSite()
  const name = site?.centreName ?? DEFAULT_NAME

  if (site?.logoUrl) {
    return (
      <span className="logo">
        <img src={site.logoUrl} alt={name} className="logo-image" />
      </span>
    )
  }

  const isDefault = name === DEFAULT_NAME
  return (
    <span className="logo" aria-label={isDefault ? 'Victory English Centre' : name}>
      <svg viewBox="0 0 28 32" width="26" height="30" aria-hidden="true">
        <path d="M1 1h26v13c0 9-6 14-13 17C7 28 1 23 1 14V1z" fill="#c8102e" />
        <path d="M1 1h13v15H1z" fill="#f2b705" opacity=".9" />
        <path d="M7 7l7 15 7-15" fill="none" stroke="#fff" strokeWidth="3.2" strokeLinejoin="round" />
      </svg>
      <span className="logo-text">
        {isDefault ? (
          <>
            <span className="logo-name">VICTORY</span>
            <span className="logo-sub">English</span>
          </>
        ) : (
          <span className="logo-name">{name}</span>
        )}
      </span>
    </span>
  )
}
