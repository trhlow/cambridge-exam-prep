using ExamPrep.Api.Admin;
using ExamPrep.Api.Auth;
using ExamPrep.Api.Content;
using ExamPrep.Api.Delivery;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Exams;

/// <param name="Seat">The computer: the seat number staff typed, or the candidate's place in the list.</param>
/// <param name="SeatTyped">False when the seat comes from the list order because no seat number was entered.</param>
/// <param name="PaperVersion">The mixed-paper version this computer gets; null when no section is mixed.</param>
public sealed record RoomSeat(int Seat, int CandidateId, string CandidateNumber, string FullName, bool SeatTyped, int? PaperVersion);

/// <summary>A section of the session and, when it gives mixed papers, its mix with the four versions.</summary>
public sealed record RoomPlanSection(int SectionId, string Title, string PaperTitle, string? PaperMixTitle, IReadOnlyList<PaperMixVersionView> Versions);

/// <summary>VE-67: the room as a grid of computers with each candidate and the version of the paper they get, for printing.</summary>
public sealed record RoomPlanView(int ExamId, string ExamName, DateOnly ScheduledDate, string? Room, int? SeatsPerRow, IReadOnlyList<RoomSeat> Seats, IReadOnlyList<RoomPlanSection> Sections);

/// <summary>
/// Exam sessions as staff see them (Inspera "parent test"): the list, the session with its PIN and invigilator
/// password, and the candidates' login slips. Invigilators and examiners only reach the sessions they are assigned
/// to (see <see cref="ExamAccess"/>).
/// </summary>
[ApiController]
[Route("api/staff/exams")]
[Authorize(Policy = StaffAuthDefaults.StaffPolicy)]
public sealed class StaffExamsController(ExamPrepDbContext db, CandidateCredentials credentials) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ExamListItem>> List(CancellationToken cancellationToken) =>
        ExamViews.ListAsync(db, db.Exams.VisibleTo(User), cancellationToken);

    [HttpGet("{examId:int}")]
    public async Task<ActionResult<ExamDetail>> Get(int examId, CancellationToken cancellationToken)
    {
        var exam = await ExamViews.DetailAsync(db, db.Exams.VisibleTo(User), examId, cancellationToken);
        if (exam is null)
        {
            return NotFound();
        }

        // The PIN and the invigilator password are for the people who run the room, not for examiners.
        var runsTheRoom = await db.Exams.SlipsVisibleTo(User).AnyAsync(e => e.Id == examId, cancellationToken);
        return runsTheRoom ? exam : exam with { SessionPin = string.Empty, InvigilatorPassword = string.Empty, SebConfigKey = null };
    }

    /// <summary>
    /// The seating plan with the paper version of every computer (VE-67): for the people who prepare or run the room.
    /// Neighbouring computers never share a version (2×2 blocks over the seat numbers).
    /// </summary>
    [HttpGet("{examId:int}/room-plan")]
    public async Task<ActionResult<RoomPlanView>> RoomPlan(int examId, CancellationToken cancellationToken)
    {
        var exam = await db.Exams.SlipsVisibleTo(User).AsNoTracking().SingleOrDefaultAsync(e => e.Id == examId, cancellationToken);
        if (exam is null)
        {
            return NotFound();
        }

        var candidates = await db.Candidates.AsNoTracking().Where(c => c.ExamId == examId).ToListAsync(cancellationToken);
        var sections = await db.ExamSections.AsNoTracking()
            .Where(s => s.ExamId == examId)
            .OrderBy(s => s.OrderNo)
            .Select(s => new { s.Id, s.SectionTemplate!.Title, PaperTitle = s.TestPaper!.Title, s.PaperMixId })
            .ToListAsync(cancellationToken);
        var mixes = await AdminPaperMixesController.ViewsAsync(db, null, cancellationToken);
        var isMixed = sections.Any(s => s.PaperMixId != null);
        var seats = PaperAssignment.SeatsOf(candidates);
        var versions = PaperAssignment.VersionsOf(candidates, exam.SeatsPerRow);

        var plan = candidates
            .Select(c => new RoomSeat(seats[c.Id], c.Id, c.CandidateNumber, $"{c.FirstName} {c.LastName}", Domain.Papers.SeatPattern.Parse(c.SeatNo) is not null, isMixed ? versions[c.Id] : null))
            .OrderBy(s => s.Seat).ThenBy(s => s.CandidateNumber)
            .ToList();
        var planSections = sections.Select(s =>
        {
            var mix = s.PaperMixId is { } id ? mixes.SingleOrDefault(m => m.Id == id) : null;
            return new RoomPlanSection(s.Id, s.Title, s.PaperTitle, mix?.Title, mix?.Versions ?? []);
        }).ToList();
        return new RoomPlanView(exam.Id, exam.Name, exam.ScheduledDate, exam.Room, exam.SeatsPerRow, plan, planSections);
    }

    /// <summary>Username and password of every candidate, for printing (again) on the day.</summary>
    [HttpGet("{examId:int}/login-slips")]
    public async Task<ActionResult<IReadOnlyList<LoginSlip>>> LoginSlips(int examId, CancellationToken cancellationToken)
    {
        var exam = await db.Exams.SlipsVisibleTo(User).AsNoTracking().SingleOrDefaultAsync(e => e.Id == examId, cancellationToken);
        if (exam is null)
        {
            return NotFound();
        }

        var candidates = await db.Candidates.AsNoTracking()
            .Where(c => c.ExamId == examId)
            .OrderBy(c => c.CandidateNumber)
            .ToListAsync(cancellationToken);
        return candidates
            .Select(c => new LoginSlip(c.Id, c.CandidateNumber, $"{c.FirstName} {c.LastName}", c.Username, credentials.Reveal(c), exam.Name, exam.ScheduledDate, exam.Room))
            .ToList();
    }
}
