import { useRef, useState } from 'react'
import { adminApi, type PaperImportSummary, type PaperListItem } from '../api'
import { Badge, ConfirmButton, ErrorBox, PageHeader } from '../ui'
import { useLoad } from '../useLoad'

/**
 * The paper library. A package (.zip from setup/exam-package, or made by pdf2package.py) is imported as a draft;
 * once checked it is published and can be scheduled. A paper used by an exam cannot be deleted.
 */
export function PapersPage() {
  const { data: papers, error, reload } = useLoad(adminApi.papers, 'papers')
  const [importError, setImportError] = useState<unknown>(null)
  const [imported, setImported] = useState<PaperImportSummary | null>(null)
  const [isImporting, setIsImporting] = useState(false)
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
          <label className={isImporting ? 'st-btn st-btn--primary is-busy' : 'st-btn st-btn--primary'}>
            {isImporting ? 'Importing…' : 'Import package (.zip)'}
            <input ref={fileRef} type="file" accept=".zip,application/zip" className="visually-hidden" onChange={(e) => void upload(e.target.files?.[0])} disabled={isImporting} />
          </label>
        }
      />
      <div className="st-steps">
        <span>1. Convert the PDF or prepare the package (setup/exam-package)</span>
        <span>2. Import the .zip here: it arrives as a draft</span>
        <span>3. Check it, then publish</span>
        <span>4. Choose it when creating an exam</span>
      </div>
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
      {papers && papers.length === 0 && <p className="st-empty">No papers yet. Import an exam package to begin.</p>}
      {papers && papers.length > 0 && <PaperTable papers={papers} onPublish={(p) => void act(() => adminApi.publishPaper(p.id))} onDelete={(p) => void act(() => adminApi.deletePaper(p.id))} />}
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
              <strong>{paper.title}</strong>
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
              {paper.usedByExams === 0 && <ConfirmButton className="st-link st-link--danger" label="Delete" confirmLabel="Confirm delete" onConfirm={() => onDelete(paper)} />}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
