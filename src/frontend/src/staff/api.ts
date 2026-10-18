// Staff portal calls (src/backend/src/ExamPrep.Api: Auth, Admin, Exams, Monitoring, Content, Results).
import { ApiError, post as rawPost, put as rawPut, request as rawRequest } from '../api/client'
import type { SiteView } from '../ui/site'

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
/** Deletions wait for a second administrator (VE-60): the server answers with the approval request it filed. */
const requestDeletion = (path: string) => request<ApprovalView>(path, { method: 'DELETE' })

export type Role = 'Admin' | 'Manager' | 'Examiner' | 'Invigilator'

/** What a manager works in; administrators work in every area. */
export type ManagerArea = 'Papers' | 'Exams' | 'Candidates' | 'Centre'
export const MANAGER_AREAS: ManagerArea[] = ['Papers', 'Exams', 'Candidates', 'Centre']

export interface StaffProfile {
  id: number
  username: string
  fullName: string
  email: string | null
  role: Role
  isActive: boolean
  areas: ManagerArea[]
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

/** An invigilator or examiner assigned to a session. */
export interface AssignedStaffView {
  id: number
  username: string
  fullName: string
}

/** An invigilator or examiner account that a session can be given. */
export interface AssignableStaff extends AssignedStaffView {
  role: Role
  isActive: boolean
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
  invigilators: AssignedStaffView[]
  examiners: AssignedStaffView[]
  /** VE-28: set when the exam only accepts Safe Exam Browser with these settings; never sent to examiners. */
  sebConfigKey: string | null
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
  /** Empty switches Safe Exam Browser off. */
  sebConfigKey?: string
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

/** A paper converted from the PDFs and imported as a draft, with what the converter asks a person to check. */
export interface PaperConversionResult extends PaperImportSummary {
  checks: string[]
}

export interface PaperConversionUpload {
  title: string
  questions: File
  answerKey: File
  /** Listening: one track per part, index 0 = Part 1; empty slots are left out. */
  audio: (File | null)[]
}

export interface PaperOption {
  key: string
  text: string | null
  imageUrl: string | null
}

export interface PaperQuestionDetail {
  number: number
  promptHtml: string | null
  stimulusHtml: string | null
  options: PaperOption[] | null
  answers: string[]
  points: number
  minWords: number | null
}

export interface PaperGroupDetail {
  orderNo: number
  interactionType: string
  instructions: string
  bodyHtml: string | null
  optionPool: PaperOption[] | null
  questions: PaperQuestionDetail[]
}

export interface PaperPartDetail {
  partNo: number
  title: string | null
  layout: string
  stimulusHtml: string | null
  audioUrl: string | null
  groups: PaperGroupDetail[]
}

export interface PaperDetail {
  id: number
  title: string
  examType: string
  section: string
  status: PaperStatus
  parts: PaperPartDetail[]
}

export interface OptionEdit {
  key: string
  text: string | null
}

export interface GroupEdit {
  instructions: string
  bodyHtml: string | null
  optionPool?: OptionEdit[]
}

export interface QuestionEdit {
  promptHtml: string | null
  stimulusHtml: string | null
  options?: OptionEdit[]
  answers: string[]
  points: number
  minWords: number | null
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
  /** Unresolved warnings (VE-29). */
  warnings: number
}

export type WarningType = 'InvalidSebKey' | 'LockdownBrowserExited' | 'ConnectionLost' | 'ClockSkew' | 'StartedEarly'

export interface WarningView {
  id: number
  candidateId: number
  fullName: string
  type: WarningType
  detail: string | null
  detectedAt: string
  resolvedAt: string | null
  resolvedBy: string | null
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

export interface QuestionMark {
  number: number
  partNo: number
  interactionType: string
  response: string | null
  acceptedAnswers: string[]
  isCorrect: boolean | null
  score: number | null
  points: number
  comment: string | null
}

export interface AttemptMarking {
  attemptId: number
  sectionTitle: string
  gradingStatus: string
  rawScore: number | null
  maxScore: number | null
  questions: QuestionMark[]
}

export interface CandidateResult {
  candidateId: number
  candidateNumber: string
  fullName: string
  sections: SectionResult[]
}

export type ApprovalKind = 'DeletePaper' | 'DeleteExam' | 'DeleteCandidate' | 'ChangeStaffAccount' | 'CreateAdmin'
export type ApprovalStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled' | 'Failed'

/** An action waiting for (or decided by) an administrator other than the person who asked for it. */
export interface ApprovalView {
  id: number
  kind: ApprovalKind
  targetId: number | null
  summary: string
  status: ApprovalStatus
  requestedById: number
  requestedBy: string
  requestedAt: string
  decidedById: number | null
  decidedBy: string | null
  decidedAt: string | null
  decisionNote: string | null
  outcome: string | null
}

export interface NewStaffAccount {
  username: string
  fullName: string
  password: string
  role: Role
  areas: ManagerArea[]
}

export interface StaffAccountUpdate {
  fullName: string
  isActive: boolean
  newPassword?: string
  role?: Role
  areas?: ManagerArea[]
}

/** Name changes apply at once; anything else in the update waits in `request`. */
export interface UpdateStaffResult {
  user: StaffProfile
  request: ApprovalView | null
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
  warnings: (examId: number, sectionId: number) => request<WarningView[]>(`${exam(examId)}/sections/${sectionId}/warnings`),
  resolveWarning: (examId: number, sectionId: number, id: number) => post<WarningView>(`${exam(examId)}/sections/${sectionId}/warnings/${id}/resolve`),

