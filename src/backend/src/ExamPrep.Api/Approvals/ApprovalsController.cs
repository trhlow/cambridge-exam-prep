using System.ComponentModel.DataAnnotations;
using ExamPrep.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamPrep.Api.Approvals;

public sealed record RejectRequest([Required, StringLength(500, MinimumLength = 1)] string Note);

/// <summary>
/// VE-60: the approval queue and log. Administrators see every request and decide the ones they did not make;
/// everyone else sees and may cancel their own.
/// </summary>
[ApiController]
[Route("api/staff/approvals")]
[Authorize(Policy = StaffAuthDefaults.StaffPolicy)]
public sealed class ApprovalsController(ApprovalService approvals) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ApprovalView>> List(CancellationToken cancellationToken) => approvals.ListAsync(User, cancellationToken);

    [HttpPost("{requestId:int}/approve")]
    [Authorize(Policy = StaffAuthDefaults.AdminPolicy)]
    public async Task<ActionResult<ApprovalView>> Approve(int requestId, CancellationToken cancellationToken) =>
        Respond(await approvals.DecideAsync(requestId, User, approve: true, note: null, cancellationToken));

    [HttpPost("{requestId:int}/reject")]
    [Authorize(Policy = StaffAuthDefaults.AdminPolicy)]
    public async Task<ActionResult<ApprovalView>> Reject(int requestId, RejectRequest request, CancellationToken cancellationToken) =>
        Respond(await approvals.DecideAsync(requestId, User, approve: false, request.Note.Trim(), cancellationToken));

    [HttpPost("{requestId:int}/cancel")]
    public async Task<ActionResult<ApprovalView>> Cancel(int requestId, CancellationToken cancellationToken) =>
        Respond(await approvals.CancelAsync(requestId, User.GetStaffId(), cancellationToken));

    private ActionResult<ApprovalView> Respond((int Status, string? Error, ApprovalView? Request) result) =>
        result.Request is not null
            ? result.Request
            : result.Error is null ? StatusCode(result.Status) : Problem(title: result.Error, statusCode: result.Status);
}
