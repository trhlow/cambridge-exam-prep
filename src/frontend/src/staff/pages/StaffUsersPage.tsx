import { useState, type FormEvent } from 'react'
import { adminApi, type Role, type StaffProfile } from '../api'
import { Badge, ErrorBox, Modal, PageHeader } from '../ui'
import { useLoad } from '../useLoad'

/** VE-23: staff accounts. Resetting a password or switching an account off signs it out everywhere. */
export function StaffUsersPage({ currentUserId }: { currentUserId: number }) {
  const { data: users, error, reload } = useLoad(adminApi.users, 'users')
  const [isCreating, setIsCreating] = useState(false)
  const [editing, setEditing] = useState<StaffProfile | null>(null)

  return (
    <>
      <PageHeader
        title="Staff"
        meta="Administrators and invigilators"
        actions={
          <button type="button" className="st-btn st-btn--primary" onClick={() => setIsCreating(true)}>
            New account
          </button>
        }
      />
      <ErrorBox error={error} />
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
                </td>
                <td>{user.isActive ? 'Active' : 'Switched off'}</td>
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
          onCreated={() => {
            setIsCreating(false)
            reload()
          }}
        />
      )}
      {editing && (
        <EditAccount
          user={editing}
          isSelf={editing.id === currentUserId}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null)
            reload()
          }}
        />
      )}
    </>
  )
}

function CreateAccount({ onClose, onCreated }: { onClose: () => void; onCreated: () => void }) {
  const [form, setForm] = useState({ username: '', fullName: '', password: '', role: 'Invigilator' as Role })
  const [error, setError] = useState<unknown>(null)
  const submit = async (event: FormEvent) => {
    event.preventDefault()
    try {
      await adminApi.createUser(form)
      onCreated()
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
          <label className="st-field">
            Role
            <select value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value as Role })}>
              <option value="Invigilator">Invigilator</option>
              <option value="Admin">Administrator</option>
            </select>
          </label>
        </div>
        <ErrorBox error={error} />
        <div className="st-form-actions">
          <button type="submit" className="st-btn st-btn--primary">
            Create
          </button>
        </div>
      </form>
    </Modal>
  )
}

function EditAccount({ user, isSelf, onClose, onSaved }: { user: StaffProfile; isSelf: boolean; onClose: () => void; onSaved: () => void }) {
  const [fullName, setFullName] = useState(user.fullName)
  const [isActive, setIsActive] = useState(user.isActive)
  const [newPassword, setNewPassword] = useState('')
  const [error, setError] = useState<unknown>(null)
  const submit = async (event: FormEvent) => {
    event.preventDefault()
    try {
      await adminApi.updateUser(user.id, { fullName, isActive, newPassword: newPassword || undefined })
      onSaved()
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
        <label className="st-field">
          New password <span className="st-faint">(leave empty to keep it)</span>
          <input type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} minLength={8} autoComplete="new-password" />
        </label>
        <label className="st-radio">
          <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} disabled={isSelf} /> Account active
        </label>
        {isSelf && <p className="st-faint">You cannot switch off your own account.</p>}
        <ErrorBox error={error} />
        <div className="st-form-actions">
          <button type="submit" className="st-btn st-btn--primary">
            Save
          </button>
        </div>
      </form>
    </Modal>
  )
}
