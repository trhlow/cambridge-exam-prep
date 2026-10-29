import { useRef, useState, type FormEvent } from 'react'
import { adminApi, type ApprovalView, type PaperConversionResult, type PaperImportSummary, type PaperListItem, type PaperMixView } from '../api'
import { navigate } from '../router'
import { Link } from '../Link'
import { Badge, ConfirmButton, ErrorBox, Modal, PageHeader, RequestSent, VersionBadge } from '../ui'
import { useLoad } from '../useLoad'
import { patternText } from '../format'
import { MAX_SOURCES, MIN_SOURCES, MIN_VERSIONS, maxVersions, sourceLetter } from '../mixing'

const PARTS = [1, 2, 3, 4, 5]

/**
 * The paper library. A paper comes in from Victory's PDFs (converted on the server) or as a ready package (.zip);
 * either way it arrives as a draft, is checked on its own page, then published so it can be scheduled. A paper used
 * by an exam cannot be deleted; deleting any other waits for a second administrator (VE-60). 2–6 published papers of
 * the same layout can be mixed into 4–3N papers, given by seat so neighbours never share one (Victory 20/10 and 29/10,
 * VE-67, VE-70).
 */
export function PapersPage() {
  const { data: papers, error, reload: reloadPapers } = useLoad(adminApi.papers, 'papers')
  const { data: mixes, error: mixesError, reload: reloadMixes } = useLoad(adminApi.paperMixes, 'paper-mixes')
  const reload = () => {
    reloadPapers()
    reloadMixes()
  }
  const [isMixing, setIsMixing] = useState(false)
  const [mixed, setMixed] = useState<PaperMixView | null>(null)
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
            <button type="button" className="st-btn" onClick={() => setIsMixing(true)}>
              Mix papers
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
      {isMixing && papers && (
        <MixDialog
          papers={papers}
          onClose={() => setIsMixing(false)}
          onMixed={(mix) => {
            setIsMixing(false)
            setMixed(mix)
            reload()
          }}
        />
      )}
      {mixed && (
        <div className="st-outcome" role="status">
          <strong>
            Mixed {mixed.sources.length} papers into {mixed.versionCount} published papers: “{mixed.title}”.
          </strong>{' '}
          Choose it for a section when creating an exam.
          <button type="button" className="st-link" onClick={() => setMixed(null)}>
            Dismiss
          </button>
        </div>
      )}
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
      <ErrorBox error={importError ?? error ?? mixesError} />
      {sent && <RequestSent request={sent} onDismiss={() => setSent(null)} />}
      {papers && papers.length === 0 && <p className="st-empty">No papers yet. Import an exam package to begin.</p>}
      {papers && papers.length > 0 && <PaperTable papers={papers.filter((p) => !p.paperMixId)} onPublish={(p) => void act(() => adminApi.publishPaper(p.id))} onDelete={(p) => void act(async () => setSent(await adminApi.deletePaper(p.id)))} />}
      {mixes && mixes.length > 0 && <MixTable mixes={mixes} />}
    </>
  )
}

