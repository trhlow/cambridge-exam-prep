using System.Security.Cryptography;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Content;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Content;

/// <summary>Pictures and recordings uploaded through the paper editor (VE-25), stored like imported files.</summary>
public static class AssetUploads
{
    public const long MaxImageBytes = 5L * 1024 * 1024;

    /// <summary>Why the bytes are not an image the exam may show (by extension and file signature), or null.</summary>
    public static string? ImageProblem(byte[] content, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!PackageFileTypes.Images.ContainsKey(extension))
        {
            return "Choose a picture (png, jpg, gif or webp).";
        }

        if (content.Length > MaxImageBytes)
        {
            return "The picture must be at most 5 MB.";
        }

        var span = content.AsSpan();
        var matches = extension switch
        {
            ".png" => span.StartsWith((byte[])[0x89, 0x50, 0x4E, 0x47]),
            ".jpg" or ".jpeg" => span.StartsWith((byte[])[0xFF, 0xD8, 0xFF]),
            ".gif" => span.StartsWith("GIF8"u8),
            ".webp" => span.Length > 12 && span.StartsWith("RIFF"u8) && span[8..12].SequenceEqual("WEBP"u8),
            _ => false,
        };
        return matches ? null : "The file is not the picture its name says.";
    }

    /// <summary>The stored asset with these bytes, or a new one (saved with the paper's next SaveChanges).</summary>
    public static async Task<Asset> StoreAsync(
        ExamPrepDbContext db, IAssetStorage storage, byte[] content, string fileName, AssetKind kind, int userId, DateTime now, CancellationToken cancellationToken)
    {
        var sha = Convert.ToHexStringLower(SHA256.HashData(content));
        var existing = await db.Assets.FirstOrDefaultAsync(a => a.Sha256 == sha, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var asset = new Asset
        {
            Kind = kind,
            OriginalFileName = Path.GetFileName(fileName),
            StoredPath = await storage.SaveAsync(content, sha, extension, cancellationToken),
            ContentType = PackageFileTypes.For(kind)[extension],
            SizeBytes = content.Length,
            Sha256 = sha,
            UploadedByUserId = userId,
            CreatedAt = now,
        };
        db.Assets.Add(asset);
        return asset;
    }

    public static async Task<byte[]> ReadAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }
}
