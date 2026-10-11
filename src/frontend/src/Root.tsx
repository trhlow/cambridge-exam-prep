import { Suspense, lazy } from 'react'
import App from './App.tsx'

// The staff portal is a separate bundle: candidate computers never download it.
const StaffApp = lazy(() => import('./staff/StaffApp.tsx'))

/** /staff… is the staff portal (administrators, invigilators); everything else is the candidate's test. */
export default function Root() {
  const path = window.location.pathname
  if (path === '/staff' || path.startsWith('/staff/')) {
    return (
      <Suspense fallback={<div className="screen-message">Loading…</div>}>
        <StaffApp />
      </Suspense>
    )
  }

  return <App />
}
