import { useState } from 'react'
import { adminApi, type ExamDetail, type InvigilatorView } from '../api'
import { Link } from '../Link'
import { ErrorBox } from '../ui'
import { useLoad } from '../useLoad'

/** VE-23: which invigilators can open this session (they see only their own sessions). */
export function InvigilatorsPanel({ exam, onChanged }: { exam: ExamDetail; onChanged: (invigilators: InvigilatorView[]) => void }) {
  const { data: users } = useLoad(adminApi.users, 'users')
  const [error, setError] = useState<unknown>(null)
  const assigned = new Set(exam.invigilators.map((i) => i.id))
  const invigilators = (users ?? []).filter((u) => u.role === 'Invigilator' && (u.isActive || assigned.has(u.id)))

  const toggle = async (userId: number) => {
    const next = assigned.has(userId) ? [...assigned].filter((id) => id !== userId) : [...assigned, userId]
    setError(null)
    try {
      onChanged(await adminApi.assignInvigilators(exam.id, next))
    } catch (e) {
      setError(e)
    }
  }

  return (
    <section className="st-panel">
      <div className="st-panel-head">
        <h2 className="st-h2">Invigilators</h2>
        <Link to="/staff/users" className="st-link">
          Manage staff accounts
        </Link>
      </div>
      <ErrorBox error={error} />
      {users && invigilators.length === 0 && <p className="st-empty">No invigilator accounts yet. Create them under Staff.</p>}
      <ul className="st-checklist">
        {invigilators.map((user) => (
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
