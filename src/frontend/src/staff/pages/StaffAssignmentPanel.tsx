import { useState } from 'react'
import { adminApi, type AssignableStaff, type AssignedStaffView, type ExamDetail } from '../api'
import { ErrorBox } from '../ui'

const TEXT = {
  Invigilator: { title: 'Invigilators', help: 'They open this session, print the slips and monitor its sections.', none: 'No invigilator accounts yet.' },
  Examiner: { title: 'Examiners', help: 'They review the finished work, mark Writing and see the results.', none: 'No examiner accounts yet.' },
}

/**
 * VE-23, VE-61: who works on this session. Invigilators and examiners see only the sessions they are assigned to;
 * accounts are created by an administrator under Staff.
 */
export function StaffAssignmentPanel(props: { exam: ExamDetail; role: 'Invigilator' | 'Examiner'; staff: AssignableStaff[] | null; onChanged: (assigned: AssignedStaffView[]) => void }) {
  const { exam, role, staff, onChanged } = props
  const [error, setError] = useState<unknown>(null)
  const assigned = new Set((role === 'Invigilator' ? exam.invigilators : exam.examiners).map((i) => i.id))
  const accounts = (staff ?? []).filter((u) => u.role === role && (u.isActive || assigned.has(u.id)))

  const toggle = async (userId: number) => {
    const next = assigned.has(userId) ? [...assigned].filter((id) => id !== userId) : [...assigned, userId]
    setError(null)
    try {
      onChanged(await (role === 'Invigilator' ? adminApi.assignInvigilators(exam.id, next) : adminApi.assignExaminers(exam.id, next)))
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
