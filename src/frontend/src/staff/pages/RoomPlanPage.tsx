import { staffApi, type RoomSeat } from '../api'
import { ErrorBox } from '../ui'
import { formatDate, patternText } from '../format'
import { useLoad } from '../useLoad'
import { VictoryLogo } from '../../ui/VictoryLogo'

/**
 * VE-67: the seating plan the invigilator prints and checks against the room. Computers are laid out row by row
 * (seat numbers, computers per row from the exam); each shows its candidate and the version of the mixed papers it
 * gets, so neighbours can be seen to differ. Printed on its own, like the login slips.
 */
export function RoomPlanPage({ examId }: { examId: number }) {
  const { data: plan, error } = useLoad(() => staffApi.roomPlan(examId), examId)

  if (!plan) {
    return error ? <ErrorBox error={error} /> : <p className="st-empty">Loading…</p>
  }

  const lastSeat = Math.max(0, ...plan.seats.map((s) => s.seat))
  const perRow = plan.seatsPerRow ?? Math.max(1, lastSeat)
  const bySeat = new Map<number, RoomSeat[]>()
  plan.seats.forEach((seat) => bySeat.set(seat.seat, [...(bySeat.get(seat.seat) ?? []), seat]))
  const shared = [...bySeat.entries()].filter(([, seats]) => seats.length > 1).map(([seat]) => seat)
  const mixed = plan.sections.filter((s) => s.paperMixTitle)
  const untyped = plan.seats.filter((s) => !s.seatTyped).length

  return (
    <div className="slips room-plan">
      <div className="slips-bar">
        <span>
          {plan.seats.length} candidate{plan.seats.length === 1 ? '' : 's'} · {plan.seatsPerRow ? `${plan.seatsPerRow} computers per row` : 'one row'}
        </span>
        <button type="button" className="st-btn st-btn--primary" onClick={() => window.print()}>
          Print
        </button>
      </div>
      <header className="room-plan-head">
        <VictoryLogo />
        <div>
          <h1>Room plan — {plan.examName}</h1>
          <div>
            {formatDate(plan.scheduledDate)}
            {plan.room && ` · ${plan.room}`}
          </div>
        </div>
      </header>

      {mixed.length === 0 && <p className="st-note">No section of this exam gives mixed papers: every computer gets the same paper.</p>}
      {mixed.map((section) => (
        <section key={section.sectionId} className="room-plan-legend">
          <strong>{section.title}</strong> · {section.paperMixTitle}
          <ul>
            {section.versions.map((v) => (
              <li key={v.version}>
                <span className={`room-version room-version--${v.version}`}>V{v.version}</span> {patternText(v.pattern)}
              </li>
            ))}
          </ul>
        </section>
      ))}
      {untyped > 0 && (
        <p className="st-note">
          {untyped} candidate{untyped === 1 ? ' has' : 's have'} no seat number: their computer is their place in the list (by candidate number). Enter seat numbers on the exam page to fix the plan.
        </p>
      )}
      {shared.length > 0 && <p className="st-note">Seat {shared.join(', ')} is given to more than one candidate.</p>}

      <div className="room-grid" style={{ gridTemplateColumns: `repeat(${perRow}, minmax(120px, 1fr))` }} role="list" aria-label="Computers">
        {Array.from({ length: lastSeat }, (_, index) => index + 1).map((seat) => {
          const seated = bySeat.get(seat) ?? []
          return (
            <div key={seat} className={seated.length === 0 ? 'room-seat is-empty' : 'room-seat'} role="listitem" aria-label={`Computer ${seat}`}>
              <div className="room-seat-top">
                <span className="room-seat-no">{seat}</span>
                {seated[0]?.paperVersion != null && <span className={`room-version room-version--${seated[0].paperVersion}`}>V{seated[0].paperVersion}</span>}
              </div>
              {seated.length === 0 && <span className="st-faint">empty</span>}
              {seated.map((s) => (
                <div key={s.candidateId}>
                  <strong>{s.fullName}</strong>
                  <div className="st-faint st-mono">{s.candidateNumber}</div>
                </div>
              ))}
            </div>
          )
        })}
      </div>
      <p className="st-faint room-plan-foot">Row 1 is at the top; seats run left to right, row by row. Neighbouring computers (side by side, front and back, diagonal) always have different versions.</p>
    </div>
  )
}
