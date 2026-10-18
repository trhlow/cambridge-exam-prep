using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using ExamPrep.Api.Auth;
using ExamPrep.Api.Common;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Content;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Centre;

/// <param name="LogoUrl">Null while the centre uses the built-in Victory logo; changes with every new upload.</param>
public sealed record SiteView(string CentreName, string? LogoUrl, DateTime UpdatedAt);

public sealed record UpdateSiteRequest([Required, StringLength(128, MinimumLength = 1)] string CentreName);

/// <summary>
/// VE-24: the centre's name and logo, shown on the sign-in screens, the exam header and the staff portal. Everyone
/// may read them (the sign-in screens come before any account); administrators and managers of the Centre area
/// change them. A logo is a PNG, so no script can ride along (SVG is refused).
/// </summary>
[ApiController]
public sealed class SiteController(ExamPrepDbContext db, IAssetStorage storage, TimeProvider clock, ILogger<SiteController> logger) : ControllerBase
{
    public const long MaxLogoBytes = 1024 * 1024;
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [HttpGet("api/site")]
    [AllowAnonymous]
    public async Task<SiteView> Get(CancellationToken cancellationToken) => View(await SettingsAsync(cancellationToken));

    /// <summary>The current logo; the URL carries the asset id, so a new logo is never served from a stale cache.</summary>
    [HttpGet("api/site/logo")]
    [AllowAnonymous]
    public async Task<IActionResult> Logo(CancellationToken cancellationToken)
    {
        var settings = await db.SiteSettings.AsNoTracking().Include(s => s.LogoAsset).SingleAsync(cancellationToken);
        if (settings.LogoAsset is not { } logo)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "public, max-age=86400";
        return PhysicalFile(storage.ResolvePath(logo.StoredPath), logo.ContentType);
    }

    [HttpPut("api/admin/site")]
    [Authorize(Policy = StaffAuthDefaults.CentrePolicy)]
    public async Task<SiteView> Update(UpdateSiteRequest request, CancellationToken cancellationToken)
    {
        var settings = await SettingsAsync(cancellationToken);
        settings.CentreName = request.CentreName.Trim();
        Touch(settings);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Staff {UserId} renamed the centre", User.GetStaffId());
        return View(settings);
    }

    [HttpPost("api/admin/site/logo")]
    [Authorize(Policy = StaffAuthDefaults.CentrePolicy)]
    [RequestSizeLimit(MaxLogoBytes + 64 * 1024)]
    public async Task<ActionResult<SiteView>> UploadLogo(IFormFile? logo, CancellationToken cancellationToken)
    {
        if (logo is null || logo.Length == 0)
        {
            return Problems.Invalid("Choose a PNG file for the logo.");
        }

        if (logo.Length > MaxLogoBytes)
        {
            return Problems.Invalid("The logo must be at most 1 MB.");
        }

        using var buffer = new MemoryStream();
        await logo.CopyToAsync(buffer, cancellationToken);
        var content = buffer.ToArray();
        if (!content.AsSpan().StartsWith(PngSignature))
        {
            return Problems.Invalid("The logo must be a PNG image.");
        }

        var sha = Convert.ToHexStringLower(SHA256.HashData(content));
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Sha256 == sha, cancellationToken);
        if (asset is null)
        {
            asset = new Asset
            {
                Kind = AssetKind.Image,
                OriginalFileName = Path.GetFileName(logo.FileName),
                StoredPath = await storage.SaveAsync(content, sha, ".png", cancellationToken),
                ContentType = "image/png",
                SizeBytes = content.Length,
                Sha256 = sha,
                UploadedByUserId = User.GetStaffId(),
                CreatedAt = clock.GetUtcNow().UtcDateTime,
            };
            db.Assets.Add(asset);
        }

        var settings = await SettingsAsync(cancellationToken);
        settings.LogoAsset = asset;
        Touch(settings);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Staff {UserId} uploaded a new centre logo (asset {AssetId})", User.GetStaffId(), asset.Id);
        return View(settings);
    }

    private Task<SiteSetting> SettingsAsync(CancellationToken cancellationToken) => db.SiteSettings.SingleAsync(cancellationToken);

    private void Touch(SiteSetting settings)
    {
        settings.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        settings.UpdatedByUserId = User.GetStaffId();
    }

    private static SiteView View(SiteSetting settings) =>
        new(settings.CentreName, settings.LogoAssetId is { } id ? $"/api/site/logo?v={id}" : null, settings.UpdatedAt);
}
