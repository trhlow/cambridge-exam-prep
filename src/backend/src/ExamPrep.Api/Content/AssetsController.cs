using ExamPrep.Api.Auth;
using ExamPrep.Infrastructure.Content;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Content;

public static class AssetAccess
{
    /// <summary>Signed-in staff, or a candidate past the PIN; the controller narrows candidates to their exam's files.</summary>
    public const string Policy = "AssetReader";

    public static IServiceCollection AddAssetAccess(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<ExamAssetIndex>();
        services.AddAuthorizationBuilder()
            .AddPolicy(Policy, policy => policy
                .AddAuthenticationSchemes(StaffAuthDefaults.Scheme, CandidateAuthDefaults.Scheme)
                .RequireAuthenticatedUser());
        return services;
    }
}

/// <summary>Serves pictures and audio tracks of exam papers.</summary>
[ApiController]
[Route("api/assets")]
[Authorize(Policy = AssetAccess.Policy)]
public sealed class AssetsController(ExamPrepDbContext db, IAssetStorage storage, ExamAssetIndex examAssets) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        if (!await MayReadAsync(id, cancellationToken))
        {
            return NotFound();
        }

        var asset = await db.Assets.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (asset is null)
        {
            return NotFound();
        }

        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.CacheControl = "private, max-age=3600";
        return PhysicalFile(storage.ResolvePath(asset.StoredPath), asset.ContentType, enableRangeProcessing: true);
    }

    /// <summary>
    /// Administrators may read any file; other staff the files of the papers they made (VE-72) or of the sessions they
    /// mark; a candidate only the files of the paper of a section they are sitting, and only after the PIN.
    /// </summary>
    private async Task<bool> MayReadAsync(int assetId, CancellationToken cancellationToken)
    {
        if (User.Identities.Any(i => i.IsAuthenticated && i.AuthenticationType == StaffAuthDefaults.Scheme))
        {
            return await examAssets.StaffMayReadAsync(User, assetId, cancellationToken);
        }

        return User.IsPinVerified() && await examAssets.CandidateMayReadAsync(User.GetCandidateId(), assetId, cancellationToken);
    }
}
