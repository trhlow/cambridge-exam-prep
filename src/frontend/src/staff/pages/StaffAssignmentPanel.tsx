import { useState } from 'react'
import { adminApi, type AssignableStaff, type AssignedStaffView, type ExamDetail } from '../api'
import { ErrorBox } from '../ui'

export type AssignedRole = 'Invigilator' | 'Examiner' | 'CandidatesManager'

const TEXT: Record<AssignedRole, { title: string; help: string; none: string }> = {
  Invigilator: { title: 'Invigilators', help: 'They open this session, print the slips and monitor its sections.', none: 'No invigilator accounts yet.' },
  Examiner: { title: 'Examiners', help: 'Writing scripts are dealt to them at random and evenly, known by a code only. They mark them under Marking and see no names, other parts or results.', none: 'No examiner accounts yet.' },
  CandidatesManager: { title: 'Candidates managers', help: 'They enter and correct the candidates of this session and print their slips.', none: 'No manager works on candidates yet.' },
}

/** The accounts a panel offers: candidates managers are managers working on candidates (the server sends only those). */
const roleOf: Record<AssignedRole, string> = { Invigilator: 'Invigilator', Examiner: 'Examiner', CandidatesManager: 'Manager' }

function assignedOf(exam: ExamDetail, role: AssignedRole): AssignedStaffView[] {
  return role === 'Invigilator' ? exam.invigilators : role === 'Examiner' ? exam.examiners : (exam.candidateManagers ?? [])
}

function save(examId: number, role: AssignedRole, userIds: number[]) {
  return role === 'Invigilator' ? adminApi.assignInvigilators(examId, userIds) : role === 'Examiner' ? adminApi.assignExaminers(examId, userIds) : adminApi.assignCandidateManagers(examId, userIds)
}

/**
 * VE-23, VE-61, VE-72: who works on this session. Invigilators, examiners and managers of candidates see only the
 * sessions they are assigned to; accounts are created by an administrator under Staff.
 */
export function StaffAssignmentPanel(props: { exam: ExamDetail; role: AssignedRole; staff: AssignableStaff[] | null; onChanged: (assigned: AssignedStaffView[]) => void }) {
  const { exam, role, staff, onChanged } = props
  const [error, setError] = useState<unknown>(null)
  const assigned = new Set(assignedOf(exam, role).map((i) => i.id))
  const accounts = (staff ?? []).filter((u) => u.role === roleOf[role] && (u.isActive || assigned.has(u.id)))

  const toggle = async (userId: number) => {
    const next = assigned.has(userId) ? [...assigned].filter((id) => id !== userId) : [...assigned, userId]
    setError(null)
    try {
      onChanged(await save(exam.id, role, next))
    } catch (e) {
      setError(e)
    }
  }

  return (
    <section className="st-panel">
      <div className="st-panel-head">
        <h2 className="st-h2">{TEXT[role].title}</h2>
      </div>
      <p className="st-faint">{TEXT[role].help}</p>
      <ErrorBox error={error} />
      {staff && accounts.length === 0 && <p className="st-empty">{TEXT[role].none} An administrator creates them under Staff.</p>}
      <ul className="st-checklist">
        {accounts.map((user) => (
          <li key={user.id}>
            <label>
              <input type="checkbox" checked={assigned.has(user.id)} onChange={() => void toggle(user.id)} disabled={!user.isActive && !assigned.has(user.id)} />
              {user.fullName} <span className="st-faint st-mono">{user.username}</span>
            </label>
          </li>
        ))}
      </ul>
    </section>
  )
}
