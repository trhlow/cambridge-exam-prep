import { useRef, useState, type FormEvent } from 'react'
import { adminApi, type ApprovalView, type PaperConversionResult, type PaperImportSummary, type PaperListItem } from '../api'
import { navigate } from '../router'
import { Link } from '../Link'
import { Badge, ConfirmButton, ErrorBox, Modal, PageHeader, RequestSent } from '../ui'
import { useLoad } from '../useLoad'

const PARTS = [1, 2, 3, 4, 5]

/**
 * The paper library. A paper comes in from Victory's PDFs (converted on the server) or as a ready package (.zip);
 * either way it arrives as a draft, is checked on its own page, then published so it can be scheduled. A paper used
 * by an exam cannot be deleted; deleting any other waits for a second administrator (VE-60).
 */
export function PapersPage() {
  const { data: papers, error, reload } = useLoad(adminApi.papers, 'papers')
  const [importError, setImportError] = useState<unknown>(null)
  const [imported, setImported] = useState<PaperImportSummary | null>(null)
  const [isImporting, setIsImporting] = useState(false)
  const [isConverting, setIsConverting] = useState(false)
  const [converted, setConverted] = useState<PaperConversionResult | null>(null)
  const [sent, setSent] = useState<ApprovalView | null>(null)
  const [isStartingBlank, setIsStartingBlank] = useState(false)
  const fileRef = useRef<HTMLInputElement>(null)

  const upload = async (file: File | undefined) => {
    if (!file) {
      return
    }

    setIsImporting(true)
    setImportError(null)
    setImported(null)
    try {
      setImported(await adminApi.importPaper(file))
      reload()
    } catch (e) {
      setImportError(e)
    } finally {
      setIsImporting(false)
      if (fileRef.current) {
        fileRef.current.value = ''
      }
    }
  }

  const act = async (action: () => Promise<unknown>) => {
    setImportError(null)
    try {
      await action()
      reload()
    } catch (e) {
      setImportError(e)
    }
  }

  return (
    <>
      <PageHeader
        title="Papers"
        meta="Exam packages imported into the system"
        actions={
          <>
            <button type="button" className="st-btn st-btn--primary" onClick={() => setIsConverting(true)}>
              Import from PDF
            </button>
            <button type="button" className="st-btn" onClick={() => setIsStartingBlank(true)}>
              New blank paper
            </button>
            <label className={isImporting ? 'st-btn is-busy' : 'st-btn'}>
            {isImporting ? 'Importing…' : 'Import package (.zip)'}
              <input ref={fileRef} type="file" accept=".zip,application/zip" className="visually-hidden" onChange={(e) => void upload(e.target.files?.[0])} disabled={isImporting} />
            </label>
          </>
        }
      />
      <div className="st-steps">
        <span>1. Import from PDF: the question paper, the answer key and, for Listening, one recording per part</span>
        <span>2. It arrives as a draft: open it and check every question</span>
        <span>3. Publish it</span>
        <span>4. Choose it when creating an exam</span>
      </div>
      {isStartingBlank && <BlankPaperDialog onClose={() => setIsStartingBlank(false)} />}
      {isConverting && (
        <ConvertDialog
          onClose={() => setIsConverting(false)}
          onConverted={(result) => {
            setIsConverting(false)
            setConverted(result)
            setImported(null)
            reload()
          }}
        />
      )}
      {converted && (
        <div className={converted.checks.length > 0 ? 'st-outcome st-outcome--warn' : 'st-outcome'} role="status">
          <div>
            <strong>
              Converted “{converted.title}” — {converted.section}, {converted.parts} parts, {converted.questions} questions, as a draft.
            </strong>{' '}
            <Link to={`/staff/papers/${converted.id}`}>Check it now</Link>
            {converted.checks.length > 0 && (
              <ul>
                {converted.checks.map((check) => (
                  <li key={check}>{check}</li>
                ))}
              </ul>
            )}
          </div>
          <button type="button" className="st-link" onClick={() => setConverted(null)}>
            Dismiss
          </button>
        </div>
      )}
      {imported && (
        <div className="st-outcome" role="status">
          <strong>
            Imported “{imported.title}” — {imported.section}, {imported.parts} parts, {imported.questions} questions, as a draft.
          </strong>
          <button type="button" className="st-link" onClick={() => setImported(null)}>
            Dismiss
          </button>
        </div>
      )}
      <ErrorBox error={importError ?? error} />
      {sent && <RequestSent request={sent} onDismiss={() => setSent(null)} />}
      {papers && papers.length === 0 && <p className="st-empty">No papers yet. Import an exam package to begin.</p>}
      {papers && papers.length > 0 && <PaperTable papers={papers} onPublish={(p) => void act(() => adminApi.publishPaper(p.id))} onDelete={(p) => void act(async () => setSent(await adminApi.deletePaper(p.id)))} />}
    </>
  )
}

