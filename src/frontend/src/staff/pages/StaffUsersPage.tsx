import { useState, type FormEvent } from 'react'
import { adminApi, MANAGER_AREAS, staffApi, type ApprovalView, type ManagerArea, type Role, type StaffProfile } from '../api'
import { Badge, ErrorBox, Modal, PageHeader, RequestSent } from '../ui'
import { useLoad } from '../useLoad'

const ROLES: { value: Role; label: string; help: string }[] = [
  { value: 'Invigilator', label: 'Invigilator', help: 'Runs the exam room for the sessions assigned to them.' },
  { value: 'Examiner', label: 'Examiner', help: 'Reviews finished work, marks Writing and sees results of their sessions.' },
  { value: 'Manager', label: 'Manager', help: 'Creates and edits in the areas below; deletions go to an administrator.' },
  { value: 'Admin', label: 'Administrator', help: 'Everything. Deletions and account changes need the other administrator.' },
]

const AREA_TEXT: Record<ManagerArea, string> = {
  Papers: 'Papers — import, check and publish',
  Exams: 'Exams — sessions, status, invigilators and examiners',
  Candidates: 'Candidates — enter them and print their slips',
  Centre: 'Centre — name and logo',
}

/**
 * VE-23, VE-59: staff accounts, all created by an administrator. Names change at once; switching an account on or
 * off, a new password, a new role or areas wait for the other administrator (VE-60), then sign the account out.
 */
