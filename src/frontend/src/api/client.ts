import type { AttemptState, CandidateProfile, CandidateSection, Contrast, Paper, TextSize } from './types'

export class ApiError extends Error {
  readonly status: number
  /** Every problem the server found (invalid package, candidate list, exam form…), when it listed them. */
  readonly errors: string[]

  constructor(status: number, message: string, errors: string[] = []) {
    super(message)
    this.status = status
    this.errors = errors
  }
}

/** Same-origin JSON calls; the session lives in an HttpOnly cookie set by the API. */
export async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const isForm = init.body instanceof FormData
  const response = await fetch(path, {
    credentials: 'same-origin',
    headers: isForm ? { Accept: 'application/json' } : { 'Content-Type': 'application/json', Accept: 'application/json' },
    ...init,
  })

  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    const errors = Array.isArray(problem?.errors) ? (problem.errors as string[]) : []
    throw new ApiError(response.status, problem?.title ?? response.statusText, errors)
  }

  return response.status === 204 ? (undefined as T) : ((await response.json()) as T)
}

export const post = <T>(path: string, body?: unknown) => request<T>(path, { method: 'POST', body: body === undefined ? undefined : JSON.stringify(body) })
export const put = <T>(path: string, body: unknown) => request<T>(path, { method: 'PUT', body: JSON.stringify(body) })
export const del = (path: string) => request<void>(path, { method: 'DELETE' })

export const candidateApi = {
  me: () => request<CandidateProfile>('/api/candidate/auth/me'),
  login: (username: string, password: string) => post<CandidateProfile>('/api/candidate/auth/login', { username, password }),
  enterPin: (pin: string) => post<CandidateProfile>('/api/candidate/auth/pin', { pin }),
  logout: () => post<void>('/api/candidate/auth/logout'),
  savePreferences: (textSize: TextSize, contrast: Contrast) => put<CandidateProfile>('/api/candidate/auth/preferences', { textSize, contrast }),
  sections: () => request<CandidateSection[]>('/api/candidate/sections'),
  paper: (sectionId: number) => request<Paper>(`/api/candidate/sections/${sectionId}/paper`),
  /** Starts the section, or returns the running attempt unchanged (after a reload). */
  startAttempt: (sectionId: number) => post<AttemptState>(`/api/candidate/sections/${sectionId}/attempt`),
  attempt: (sectionId: number) => request<AttemptState>(`/api/candidate/sections/${sectionId}/attempt`),
  /** Every few seconds: keeps the candidate online for the invigilator and returns the clock and messages. */
  // The computer's clock goes along so the invigilator is warned about one set far off (VE-29).
  heartbeat: (sectionId: number) =>
    request<AttemptState>(`/api/candidate/sections/${sectionId}/heartbeat`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json', 'X-Client-Time': String(Date.now()) },
    }),
  /** Listening: starts the recording once; later calls return where it is now. */
  playAudio: (sectionId: number) => post<AttemptState>(`/api/candidate/sections/${sectionId}/audio`),
  saveResponses: (sectionId: number, responses: { questionNumber: number; value: string }[]) =>
    request<void>(`/api/candidate/sections/${sectionId}/responses`, { method: 'PUT', body: JSON.stringify({ responses }) }),
}
