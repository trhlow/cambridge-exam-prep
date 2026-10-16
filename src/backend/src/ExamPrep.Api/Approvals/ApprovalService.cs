using System.Security.Claims;
using ExamPrep.Api.Auth;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Approvals;

/// <summary>A request as the staff portal shows it, with who asked and who decided.</summary>
public sealed record ApprovalView(
    int Id,
    string Kind,
    int? TargetId,
    string Summary,
    string Status,
    int RequestedById,
    string RequestedBy,
    DateTime RequestedAt,
    int? DecidedById,
    string? DecidedBy,
    DateTime? DecidedAt,
    string? DecisionNote,
    string? Outcome);

/// <summary>
/// VE-60: actions that wait for a second person. Anyone allowed to start an action files the request; an
/// administrator other than the requester approves or rejects it (a manager's request needs one administrator, an
/// administrator's request the other one); the requester may cancel it while it waits.
/// </summary>
public sealed class ApprovalService(ExamPrepDbContext db, TimeProvider clock, ILogger<ApprovalService> logger)
{
    /// <summary>Files a request, unless the same action is already waiting (then returns the sentence to show).</summary>
    public async Task<(ApprovalView? Request, string? Conflict)> RequestAsync(
        ApprovalKind kind, int? targetId, string summary, string? payloadJson, int requesterId, CancellationToken cancellationToken)
    {
        if (targetId is not null && await db.ApprovalRequests.AnyAsync(
                r => r.Status == ApprovalStatus.Pending && r.Kind == kind && r.TargetId == targetId, cancellationToken))
        {
            return (null, "The same request is already waiting for approval.");
        }

        var request = new ApprovalRequest
        {
            Kind = kind,
            TargetId = targetId,
            Summary = summary.Length > 512 ? summary[..512] : summary,
            PayloadJson = payloadJson,
            RequestedByUserId = requesterId,
            RequestedAt = clock.GetUtcNow().UtcDateTime,
        };
        db.ApprovalRequests.Add(request);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Staff {UserId} requested {Kind} of {TargetId} (approval {RequestId})", requesterId, kind, targetId, request.Id);
        return (await ViewAsync(request.Id, cancellationToken), null);
    }

    public async Task<IReadOnlyList<ApprovalView>> ListAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var requests = db.ApprovalRequests.AsNoTracking();
        if (!user.IsInRole(nameof(UserRole.Admin)))
        {
            var userId = user.GetStaffId();
            requests = requests.Where(r => r.RequestedByUserId == userId);
        }

        return await Project(requests.OrderByDescending(r => r.Id).Take(500)).ToListAsync(cancellationToken);
    }

    /// <summary>Approves (and carries out) or rejects a waiting request. Status codes: 404, 403 (own request), 409 (decided).</summary>
    public async Task<(int Status, string? Error, ApprovalView? Request)> DecideAsync(
        int requestId, ClaimsPrincipal approver, bool approve, string? note, CancellationToken cancellationToken)
    {
        var request = await db.ApprovalRequests.SingleOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (request is null)
        {
            return (StatusCodes.Status404NotFound, null, null);
        }

        var approverId = approver.GetStaffId();
        if (request.RequestedByUserId == approverId)
        {
            return (StatusCodes.Status403Forbidden, "A request must be decided by another administrator.", null);
        }

        if (request.Status != ApprovalStatus.Pending)
        {
            return (StatusCodes.Status409Conflict, $"This request is already {request.Status.ToString().ToLowerInvariant()}.", null);
        }

        request.DecidedByUserId = approverId;
        request.DecidedAt = clock.GetUtcNow().UtcDateTime;
        if (approve)
        {
            var problem = await ApprovalActions.ExecuteAsync(db, request, cancellationToken);
            request.Status = problem is null ? ApprovalStatus.Approved : ApprovalStatus.Failed;
            request.Outcome = problem;
        }
        else
        {
            request.Status = ApprovalStatus.Rejected;
            request.DecisionNote = note;
        }

        ApprovalActions.ForgetPassword(request);

        // The action and the decision are saved together: both happen or neither.
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return (StatusCodes.Status409Conflict, "Another administrator decided this request at the same moment.", null);
        }

        logger.LogInformation("Staff {UserId} {Decision} approval {RequestId} ({Kind} of {TargetId})", approverId, request.Status, requestId, request.Kind, request.TargetId);
        return (StatusCodes.Status200OK, null, await ViewAsync(requestId, cancellationToken));
    }

    public async Task<(int Status, string? Error, ApprovalView? Request)> CancelAsync(int requestId, int userId, CancellationToken cancellationToken)
    {
        var request = await db.ApprovalRequests.SingleOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (request is null)
        {
            return (StatusCodes.Status404NotFound, null, null);
        }

        if (request.RequestedByUserId != userId)
        {
            return (StatusCodes.Status403Forbidden, "Only the person who made a request can cancel it.", null);
        }

        if (request.Status != ApprovalStatus.Pending)
        {
            return (StatusCodes.Status409Conflict, $"This request is already {request.Status.ToString().ToLowerInvariant()}.", null);
        }

        request.Status = ApprovalStatus.Cancelled;
        request.DecidedByUserId = userId;
        request.DecidedAt = clock.GetUtcNow().UtcDateTime;
        ApprovalActions.ForgetPassword(request);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return (StatusCodes.Status409Conflict, "This request was decided at the same moment.", null);
        }

        return (StatusCodes.Status200OK, null, await ViewAsync(requestId, cancellationToken));
    }

    public async Task<ApprovalView> ViewAsync(int requestId, CancellationToken cancellationToken) =>
        await Project(db.ApprovalRequests.AsNoTracking().Where(r => r.Id == requestId)).SingleAsync(cancellationToken);

    private static IQueryable<ApprovalView> Project(IQueryable<ApprovalRequest> requests) =>
        requests.Select(r => new ApprovalView(
            r.Id,
            r.Kind.ToString(),
            r.TargetId,
            r.Summary,
            r.Status.ToString(),
            r.RequestedByUserId,
            r.RequestedBy!.FullName,
            r.RequestedAt,
            r.DecidedByUserId,
            r.DecidedBy != null ? r.DecidedBy.FullName : null,
            r.DecidedAt,
            r.DecisionNote,
            r.Outcome));
}
