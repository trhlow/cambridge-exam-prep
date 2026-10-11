using ExamPrep.Api.Admin;
using ExamPrep.Api.Auth;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Exams;

/// <summary>
/// Exam sessions as staff see them (Inspera "parent test"): the list, the session with its PIN and invigilator
/// password, and the candidates' login slips. Invigilators only reach the sessions they are assigned to.
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
        return exam is null ? NotFound() : exam;
    }

    /// <summary>Username and password of every candidate, for printing (again) on the day.</summary>
    [HttpGet("{examId:int}/login-slips")]
    public async Task<ActionResult<IReadOnlyList<LoginSlip>>> LoginSlips(int examId, CancellationToken cancellationToken)
    {
        var exam = await db.Exams.VisibleTo(User).AsNoTracking().SingleOrDefaultAsync(e => e.Id == examId, cancellationToken);
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