export function StaffUsersPage({ currentUserId }: { currentUserId: number }) {
  const { data: users, error, reload } = useLoad(adminApi.users, 'users')
  const { data: approvals, reload: reloadApprovals } = useLoad(staffApi.approvals, 'approvals')
  const [isCreating, setIsCreating] = useState(false)
  const [editing, setEditing] = useState<StaffProfile | null>(null)
  const [sent, setSent] = useState<ApprovalView | null>(null)
  const waiting = new Set((approvals ?? []).filter((a) => a.status === 'Pending' && a.kind === 'ChangeStaffAccount').map((a) => a.targetId))
  const done = (request: ApprovalView | null) => {
    setSent(request)
    reload()
    reloadApprovals()
  }

  return (
    <>
      <PageHeader
        title="Staff"
        meta="Administrators, managers, examiners and invigilators"
        actions={
          <button type="button" className="st-btn st-btn--primary" onClick={() => setIsCreating(true)}>
            New account
          </button>
        }
      />
      <ErrorBox error={error} />
      {sent && <RequestSent request={sent} onDismiss={() => setSent(null)} />}
      {users && (
        <table className="st-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Username</th>
              <th>Role</th>
              <th>Account</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {users.map((user) => (
              <tr key={user.id} className={user.isActive ? undefined : 'is-muted'}>
                <td>
                  <strong>{user.fullName}</strong>
                  {user.id === currentUserId && <span className="st-faint"> (you)</span>}
                </td>
                <td className="st-mono">{user.username}</td>
                <td>
                  <Badge value={user.role} />
                  {user.areas.length > 0 && <div className="st-faint">{user.areas.join(', ')}</div>}
                </td>
                <td>
                  {user.isActive ? 'Active' : 'Switched off'}
                  {waiting.has(user.id) && (
                    <div>
                      <Badge value="Pending" /> <span className="st-faint">change waiting for approval</span>
                    </div>
                  )}
                </td>
                <td className="st-row-actions">
                  <button type="button" className="st-link" onClick={() => setEditing(user)}>
                    Edit
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {isCreating && (
        <CreateAccount
          onClose={() => setIsCreating(false)}
          onCreated={(request) => {
            setIsCreating(false)
            done(request)
          }}
        />
      )}
      {editing && (
        <EditAccount
          user={editing}
          isSelf={editing.id === currentUserId}
          onClose={() => setEditing(null)}
          onSaved={(request) => {
            setEditing(null)
            done(request)
          }}
        />
      )}
    </>
  )
}

function RoleFields(props: { role: Role; areas: ManagerArea[]; disabled?: boolean; onChange: (role: Role, areas: ManagerArea[]) => void }) {
  const { role, areas, disabled, onChange } = props
  const toggle = (area: ManagerArea) => onChange(role, areas.includes(area) ? areas.filter((a) => a !== area) : MANAGER_AREAS.filter((a) => a === area || areas.includes(a)))
  return (
    <>
      <label className="st-field">
        Role
        <select value={role} disabled={disabled} onChange={(e) => onChange(e.target.value as Role, e.target.value === 'Manager' ? areas : [])}>
          {ROLES.map((r) => (
            <option key={r.value} value={r.value}>
              {r.label}
            </option>
          ))}
        </select>
        <span className="st-faint">{ROLES.find((r) => r.value === role)?.help}</span>
      </label>
      {role === 'Manager' && (
        <fieldset className="st-fieldset" disabled={disabled}>
          <legend>Areas (at least one)</legend>
          {MANAGER_AREAS.map((area) => (
            <label key={area} className="st-radio">
              <input type="checkbox" checked={areas.includes(area)} onChange={() => toggle(area)} /> {AREA_TEXT[area]}
            </label>
          ))}
        </fieldset>
      )}
    </>
  )
}

function CreateAccount({ onClose, onCreated }: { onClose: () => void; onCreated: (request: ApprovalView | null) => void }) {
  const [form, setForm] = useState({ username: '', fullName: '', password: '', role: 'Invigilator' as Role, areas: [] as ManagerArea[] })
  const [error, setError] = useState<unknown>(null)
  const submit = async (event: FormEvent) => {
    event.preventDefault()
    try {
      const created = await adminApi.createUser(form)
      // A third administrator is only a request until the other administrator approves it.
      onCreated('kind' in created ? created : null)
    } catch (e) {
      setError(e)
    }
  }

  return (
    <Modal title="New staff account" onClose={onClose}>
      <form className="st-form" onSubmit={submit}>
        <div className="st-grid-2">
          <label className="st-field">
            Full name
            <input value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} required maxLength={128} />
          </label>
          <label className="st-field">
            Username
            <input value={form.username} onChange={(e) => setForm({ ...form, username: e.target.value })} required minLength={3} maxLength={64} pattern="[A-Za-z0-9._\-]+" autoComplete="off" />
          </label>
          <label className="st-field">
            Password
            <input type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} required minLength={8} autoComplete="new-password" />
          </label>
        </div>
        <RoleFields role={form.role} areas={form.areas} onChange={(role, areas) => setForm({ ...form, role, areas })} />
        <ErrorBox error={error} />
        <div className="st-form-actions">
          <button type="submit" className="st-btn st-btn--primary" disabled={form.role === 'Manager' && form.areas.length === 0}>
            Create
          </button>
        </div>
      </form>
    </Modal>
  )
}

function EditAccount(props: { user: StaffProfile; isSelf: boolean; onClose: () => void; onSaved: (request: ApprovalView | null) => void }) {
  const { user, isSelf, onClose, onSaved } = props
  const [fullName, setFullName] = useState(user.fullName)
  const [isActive, setIsActive] = useState(user.isActive)
  const [role, setRole] = useState(user.role)
  const [areas, setAreas] = useState(user.areas)
  const [newPassword, setNewPassword] = useState('')
  const [error, setError] = useState<unknown>(null)
  const roleChanged = role !== user.role
  const areasChanged = role === 'Manager' && [...areas].sort().join() !== [...user.areas].sort().join()
  const submit = async (event: FormEvent) => {
    event.preventDefault()
    try {
      const result = await adminApi.updateUser(user.id, {
        fullName,
        isActive,
        newPassword: newPassword || undefined,
        role: roleChanged ? role : undefined,
        areas: roleChanged || areasChanged ? areas : undefined,
      })
      onSaved(result.request)
    } catch (e) {
      setError(e)
    }
  }

  return (
    <Modal title={user.username} onClose={onClose}>
      <form className="st-form" onSubmit={submit}>
        <label className="st-field">
          Full name
          <input value={fullName} onChange={(e) => setFullName(e.target.value)} required />
        </label>
        <RoleFields
          role={role}
          areas={areas}
          disabled={isSelf}
          onChange={(nextRole, nextAreas) => {
            setRole(nextRole)
            setAreas(nextAreas)
          }}
        />
        <label className="st-field">
          New password <span className="st-faint">(leave empty to keep it)</span>
          <input type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} minLength={8} autoComplete="new-password" />
        </label>
        <label className="st-radio">
          <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} disabled={isSelf} /> Account active
        </label>
        {isSelf && <p className="st-faint">You cannot switch off your own account or change your own role.</p>}
        <p className="st-faint">The name changes at once. A new role, areas, password, or switching the account on or off waits for the other administrator, then signs the account out.</p>
        <ErrorBox error={error} />
        <div className="st-form-actions">
          <button type="submit" className="st-btn st-btn--primary" disabled={role === 'Manager' && areas.length === 0}>
            Save
          </button>
        </div>
      </form>
    </Modal>
  )
}
