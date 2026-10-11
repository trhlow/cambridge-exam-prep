import { staffApi } from '../api'
import { Link } from '../Link'
import { Badge, ErrorBox, PageHeader } from '../ui'
import { formatDate } from '../format'
import { useLoad } from '../useLoad'

/** Every session the staff member may open (invigilators: the ones assigned to them), newest first. */
export function ExamsPage({ isAdmin }: { isAdmin: boolean }) {
  const { data: exams, error } = useLoad(staffApi.exams, 'exams')

  return (
    <>
      <PageHeader
        title="Exams"
        meta={isAdmin ? 'Exam sessions at the centre' : 'Sessions you are invigilating'}
        actions={
          isAdmin && (
            <Link to="/staff/exams/new" className="st-btn st-btn--primary">
              New exam
            </Link>
          )
        }
      />
      <ErrorBox error={error} />
      {exams && exams.length === 0 && (
        <p className="st-empty">{isAdmin ? 'No exams yet. Import and publish the papers, then create an exam.' : 'You are not assigned to any exam yet.'}</p>
      )}
      {exams && exams.length > 0 && (
        <table className="st-table">
          <thead>
            <tr>
              <th>Exam</th>
              <th>Date</th>
              <th>Room</th>
              <th>Sections</th>
              <th className="st-num">Candidates</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {exams.map((exam) => (
              <tr key={exam.id}>
                <td>
                  <Link to={`/staff/exams/${exam.id}`} className="st-strong-link">
                    {exam.name}
                  </Link>
                  <div className="st-faint">{exam.examType}</div>
                </td>
                <td>{formatDate(exam.scheduledDate)}</td>
                <td>{exam.room ?? '–'}</td>
                <td>{exam.sections.map((s) => s.title).join(', ')}</td>
                <td className="st-num">{exam.candidates}</td>
                <td>
                  <Badge value={exam.status} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  )
}
