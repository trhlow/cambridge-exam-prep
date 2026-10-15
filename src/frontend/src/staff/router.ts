import { useEffect, useState } from 'react'

/** The staff portal's pages, read from the address bar so a reload or a second tab opens the same page. */
export type Route =
  | { page: 'exams' }
  | { page: 'new-exam' }
  | { page: 'exam'; examId: number }
  | { page: 'edit-exam'; examId: number }
  | { page: 'monitor'; examId: number; sectionId: number }
  | { page: 'slips'; examId: number }
  | { page: 'results'; examId: number }
  | { page: 'marking'; examId: number; attemptId: number }
  | { page: 'papers' }
  | { page: 'paper'; paperId: number }
  | { page: 'users' }
  | { page: 'not-found' }

export function parseRoute(pathname: string): Route {
  const parts = pathname.replace(/^\/staff\/?/, '').split('/').filter(Boolean)
  const id = (value: string | undefined) => (value && /^\d+$/.test(value) ? Number(value) : null)
  const [first, second, third, fourth] = parts

  if (parts.length === 0) return { page: 'exams' }
  if (first === 'papers' && parts.length === 1) return { page: 'papers' }
  if (first === 'papers' && parts.length === 2) {
    const paperId = id(second)
    return paperId === null ? { page: 'not-found' } : { page: 'paper', paperId }
  }
  if (first === 'users' && parts.length === 1) return { page: 'users' }
  if (first !== 'exams') return { page: 'not-found' }
  if (second === 'new' && parts.length === 2) return { page: 'new-exam' }

  const examId = id(second)
  if (examId === null) return parts.length === 1 ? { page: 'exams' } : { page: 'not-found' }
  if (parts.length === 2) return { page: 'exam', examId }
  if (third === 'edit' && parts.length === 3) return { page: 'edit-exam', examId }
  if (third === 'slips' && parts.length === 3) return { page: 'slips', examId }
  if (third === 'results' && parts.length === 3) return { page: 'results', examId }
  const sectionId = id(fourth)
  if (third === 'sections' && sectionId !== null && parts.length === 4) return { page: 'monitor', examId, sectionId }
  if (third === 'marking' && sectionId !== null && parts.length === 4) return { page: 'marking', examId, attemptId: sectionId }
  return { page: 'not-found' }
}

export function navigate(path: string) {
  window.history.pushState(null, '', path)
  window.dispatchEvent(new PopStateEvent('popstate'))
}

export function useRoute(): Route {
  const [route, setRoute] = useState(() => parseRoute(window.location.pathname))

  useEffect(() => {
    const update = () => {
      setRoute(parseRoute(window.location.pathname))
      window.scrollTo(0, 0)
    }
    window.addEventListener('popstate', update)
    return () => window.removeEventListener('popstate', update)
  }, [])

  return route
}
