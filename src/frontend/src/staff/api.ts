// Staff portal calls (src/backend/src/ExamPrep.Api: Auth, Admin, Exams, Monitoring, Content, Results).
import { ApiError, del as rawDel, post as rawPost, put as rawPut, request as rawRequest } from '../api/client'

/** Fired when the staff session has ended (expired, signed out elsewhere, password reset): the portal shows the sign-in. */
export const SESSION_ENDED_EVENT = 'victory:staff-session-ended'

function watch<T>(call: Promise<T>): Promise<T> {
  return call.catch((error: unknown) => {
    if (error instanceof ApiError && error.status === 401) {
      window.dispatchEvent(new Event(SESSION_ENDED_EVENT))
    }

    throw error
  })
}

const request = <T>(path: string, init?: RequestInit) => watch(rawRequest<T>(path, init))
const post = <T>(path: string, body?: unknown) => watch(rawPost<T>(path, body))
const put = <T>(path: string, body: unknown) => watch(rawPut<T>(path, body))
const del = (path: string) => watch(rawDel(path))

export type Role = 'Admin' | 'Invigilator'

export interface StaffProfile {
  id: number
  username: string
  fullName: string
  email: string | null
  role: Role
  isActive: boolean
}

export type ExamStatus = 'Draft' | 'Scheduled' | 'Active' | 'Closed'
export const EXAM_STATUSES: ExamStatus[] = ['Draft', 'Scheduled', 'Active', 'Closed']

export interface ExamSectionView {
  id: number
  sectionTemplateId: number
  orderNo: number
  title: string
  durationMinutes: number
  testPaperId: number
  paperTitle: string
}

export interface InvigilatorView {
  id: number
  username: string
  fullName: string
}

export interface ExamListItem {
  id: number
  name: string
  examType: string
  scheduledDate: string
  room: string | null
  status: ExamStatus
  candidates: number
  sections: ExamSectionView[]
}

export interface ExamDetail extends Omit<ExamListItem, 'candidates'> {
  examTypeId: number
  sessionPin: string
  invigilatorPassword: string
  candidates: number
  hasStarted: boolean
  invigilators: InvigilatorView[]
}

export interface SectionTemplateView {
  id: number
  orderNo: number
  title: string
  durationMinutes: number
}

export interface ExamTypeView {
  id: number
  code: string
  name: string
  sections: SectionTemplateView[]
}

export interface SaveExamRequest {
  name: string
  examTypeId: number
  scheduledDate: string
  room: string | null
  papers: { sectionTemplateId: number; testPaperId: number }[]
  sessionPin?: string | null
  invigilatorPassword?: string | null
}

export type PaperStatus = 'Draft' | 'Published' | 'Archived'

export interface PaperListItem {
  id: number
  title: string
  examType: string
  sectionTemplateId: number
  section: string
  status: PaperStatus
  parts: number
  questions: number
  createdAt: string
  usedByExams: number
}

export interface PaperImportSummary {
  id: number
  title: string
  examType: string
  section: string
  status: PaperStatus
  parts: number
  questions: number
}

export interface NewCandidate {
  candidateNumber: string
  firstName: string
  lastName: string
  dateOfBirth?: string | null
  plannedExtraPercent?: number
  seatNo?: string | null
}

export interface CandidateView {
  id: number
  candidateNumber: string
  firstName: string
  lastName: string
  username: string
  dateOfBirth: string | null
  plannedExtraPercent: number
  seatNo: string | null
  hasStarted: boolean
}

export interface LoginSlip {
  candidateId: number
  candidateNumber: string
  fullName: string
  username: string
  password: string | null
  examName: string
  scheduledDate: string
  room: string | null
}

export type AttemptStatus = 'NotStarted' | 'InProgress' | 'Paused' | 'Submitted'

export interface MonitorRow {
  candidateId: number
  candidateNumber: string
  fullName: string
  username: string
  attemptId: number | null
  status: AttemptStatus
  isOnline: boolean
  startedAt: string | null
  endsAt: string | null
  remainingSeconds: number | null
  plannedExtraSeconds: number
  incidentExtraSeconds: number
  endTimeEdited: boolean
  submitReason: string | null
  audioPositionSeconds: number | null
  answered: number
}

export interface MonitorView {
  examId: number
  examName: string
  sectionId: number
  sectionTitle: string
  hasAudio: boolean
  durationMinutes: number
  serverNow: string
  candidates: MonitorRow[]
}

