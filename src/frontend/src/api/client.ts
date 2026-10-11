import type { CandidateProfile, CandidateSection, Paper } from './types'

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

/** Same-origin JSON calls; the session lives in an HttpOnly cookie set by the API. */
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(path, {
    credentials: 'same-origin',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    ...init,
  })

  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    throw new ApiError(response.status, problem?.title ?? response.statusText)
  }

  return response.status === 204 ? (undefined as T) : ((await response.json()) as T)
}

const post = <T>(path: string, body?: unknown) => request<T>(path, { method: 'POST', body: body === undefined ? undefined : JSON.stringify(body) })

export const candidateApi = {
  me: () => request<CandidateProfile>('/api/candidate/auth/me'),
  login: (username: string, password: string) => post<CandidateProfile>('/api/candidate/auth/login', { username, password }),
  enterPin: (pin: string) => post<CandidateProfile>('/api/candidate/auth/pin', { pin }),
  logout: () => post<void>('/api/candidate/auth/logout'),
  sections: () => request<CandidateSection[]>('/api/candidate/sections'),
  paper: (sectionId: number) => request<Paper>(`/api/candidate/sections/${sectionId}/paper`),
}
