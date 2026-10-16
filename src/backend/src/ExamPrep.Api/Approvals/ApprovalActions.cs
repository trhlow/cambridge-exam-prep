using System.Text.Json;
using ExamPrep.Api.Exams;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Approvals;

/// <summary>Details of a staff account change waiting for approval; null fields stay as they are.</summary>
public sealed record StaffAccountChange(bool? IsActive, string? Role, IReadOnlyList<string>? Areas, string? PasswordHash);

/// <summary>A further administrator waiting for approval; the password only as its hash.</summary>
public sealed record NewAdministrator(string Username, string FullName, string? Email, string PasswordHash);

/// <summary>
/// What each kind of request checks and does. The checks run when the request is made (so an impossible one is
/// refused at once) and again when it is approved, because the situation may have changed in between.
/// </summary>
public static class ApprovalActions
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string Serialize<T>(T payload) => JsonSerializer.Serialize(payload, Json);

    /// <summary>Once decided, a request keeps what was asked but no longer the password hash it carried.</summary>
    public static void ForgetPassword(ApprovalRequest request)
    {
        if (request.PayloadJson is null)
        {
            return;
        }

        if (request.Kind == ApprovalKind.ChangeStaffAccount)
        {
            var change = JsonSerializer.Deserialize<StaffAccountChange>(request.PayloadJson, Json)!;
            if (change.PasswordHash is not null)
            {
                request.PayloadJson = Serialize(change with { PasswordHash = "(set)" });
            }
        }
        else if (request.Kind == ApprovalKind.CreateAdmin)
        {
            var admin = JsonSerializer.Deserialize<NewAdministrator>(request.PayloadJson, Json)!;
            request.PayloadJson = Serialize(admin with { PasswordHash = "(set)" });
        }
    }

    public static async Task<string?> PaperProblemAsync(ExamPrepDbContext db, int paperId, CancellationToken cancellationToken) =>
        await db.ExamSections.AnyAsync(s => s.TestPaperId == paperId, cancellationToken)
            ? "This paper is used by an exam and cannot be deleted."
            : null;

    public static async Task<string?> ExamProblemAsync(ExamPrepDbContext db, int examId, CancellationToken cancellationToken) =>
        await ExamViews.HasStartedAsync(db, examId, cancellationToken)
            ? "Candidates have already started this exam. It cannot be deleted."
            : null;

    public static async Task<string?> CandidateProblemAsync(ExamPrepDbContext db, int candidateId, CancellationToken cancellationToken) =>
        await db.Attempts.AnyAsync(a => a.CandidateId == candidateId, cancellationToken)
            ? "This candidate has already started the exam and cannot be removed."
            : null;

    /// <summary>Carries out an approved request; returns why it could not be done, or null when it was.</summary>
    public static async Task<string?> ExecuteAsync(ExamPrepDbContext db, ApprovalRequest request, CancellationToken cancellationToken)
    {
        var id = request.TargetId ?? 0;
        switch (request.Kind)
        {
            case ApprovalKind.DeletePaper:
            {
                var paper = await db.TestPapers.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
                if (paper is null)
                {
                    return "The paper no longer exists.";
                }

                var problem = await PaperProblemAsync(db, id, cancellationToken);
                if (problem is null)
                {
                    db.TestPapers.Remove(paper);
                }

                return problem;
            }

            case ApprovalKind.DeleteExam:
            {
                var exam = await db.Exams.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
                if (exam is null)
                {
                    return "The exam no longer exists.";
                }

                var problem = await ExamProblemAsync(db, id, cancellationToken);
                if (problem is null)
                {
                    db.Exams.Remove(exam);
                }

                return problem;
            }

            case ApprovalKind.DeleteCandidate:
            {
                var candidate = await db.Candidates.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
                if (candidate is null)
                {
                    return "The candidate no longer exists.";
                }

                var problem = await CandidateProblemAsync(db, id, cancellationToken);
                if (problem is null)
                {
                    db.Candidates.Remove(candidate);
                }

                return problem;
            }

            case ApprovalKind.ChangeStaffAccount:
                return await ChangeAccountAsync(db, id, JsonSerializer.Deserialize<StaffAccountChange>(request.PayloadJson!, Json)!, cancellationToken);

            case ApprovalKind.CreateAdmin:
            {
                var admin = JsonSerializer.Deserialize<NewAdministrator>(request.PayloadJson!, Json)!;
                if (await db.Users.AnyAsync(u => u.Username == admin.Username, cancellationToken))
                {
                    return $"The username {admin.Username} has been taken meanwhile.";
                }

                db.Users.Add(new User
                {
                    Username = admin.Username,
                    FullName = admin.FullName,
                    Email = admin.Email,
                    PasswordHash = admin.PasswordHash,
                    Role = UserRole.Admin,
                    CreatedAt = DateTime.UtcNow,
                });
                return null;
            }

            default:
                return $"Unknown request kind {request.Kind}.";
        }
    }

    private static async Task<string?> ChangeAccountAsync(ExamPrepDbContext db, int userId, StaffAccountChange change, CancellationToken cancellationToken)
    {
        var user = await db.Users.Include(u => u.Areas).SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return "The account no longer exists.";
        }

        var role = change.Role is null ? user.Role : Enum.Parse<UserRole>(change.Role);
        var isActive = change.IsActive ?? user.IsActive;
        var losesAdmin = user.Role == UserRole.Admin && user.IsActive && (role != UserRole.Admin || !isActive);
        if (losesAdmin && !await db.Users.AnyAsync(u => u.Id != userId && u.Role == UserRole.Admin && u.IsActive, cancellationToken))
        {
            return "This would leave no active administrator.";
        }

        user.Role = role;
        user.IsActive = isActive;
        if (change.PasswordHash is not null)
        {
            user.PasswordHash = change.PasswordHash;
        }

        if (role != UserRole.Manager)
        {
            user.Areas.Clear();
        }
        else if (change.Areas is not null)
        {
            user.Areas.Clear();
            foreach (var area in change.Areas.Select(Enum.Parse<ManagerArea>))
            {
                user.Areas.Add(new UserArea { Area = area });
            }
        }

        // The account signs in again with its new rights (or not at all).
        user.SecurityStamp = Guid.NewGuid();
        return null;
    }
}