export type MonitorAction = 'Pause' | 'Resume' | 'AddIncidentTime' | 'EndNow' | 'EndAt' | 'EndIn' | 'UseTestEndTime' | 'RewindAudio'

export interface MonitorActionRequest {
  action: MonitorAction
  candidateIds: number[]
  minutes?: number
  endAt?: string
  disregardExtraTime?: boolean
  reason?: string
}

export interface ActionOutcome {
  applied: number
  skipped: { candidateId: number; fullName: string; reason: string }[]
}

export interface MessageView {
  id: number
  text: string
  sentAt: string
  candidateId: number | null
}

export interface SectionResult {
  sectionId: number
  title: string
  status: string
  gradingStatus: string
  rawScore: number | null
  maxScore: number | null
  attemptId: number | null
}

export interface CandidateResult {
  candidateId: number
  candidateNumber: string
  fullName: string
  sections: SectionResult[]
}

const exam = (id: number) => `/api/staff/exams/${id}`

export const staffApi = {
  // Sign-in calls answer 401 on purpose (not signed in, wrong password): they never mean "session ended".
  me: () => rawRequest<StaffProfile>('/api/staff/auth/me'),
  login: (username: string, password: string) => rawPost<StaffProfile>('/api/staff/auth/login', { username, password }),
  logout: () => rawPost<void>('/api/staff/auth/logout'),

  exams: () => request<ExamListItem[]>('/api/staff/exams'),
  exam: (id: number) => request<ExamDetail>(exam(id)),
  loginSlips: (id: number) => request<LoginSlip[]>(`${exam(id)}/login-slips`),
  results: (id: number) => request<CandidateResult[]>(`${exam(id)}/results`),

  monitor: (examId: number, sectionId: number) => request<MonitorView>(`${exam(examId)}/sections/${sectionId}/monitor`),
  act: (examId: number, sectionId: number, body: MonitorActionRequest) => post<ActionOutcome>(`${exam(examId)}/sections/${sectionId}/actions`, body),
  messages: (examId: number) => request<MessageView[]>(`${exam(examId)}/messages`),
  sendMessage: (examId: number, text: string, candidateIds: number[]) => post<MessageView[]>(`${exam(examId)}/messages`, { text, candidateIds }),
}

export const adminApi = {
  examTypes: () => request<ExamTypeView[]>('/api/admin/exam-types'),
  createExam: (body: SaveExamRequest) => post<ExamDetail>('/api/admin/exams', body),
  updateExam: (id: number, body: SaveExamRequest) => put<ExamDetail>(`/api/admin/exams/${id}`, body),
  setStatus: (id: number, status: ExamStatus) => post<ExamDetail>(`/api/admin/exams/${id}/status`, { status }),
  deleteExam: (id: number) => del(`/api/admin/exams/${id}`),
  assignInvigilators: (id: number, userIds: number[]) => put<InvigilatorView[]>(`/api/admin/exams/${id}/invigilators`, { userIds }),

  candidates: (examId: number) => request<CandidateView[]>(`/api/admin/exams/${examId}/candidates`),
  addCandidates: (examId: number, candidates: NewCandidate[]) => post<CandidateView[]>(`/api/admin/exams/${examId}/candidates`, { candidates }),
  updateCandidate: (id: number, body: Required<Omit<NewCandidate, 'candidateNumber'>>) => put<CandidateView>(`/api/admin/candidates/${id}`, body),
  deleteCandidate: (id: number) => del(`/api/admin/candidates/${id}`),
  resetCandidatePassword: (id: number) => post<LoginSlip>(`/api/admin/candidates/${id}/reset-password`),

  papers: () => request<PaperListItem[]>('/api/admin/papers'),
  importPaper: (file: File) => {
    const form = new FormData()
    form.append('package', file)
    return request<PaperImportSummary>('/api/admin/papers/import', { method: 'POST', body: form })
  },
  publishPaper: (id: number) => post<PaperListItem>(`/api/admin/papers/${id}/publish`),
  deletePaper: (id: number) => del(`/api/admin/papers/${id}`),

  users: () => request<StaffProfile[]>('/api/admin/users'),
  createUser: (body: { username: string; fullName: string; password: string; role: Role }) => post<StaffProfile>('/api/admin/users', body),
  updateUser: (id: number, body: { fullName: string; isActive: boolean; newPassword?: string }) => put<StaffProfile>(`/api/admin/users/${id}`, body),
}
