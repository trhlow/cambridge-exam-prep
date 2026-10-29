using System.ComponentModel.DataAnnotations;
using ExamPrep.Api.Auth;
using ExamPrep.Api.Common;
using ExamPrep.Infrastructure.Content;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Content;

/// <param name="SourcePaperIds">2–6 published papers of the same layout, in letter order (A first).</param>
/// <param name="VersionCount">How many papers to make: 4 to 3 × the number of sources.</param>
/// <param name="Seed">Optional: the same sources, count and seed give the same papers again.</param>
public sealed record NewPaperMix([Required] IReadOnlyList<int> SourcePaperIds, int VersionCount, [StringLength(PaperMixer.MaxTitleLength)] string? Title, int? Seed);

public sealed record PaperRef(int Id, string Title);

/// <param name="Letter">The letter the patterns use for this source: A, B, C, …</param>
public sealed record PaperMixSourceView(string Letter, int Id, string Title);

/// <param name="Pattern">The source of each part in part order, e.g. "ABBAABA".</param>
public sealed record PaperMixVersionView(int Version, int PaperId, string Title, string Pattern);

public sealed record PaperMixView(
    int Id,
    string Title,
    string ExamType,
    int SectionTemplateId,
    string Section,
    IReadOnlyList<PaperMixSourceView> Sources,
    int VersionCount,
    int Seed,
    DateTime CreatedAt,
    IReadOnlyList<PaperMixVersionView> Versions);

/// <summary>
/// Paper shuffling (Victory 20/10, VE-65; 29/10, VE-70): 2–6 published papers of the same layout become 4–3N published
/// papers, each taking every part whole from one of them. A session section then delivers the versions by seat (VE-66).
/// </summary>
[ApiController]
[Route("api/admin/paper-mixes")]
[Authorize(Policy = StaffAuthDefaults.PapersPolicy)]
public sealed class AdminPaperMixesController(ExamPrepDbContext db, PaperMixer mixer, ILogger<AdminPaperMixesController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<PaperMixView>> List(CancellationToken cancellationToken) =>
        (await ViewsAsync(db, null, cancellationToken, db.PaperMixes.VisibleTo(User))).OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id).ToList();

    [HttpGet("{mixId:int}")]
    public async Task<ActionResult<PaperMixView>> Get(int mixId, CancellationToken cancellationToken) =>
        (await ViewsAsync(db, mixId, cancellationToken, db.PaperMixes.VisibleTo(User))).SingleOrDefault() is { } view ? view : NotFound();

    /// <summary>201 with the mixed papers; 400 listing every reason the papers cannot be mixed.</summary>
    [HttpPost]
    public async Task<ActionResult<PaperMixView>> Create(NewPaperMix request, CancellationToken cancellationToken)
    {
        var result = await mixer.CreateAsync(request.SourcePaperIds, request.VersionCount, request.Title, request.Seed, User.GetStaffId(), User.Owns, cancellationToken);
        if (result.Mix is not { } mix)
        {
            return Problems.Invalid("These papers cannot be mixed.", result.Errors);
        }

        logger.LogInformation("Staff {UserId} mixed papers {Sources} into {Count} papers, mix {MixId}", User.GetStaffId(), string.Join(", ", request.SourcePaperIds), request.VersionCount, mix.Id);
        var view = (await ViewsAsync(db, mix.Id, cancellationToken)).Single();
        return CreatedAtAction(nameof(Get), new { mixId = mix.Id }, view);
    }

    /// <param name="source">The mixes to read from (VE-72: a papers manager's own); every mix when null.</param>
    internal static async Task<List<PaperMixView>> ViewsAsync(ExamPrepDbContext db, int? mixId, CancellationToken cancellationToken, IQueryable<Domain.Entities.PaperMix>? source = null)
    {
        var mixes = await (source ?? db.PaperMixes).AsNoTracking()
            .Where(m => mixId == null || m.Id == mixId)
            .Select(m => new
            {
                m.Id,
                m.Title,
                ExamType = m.SectionTemplate!.ExamType!.Name,
                m.SectionTemplateId,
                Section = m.SectionTemplate.Title,
                Sources = m.Sources.OrderBy(x => x.Position).Select(x => new { x.Position, x.TestPaperId, x.TestPaper!.Title }).ToList(),
                m.VersionCount,
                m.Seed,
                m.CreatedAt,
            })
            .ToListAsync(cancellationToken);
        var ids = mixes.Select(m => m.Id).ToList();
        var versions = await db.TestPapers.AsNoTracking()
            .Where(p => p.PaperMixId != null && ids.Contains(p.PaperMixId.Value))
            .Select(p => new { MixId = p.PaperMixId!.Value, View = new PaperMixVersionView(p.MixVersion!.Value, p.Id, p.Title, p.MixPattern!) })
            .ToListAsync(cancellationToken);

        return mixes.Select(m => new PaperMixView(
            m.Id, m.Title, m.ExamType, m.SectionTemplateId, m.Section,
            m.Sources.Select(x => new PaperMixSourceView(((char)('A' + x.Position - 1)).ToString(), x.TestPaperId, x.Title)).ToList(),
            m.VersionCount, m.Seed, m.CreatedAt,
            versions.Where(v => v.MixId == m.Id).Select(v => v.View).OrderBy(v => v.Version).ToList())).ToList();
    }
}
