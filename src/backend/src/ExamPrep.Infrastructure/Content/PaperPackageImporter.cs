using System.Security.Cryptography;
using System.Text.Json;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Infrastructure.Content;

public sealed record PaperImportResult(TestPaper? Paper, ExamType? ExamType, SectionTemplate? Section, IReadOnlyList<string> Errors);

/// <summary>
/// Turns a validated exam package into a draft <see cref="TestPaper"/>: stores the referenced files (reusing
/// identical ones), points HTML at <c>/api/assets/{id}</c>, strips scripts from HTML, and saves everything in
/// one transaction. Nothing is created when the package has any error.
/// </summary>
public sealed class PaperPackageImporter(ExamPrepDbContext db, IAssetStorage storage, TimeProvider clock)
{
    public const string AssetUrlPrefix = "/api/assets/";

    private static readonly HtmlSanitizer Sanitizer = new();
    private static readonly JsonSerializerOptions OptionJson = new(JsonSerializerDefaults.Web);

    private sealed record StoredOption(string Key, string? Text, int? ImageAssetId);

    public async Task<PaperImportResult> ImportAsync(Stream zip, int userId, CancellationToken cancellationToken)
    {
        var read = PaperPackageReader.Read(zip);
        if (read.Package is null)
        {
            return Failed(read.Errors);
        }

        var paper = read.Package.Paper;
        var examType = await db.ExamTypes.AsNoTracking().SingleOrDefaultAsync(t => t.Code == paper.ExamType, cancellationToken);
        if (examType is null)
        {
            return Failed([$"Unknown exam type '{paper.ExamType}'."]);
        }

        var template = await db.SectionTemplates.AsNoTracking()
            .SingleOrDefaultAsync(s => s.ExamTypeId == examType.Id && s.OrderNo == paper.Section, cancellationToken);
        if (template is null)
        {
            return Failed([$"{examType.Name} has no section {paper.Section}."]);
        }

        var errors = PaperPackageValidator.Validate(read.Package, template);
        if (errors.Count > 0)
        {
            return Failed(errors);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var assets = await StoreAssetsAsync(read.Package, userId, cancellationToken);
        var entity = ToEntity(paper, template, assets, userId);
        db.TestPapers.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new PaperImportResult(entity, examType, template, []);
    }

    private static PaperImportResult Failed(IReadOnlyList<string> errors) => new(null, null, null, errors);

    /// <summary>Every file the paper refers to, with the role it plays (picture or audio track).</summary>
    private static Dictionary<string, AssetKind> ReferencedFiles(PackagePaper paper)
    {
        var files = new Dictionary<string, AssetKind>(StringComparer.Ordinal);
        foreach (var part in paper.Parts)
        {
            if (part.Audio is not null)
            {
                files[part.Audio] = AssetKind.Audio;
            }

            var html = part.Groups
                .SelectMany(g => g.Questions.SelectMany(q => new[] { q.PromptHtml, q.StimulusHtml }).Append(g.BodyHtml))
                .Append(part.StimulusHtml);
            var optionImages = part.Groups
                .SelectMany(g => (g.OptionPool ?? []).Concat(g.Questions.SelectMany(q => q.Options ?? [])))
                .Select(o => o.Image)
                .OfType<string>();

            foreach (var path in html.SelectMany(PackageFileTypes.ReferencesIn).Concat(optionImages))
            {
                files[path] = AssetKind.Image;
            }
        }

        return files;
    }

    private async Task<Dictionary<string, Asset>> StoreAssetsAsync(PaperPackage package, int userId, CancellationToken cancellationToken)
    {
        var byPath = new Dictionary<string, Asset>(StringComparer.Ordinal);
        var bySha = new Dictionary<string, Asset>(StringComparer.Ordinal);

        foreach (var (path, kind) in ReferencedFiles(package.Paper))
        {
            var content = package.Files[path];
            var sha = Convert.ToHexStringLower(SHA256.HashData(content));
            if (!bySha.TryGetValue(sha, out var asset))
            {
                asset = await db.Assets.FirstOrDefaultAsync(a => a.Sha256 == sha, cancellationToken)
                    ?? await CreateAssetAsync(path, kind, content, sha, userId, cancellationToken);
                bySha[sha] = asset;
            }

            byPath[path] = asset;
        }

        await db.SaveChangesAsync(cancellationToken);
        return byPath;
    }

    private async Task<Asset> CreateAssetAsync(string path, AssetKind kind, byte[] content, string sha, int userId, CancellationToken cancellationToken)
    {
        var storedPath = await storage.SaveAsync(content, sha, Path.GetExtension(path), cancellationToken);
        var asset = new Asset
        {
            Kind = kind,
            OriginalFileName = Path.GetFileName(path),
            StoredPath = storedPath,
            ContentType = PackageFileTypes.ContentType(path, kind),
            SizeBytes = content.Length,
            Sha256 = sha,
            UploadedByUserId = userId,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
        };
        db.Assets.Add(asset);
        return asset;
    }

    private TestPaper ToEntity(PackagePaper paper, SectionTemplate template, Dictionary<string, Asset> assets, int userId) => new()
    {
        SectionTemplateId = template.Id,
        Title = paper.Title.Trim(),
        Status = PaperStatus.Draft,
        CreatedByUserId = userId,
        CreatedAt = clock.GetUtcNow().UtcDateTime,
        Parts = paper.Parts.Select(part => new PaperPart
        {
            PartNo = part.PartNo,
            Title = part.Title,
            Layout = part.Layout,
            StimulusHtml = Html(part.StimulusHtml, assets),
            AudioAsset = part.Audio is null ? null : assets[part.Audio],
            Groups = part.Groups.Select((group, i) => ToEntity(group, i + 1, assets)).ToList(),
        }).ToList(),
    };

    private static QuestionGroup ToEntity(PackageGroup group, int orderNo, Dictionary<string, Asset> assets) => new()
    {
        OrderNo = orderNo,
        InteractionType = group.InteractionType,
        Instructions = group.Instructions.Trim(),
        BodyHtml = Html(group.BodyHtml, assets),
        OptionPoolJson = Options(group.OptionPool, assets),
        AllowOptionReuse = group.AllowOptionReuse,
        MaxWordsPerAnswer = group.MaxWordsPerAnswer,
        Questions = group.Questions.Select((question, i) => new Question
        {
            Number = question.Number,
            OrderNo = i + 1,
            PromptHtml = Html(question.PromptHtml, assets),
            StimulusHtml = Html(question.StimulusHtml, assets),
            OptionsJson = Options(question.Options, assets),
            Points = question.Points,
            MinWords = question.MinWords,
            AnswerKeys = (question.Answers ?? []).Select(a => new AnswerKey { Value = a.Trim() }).ToList(),
        }).ToList(),
    };

    /// <summary>Points <c>asset:</c> references at the stored files, then removes scripts and event handlers.</summary>
    private static string? Html(string? html, Dictionary<string, Asset> assets)
    {
        if (html is null)
        {
            return null;
        }

        var linked = PackageFileTypes.AssetReference().Replace(html, m => $"{AssetUrlPrefix}{assets[m.Groups[1].Value].Id}");
        return Sanitizer.Sanitize(linked);
    }

    private static string? Options(IReadOnlyList<PackageOption>? options, Dictionary<string, Asset> assets) =>
        options is null
            ? null
            : JsonSerializer.Serialize(
                options.Select(o => new StoredOption(o.Key, o.Text, o.Image is null ? null : assets[o.Image].Id)),
                OptionJson);
}
