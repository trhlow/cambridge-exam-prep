import { useRef, useState, type FormEvent } from 'react'
import { setSite, useSite } from '../../ui/site'
import { VictoryLogo } from '../../ui/VictoryLogo'
import { adminApi } from '../api'
import { ErrorBox, PageHeader } from '../ui'

/**
 * VE-24: the centre's name and logo, shown on the sign-in screens, the exam header, the staff portal and the login
 * slips. Administrators and managers of the Centre area change them; the logo is a PNG of at most 1 MB.
 */
export function CentrePage() {
  const site = useSite()
  const [name, setName] = useState<string | null>(null)
  const [error, setError] = useState<unknown>(null)
  const [saved, setSaved] = useState<string | null>(null)
  const [isBusy, setIsBusy] = useState(false)
  const fileRef = useRef<HTMLInputElement>(null)

  if (!site) {
    return <p className="st-empty">Loading…</p>
  }

  const run = async (action: () => Promise<Parameters<typeof setSite>[0]>, done: string) => {
    setIsBusy(true)
    setError(null)
    setSaved(null)
    try {
      setSite(await action())
      setSaved(done)
    } catch (e) {
      setError(e)
    } finally {
      setIsBusy(false)
    }
  }
  const rename = (event: FormEvent) => {
    event.preventDefault()
    void run(() => adminApi.updateSite((name ?? site.centreName).trim()), 'The centre name is saved.')
  }
  const upload = (file: File | undefined) => {
    if (file) {
      void run(() => adminApi.uploadLogo(file), 'The new logo is in use.').finally(() => {
        if (fileRef.current) {
          fileRef.current.value = ''
        }
      })
    }
  }

  return (
    <>
      <PageHeader title="Centre" meta="Name and logo shown to candidates and staff" />
      <ErrorBox error={error} />
      {saved && (
        <p className="st-note" role="status">
          {saved}
        </p>
      )}
      <div className="st-bento">
        <section className="st-card">
          <h2 className="st-h2">Centre name</h2>
          <form className="st-form" onSubmit={rename}>
            <label className="st-field">
              Name
              <input value={name ?? site.centreName} onChange={(e) => setName(e.target.value)} required maxLength={128} />
            </label>
            <div className="st-form-actions">
              <button type="submit" className="st-btn st-btn--primary" disabled={isBusy}>
                Save name
              </button>
            </div>
          </form>
        </section>
        <section className="st-card">
          <h2 className="st-h2">Logo</h2>
          <div className="st-logo-preview">
            <VictoryLogo />
          </div>
          <p className="st-faint">PNG, at most 1 MB; shown at 40 pixels high. Until one is uploaded the built-in Victory crest is used.</p>
          <label className="st-btn">
            Upload PNG…
            <input ref={fileRef} type="file" accept="image/png" hidden disabled={isBusy} onChange={(e) => upload(e.target.files?.[0])} />
          </label>
        </section>
      </div>
    </>
  )
}
