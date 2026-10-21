using System.ComponentModel.DataAnnotations;
using ExamPrep.Api.Auth;
using ExamPrep.Api.Common;
using ExamPrep.Infrastructure.Content;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Content;

/// <param name="Seed">Optional: the same sources and seed give the same four papers again.</param>
public sealed record NewPaperMix(int SourceAPaperId, int SourceBPaperId, [StringLength(PaperMixer.MaxTitleLength)] string? Title, int? Seed);

public sealed record PaperRef(int Id, string Title);

/// <param name="Pattern">The source of each part in part order, e.g. "ABBAABA".</param>
public sealed record PaperMixVersionView(int Version, int PaperId, string Title, string Pattern);

public sealed record PaperMixView(
    int Id,
    string Title,
    string ExamType,
    int SectionTemplateId,
    string Section,
    PaperRef SourceA,
    PaperRef SourceB,
    int Seed,
    DateTime CreatedAt,
    IReadOnlyList<PaperMixVersionView> Versions);

/// <summary>
/// Paper shuffling (Victory 20/10, VE-65): two published papers of the same layout become four published papers, each
/// taking every part whole from one of them. A session section then delivers the four versions by seat (VE-66).
/// </summary>
[ApiController]
[Route("api/admin/paper-mixes")]
[Authorize(Policy = StaffAuthDefaults.PapersPolicy)]
public sealed class AdminPaperMixesController(ExamPrepDbContext db, PaperMixer mixer, ILogger<AdminPaperMixesController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<PaperMixView>> List(CancellationToken cancellationToken) =>
        (await ViewsAsync(db, null, cancellationToken)).OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id).ToList();

    [HttpGet("{mixId:int}")]
    public async Task<ActionResult<PaperMixView>> Get(int mixId, CancellationToken cancellationToken) =>
        (await ViewsAsync(db, mixId, cancellationToken)).SingleOrDefault() is { } view ? view : NotFound();

    /// <summary>201 with the four papers; 400 listing every reason the two papers cannot be mixed.</summary>
    [HttpPost]
    public async Task<ActionResult<PaperMixView>> Create(NewPaperMix request, CancellationToken cancellationToken)
    {
        var result = await mixer.CreateAsync(request.SourceAPaperId, request.SourceBPaperId, request.Title, request.Seed, User.GetStaffId(), cancellationToken);
        if (result.Mix is not { } mix)
        {
            return Problems.Invalid("These papers cannot be mixed.", result.Errors);
        }

        logger.LogInformation("Staff {UserId} mixed papers {A} and {B} into mix {MixId}", User.GetStaffId(), request.SourceAPaperId, request.SourceBPaperId, mix.Id);
        var view = (await ViewsAsync(db, mix.Id, cancellationToken)).Single();
        return CreatedAtAction(nameof(Get), new { mixId = mix.Id }, view);
    }

    internal static async Task<List<PaperMixView>> ViewsAsync(ExamPrepDbContext db, int? mixId, CancellationToken cancellationToken)
    {
        var mixes = await db.PaperMixes.AsNoTracking()
            .Where(m => mixId == null || m.Id == mixId)
            .Select(m => new
            {
                m.Id,
                m.Title,
                ExamType = m.SectionTemplate!.ExamType!.Name,
                m.SectionTemplateId,
                Section = m.SectionTemplate.Title,
                SourceA = new PaperRef(m.SourceAPaperId, m.SourceAPaper!.Title),
                SourceB = new PaperRef(m.SourceBPaperId, m.SourceBPaper!.Title),
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
            m.Id, m.Title, m.ExamType, m.SectionTemplateId, m.Section, m.SourceA, m.SourceB, m.Seed, m.CreatedAt,
            versions.Where(v => v.MixId == m.Id).Select(v => v.View).OrderBy(v => v.Version).ToList())).ToList();
    }
}