/** VE-65, VE-70: each mix with its sources and its papers; a pattern reads part by part (A = first source). */
function MixTable({ mixes }: { mixes: PaperMixView[] }) {
  return (
    <>
      <h2 className="st-h2">Mixed papers</h2>
      <p className="st-faint">Each version takes every part whole from one source (A, B, C…). Versions of one group share no part and sit side by side in the room.</p>
      <table className="st-table">
        <thead>
          <tr>
            <th>Mix</th>
            <th>Section</th>
            <th>Sources</th>
            <th>Versions</th>
          </tr>
        </thead>
        <tbody>
          {mixes.map((mix) => (
            <tr key={mix.id}>
              <td>
                <strong>{mix.title}</strong>
                <div className="st-faint">
                  {mix.examType} · {mix.versionCount} papers
                </div>
              </td>
              <td className="st-nowrap">{mix.section}</td>
              <td>
                {mix.sources.map((source) => (
                  <div key={source.letter}>
                    {source.letter}: <Link to={`/staff/papers/${source.id}`}>{source.title}</Link>
                  </div>
                ))}
              </td>
              <td>
                {mix.versions.map((v) => (
                  <div key={v.version}>
                    <Link to={`/staff/papers/${v.paperId}`} aria-label={`Version ${v.version}`}>
                      <VersionBadge version={v.version} />
                    </Link>{' '}
                    <span className="st-faint st-mono" title={patternText(v.pattern)}>
                      {v.pattern}
                    </span>
                  </div>
                ))}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </>
  )
}

/**
 * VE-70 (Victory 29/10): 2–6 published papers of one section and layout become as many papers as the papers manager
 * asks for, from 4 up to three per source (for example 4 sources, 8 papers for a room of 30). Ticking order gives the
 * letters A, B, C… The server checks the layout part by part and lists every difference when they do not fit.
 */
function MixDialog({ papers, onClose, onMixed }: { papers: PaperListItem[]; onClose: () => void; onMixed: (mix: PaperMixView) => void }) {
  const published = papers.filter((p) => p.status === 'Published' && !p.paperMixId)
  const sections = [...new Map(published.map((p) => [p.sectionTemplateId, `${p.examType} — ${p.section}`])).entries()]
  const [sectionId, setSectionId] = useState<number | null>(sections.length === 1 ? sections[0][0] : null)
  const [chosen, setChosen] = useState<number[]>([])
  const [count, setCount] = useState(MIN_VERSIONS)
  const [title, setTitle] = useState('')
  const [error, setError] = useState<unknown>(null)
  const [isBusy, setIsBusy] = useState(false)
  const choices = published.filter((p) => p.sectionTemplateId === sectionId)
  const parts = Math.min(16, ...choices.filter((p) => chosen.includes(p.id)).map((p) => p.parts))
  const max = chosen.length >= MIN_SOURCES ? maxVersions(chosen.length, parts) : 0
  const fits = chosen.length >= MIN_SOURCES && count >= MIN_VERSIONS && count <= max

  const toggle = (paperId: number) =>
    setChosen((now) => (now.includes(paperId) ? now.filter((id) => id !== paperId) : now.length < MAX_SOURCES ? [...now, paperId] : now))

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (!fits) {
      return
    }

    setIsBusy(true)
    setError(null)
    try {
      onMixed(await adminApi.mixPapers(chosen, count, title.trim() || null))
    } catch (e) {
      setError(e)
      setIsBusy(false)
    }
  }

  return (
    <Modal title="Mix papers" onClose={onClose}>
      <form className="st-form" onSubmit={submit}>
        <p className="st-faint">
          Choose {MIN_SOURCES} to {MAX_SOURCES} published papers of the same section with the same parts and questions, then how many papers to make: at least {MIN_VERSIONS}, at most three per source paper. Each new paper takes every
          part whole from one source; an exam section then gives them by seat, so neighbouring computers never get the same paper.
        </p>
        <label className="st-field">
          Section
          <select
            value={sectionId ?? ''}
            onChange={(e) => {
              setSectionId(e.target.value ? Number(e.target.value) : null)
              setChosen([])
            }}
            required
          >
            <option value="">Choose a section…</option>
            {sections.map(([id, name]) => (
              <option key={id} value={id}>
                {name}
              </option>
            ))}
          </select>
        </label>
        {sectionId !== null && (
          <fieldset className="st-field mix-sources">
            <legend>
              Source papers <span className="st-faint">(ticking order gives A, B, C…)</span>
            </legend>
            {choices.map((p) => {
              const index = chosen.indexOf(p.id)
              return (
                <label key={p.id} className="mix-source">
                  <input type="checkbox" checked={index >= 0} onChange={() => toggle(p.id)} disabled={index < 0 && chosen.length >= MAX_SOURCES} />
                  <span className="mix-letter">{index >= 0 ? sourceLetter(index) : ''}</span>
                  {p.title}
                </label>
              )
            })}
          </fieldset>
        )}
        <label className="st-field">
          <span>
            Papers to make <span className="st-faint">{max > 0 ? `(${MIN_VERSIONS} to ${max})` : '(choose two papers first)'}</span>
          </span>
          <input type="number" min={MIN_VERSIONS} max={Math.max(max, MIN_VERSIONS)} value={count} onChange={(e) => setCount(Number(e.target.value))} disabled={max === 0} required />
        </label>
        <label className="st-field">
          <span>
            Title <span className="st-faint">(optional)</span>
          </span>
          <input value={title} onChange={(e) => setTitle(e.target.value)} maxLength={200} placeholder="Leave empty for “A × B × …”" />
        </label>
        <ErrorBox error={error} />
        <div className="st-form-actions">
          <button type="submit" className="st-btn st-btn--primary" disabled={isBusy || !fits}>
            {fits ? `Make ${count} papers` : 'Make papers'}
          </button>
        </div>
      </form>
    </Modal>
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
              <div className="st-faint">
                {paper.examType}
                {paper.mixVersion ? ` · mixed, version ${paper.mixVersion}` : ''}
              </div>
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
              {paper.usedByExams === 0 && !paper.paperMixId && <ConfirmButton className="st-link st-link--danger" label="Request deletion" confirmLabel="Ask an administrator to delete" onConfirm={() => onDelete(paper)} />}
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
    if (!questions) {
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
            Answer key (PDF, none for B1 Writing)
            <input type="file" accept=".pdf,application/pdf" onChange={(e) => setAnswerKey(e.target.files?.[0] ?? null)} />
          </label>
        </div>
        <fieldset className="st-fieldset">
          <legend>Listening only: one recording per part (A2 Key 5, B1 Preliminary 4)</legend>
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
          The level (A2 Key or B1 Preliminary) and the paper (Reading, Writing, Listening) are read from the cover page. The paper is imported as a draft for you to check; nothing is saved if the PDF cannot be read.
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
