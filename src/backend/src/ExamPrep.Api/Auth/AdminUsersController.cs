using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Auth;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = StaffAuthDefaults.AdminPolicy)]
public sealed class AdminUsersController(ExamPrepDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<StaffProfile>> List(CancellationToken cancellationToken)
    {
        var users = await db.Users.AsNoTracking().OrderBy(u => u.Username).ToListAsync(cancellationToken);
        return users.Select(StaffProfile.From).ToList();
    }
}