function PaperTable({ papers, onPublish, onDelete }: { papers: PaperListItem[]; onPublish: (p: PaperListItem) => void; onDelete: (p: PaperListItem) => void }) {
  return (
    <table className="st-table">
      <thead>
        <tr>
          <th>Paper</th>
          <th>Section</th>
          <th>Imported</th>
          <th className="st-num">Parts</th>
          <th className="st-num">Questions</th>
          <th className="st-num">Used by</th>
          <th>Status</th>
          <th />
        </tr>
      </thead>
      <tbody>
        {papers.map((paper) => (
          <tr key={paper.id}>
            <td>
              <Link to={`/staff/papers/${paper.id}`} className="st-strong-link">
                {paper.title}
              </Link>
              <div className="st-faint">{paper.examType}</div>
            </td>
            <td className="st-nowrap">{paper.section}</td>
            <td className="st-nowrap">{new Date(paper.createdAt).toLocaleString('en-GB', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })}</td>
            <td className="st-num">{paper.parts}</td>
            <td className="st-num">{paper.questions}</td>
            <td className="st-num">{paper.usedByExams > 0 ? `${paper.usedByExams} exam${paper.usedByExams === 1 ? '' : 's'}` : '–'}</td>
            <td>
              <Badge value={paper.status} />
            </td>
            <td className="st-row-actions">
              {paper.status === 'Draft' && (
                <button type="button" className="st-link" onClick={() => onPublish(paper)}>
                  Publish
                </button>
              )}
              {paper.usedByExams === 0 && <ConfirmButton className="st-link st-link--danger" label="Request deletion" confirmLabel="Ask an administrator to delete" onConfirm={() => onDelete(paper)} />}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}

/** The PDFs go to the server, which converts and imports them; nothing is created when it finds errors. */
function ConvertDialog({ onClose, onConverted }: { onClose: () => void; onConverted: (result: PaperConversionResult) => void }) {
  const [title, setTitle] = useState('')
  const [questions, setQuestions] = useState<File | null>(null)
  const [answerKey, setAnswerKey] = useState<File | null>(null)
  const [audio, setAudio] = useState<(File | null)[]>(PARTS.map(() => null))
  const [error, setError] = useState<unknown>(null)
  const [isBusy, setIsBusy] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (!questions || !answerKey) {
      return
    }

    setIsBusy(true)
    setError(null)
    try {
      onConverted(await adminApi.convertPaper({ title: title.trim(), questions, answerKey, audio }))
    } catch (e) {
      setError(e)
    } finally {
      setIsBusy(false)
    }
  }

  return (
    <Modal title="Import a paper from PDF" onClose={onClose}>
      <form className="st-form" onSubmit={submit}>
        <label className="st-field">
          Paper title
          <input value={title} onChange={(e) => setTitle(e.target.value)} required maxLength={256} placeholder="A2 Key for Schools – Test 2 – Listening" />
        </label>
        <div className="st-grid-2">
          <label className="st-field">
            Question paper (PDF)
            <input type="file" accept=".pdf,application/pdf" required onChange={(e) => setQuestions(e.target.files?.[0] ?? null)} />
          </label>
          <label className="st-field">
            Answer key (PDF)
            <input type="file" accept=".pdf,application/pdf" required onChange={(e) => setAnswerKey(e.target.files?.[0] ?? null)} />
          </label>
        </div>
        <fieldset className="st-fieldset">
          <legend>Listening only: one recording per part</legend>
          <div className="st-grid-2">
            {PARTS.map((part, index) => (
              <label key={part} className="st-field">
                Part {part}
                <input
                  type="file"
                  accept=".mp3,.m4a,.ogg,.wav,audio/*"
                  onChange={(e) => {
                    const file = e.target.files?.[0] ?? null
                    setAudio((current) => current.map((old, i) => (i === index ? file : old)))
                  }}
                />
              </label>
            ))}
          </div>
        </fieldset>
        <p className="st-faint">
          Reading &amp; Writing or Listening is read from the cover page. The paper is imported as a draft for you to check; nothing is saved if the PDF cannot be read.
        </p>
        <ErrorBox error={error} />
        <div className="st-form-actions">
          <button type="submit" className={isBusy ? 'st-btn st-btn--primary is-busy' : 'st-btn st-btn--primary'} disabled={isBusy}>
            {isBusy ? 'Converting… (up to a minute)' : 'Convert and import'}
          </button>
        </div>
      </form>
    </Modal>
  )
}

/**
 * VE-25: a paper written entirely on the web. The draft has the exam's parts, question types and numbering; every
 * text, picture and recording still to write is marked, and publishing refuses the paper until none is left.
 */
function BlankPaperDialog({ onClose }: { onClose: () => void }) {
  const { data: layouts, error: loadError } = useLoad(adminApi.blankLayouts, 'blank-layouts')
  const [sectionTemplateId, setSectionTemplateId] = useState<number | null>(null)
  const [title, setTitle] = useState('')
  const [error, setError] = useState<unknown>(null)
  const [isBusy, setIsBusy] = useState(false)
  const chosen = sectionTemplateId ?? layouts?.[0]?.sectionTemplateId ?? null

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (chosen === null) {
      return
    }

    setIsBusy(true)
    setError(null)
    try {
      const created = await adminApi.createBlankPaper(chosen, title.trim())
      navigate(`/staff/papers/${created.id}`)
    } catch (e) {
      setError(e)
      setIsBusy(false)
    }
  }

  return (
    <Modal title="New blank paper" onClose={onClose}>
      <form className="st-form" onSubmit={submit}>
        <p className="st-faint">
          The draft has the exam's parts, question types and numbering. Write every text marked “[to write…]”, upload the pictures and recordings and set the answers on the paper's page; it can be published once nothing is left to write.
        </p>
        <ErrorBox error={loadError ?? error} />
        <label className="st-field">
          Section
          <select value={chosen ?? ''} onChange={(e) => setSectionTemplateId(Number(e.target.value))} required>
            {(layouts ?? []).map((layout) => (
              <option key={layout.sectionTemplateId} value={layout.sectionTemplateId}>
                {layout.examType} — {layout.section}
              </option>
            ))}
          </select>
        </label>
        <label className="st-field">
          Title
          <input value={title} onChange={(e) => setTitle(e.target.value)} required maxLength={256} placeholder="A2 Key for Schools – Reading & Writing – November" />
        </label>
        <div className="st-form-actions">
          <button type="submit" className="st-btn st-btn--primary" disabled={isBusy || chosen === null}>
            Create draft
          </button>
        </div>
      </form>
    </Modal>
  )
}