  approvals: () => request<ApprovalView[]>('/api/staff/approvals'),
  approve: (id: number) => post<ApprovalView>(`/api/staff/approvals/${id}/approve`),
  reject: (id: number, note: string) => post<ApprovalView>(`/api/staff/approvals/${id}/reject`, { note }),
  cancelRequest: (id: number) => post<ApprovalView>(`/api/staff/approvals/${id}/cancel`),
}

export const adminApi = {
  examTypes: () => request<ExamTypeView[]>('/api/admin/exam-types'),
  createExam: (body: SaveExamRequest) => post<ExamDetail>('/api/admin/exams', body),
  updateExam: (id: number, body: SaveExamRequest) => put<ExamDetail>(`/api/admin/exams/${id}`, body),
  setStatus: (id: number, status: ExamStatus) => post<ExamDetail>(`/api/admin/exams/${id}/status`, { status }),
  deleteExam: (id: number) => requestDeletion(`/api/admin/exams/${id}`),
  examPapers: () => request<PaperListItem[]>('/api/admin/exam-papers'),
  assignableStaff: () => request<AssignableStaff[]>('/api/admin/assignable-staff'),
  assignInvigilators: (id: number, userIds: number[]) => put<AssignedStaffView[]>(`/api/admin/exams/${id}/invigilators`, { userIds }),
  assignExaminers: (id: number, userIds: number[]) => put<AssignedStaffView[]>(`/api/admin/exams/${id}/examiners`, { userIds }),

  candidates: (examId: number) => request<CandidateView[]>(`/api/admin/exams/${examId}/candidates`),
  addCandidates: (examId: number, candidates: NewCandidate[]) => post<CandidateView[]>(`/api/admin/exams/${examId}/candidates`, { candidates }),
  updateCandidate: (id: number, body: Required<Omit<NewCandidate, 'candidateNumber'>>) => put<CandidateView>(`/api/admin/candidates/${id}`, body),
  deleteCandidate: (id: number) => requestDeletion(`/api/admin/candidates/${id}`),
  resetCandidatePassword: (id: number) => post<LoginSlip>(`/api/admin/candidates/${id}/reset-password`),

  papers: () => request<PaperListItem[]>('/api/admin/papers'),
  importPaper: (file: File) => {
    const form = new FormData()
    form.append('package', file)
    return request<PaperImportSummary>('/api/admin/papers/import', { method: 'POST', body: form })
  },
  convertPaper: (upload: PaperConversionUpload) => {
    const form = new FormData()
    form.append('title', upload.title)
    form.append('questions', upload.questions)
    form.append('answerKey', upload.answerKey)
    upload.audio.forEach((file, index) => {
      if (file) {
        form.append(`audio${index + 1}`, file)
      }
    })
    return request<PaperConversionResult>('/api/admin/papers/convert', { method: 'POST', body: form })
  },
  paper: (id: number) => request<PaperDetail>(`/api/admin/papers/${id}`),
  publishPaper: (id: number) => post<PaperListItem>(`/api/admin/papers/${id}/publish`),
  // VE-25: fixing a draft on the web; each call answers with the whole paper as it now is.
  editPaperTitle: (id: number, title: string) => put<PaperDetail>(`/api/admin/papers/${id}`, { title }),
  editPart: (id: number, partNo: number, body: { title: string | null; stimulusHtml: string | null }) => put<PaperDetail>(`/api/admin/papers/${id}/parts/${partNo}`, body),
  editGroup: (id: number, partNo: number, orderNo: number, body: GroupEdit) => put<PaperDetail>(`/api/admin/papers/${id}/parts/${partNo}/groups/${orderNo}`, body),
  editQuestion: (id: number, number: number, body: QuestionEdit) => put<PaperDetail>(`/api/admin/papers/${id}/questions/${number}`, body),
  replaceAudio: (id: number, partNo: number, file: File) => {
    const form = new FormData()
    form.append('audio', file)
    return request<PaperDetail>(`/api/admin/papers/${id}/parts/${partNo}/audio`, { method: 'POST', body: form })
  },
  deletePaper: (id: number) => requestDeletion(`/api/admin/papers/${id}`),

  marking: (attemptId: number) => request<AttemptMarking>(`/api/staff/attempts/${attemptId}/marking`),
  markWriting: (attemptId: number, questionNumber: number, score: number, comment: string) =>
    post<AttemptMarking>(`/api/staff/attempts/${attemptId}/writing-marks`, { questionNumber, score, comment }),

  users: () => request<StaffProfile[]>('/api/admin/users'),
  /** A third administrator waits for approval: the answer is then the request, not the account. */
  createUser: (body: NewStaffAccount) => post<StaffProfile | ApprovalView>('/api/admin/users', body),
  updateUser: (id: number, body: StaffAccountUpdate) => put<UpdateStaffResult>(`/api/admin/users/${id}`, body),

  updateSite: (centreName: string) => put<SiteView>('/api/admin/site', { centreName }),
  uploadLogo: (file: File) => {
    const form = new FormData()
    form.append('logo', file)
    return request<SiteView>('/api/admin/site/logo', { method: 'POST', body: form })
  },
}
