using ExamPrep.Domain.Enums;

namespace ExamPrep.Domain.Entities;

/// <summary>An uploaded file (picture, audio track, logo) stored on disk.</summary>
public class Asset
{
    public int Id { get; set; }

    public AssetKind Kind { get; set; }

    public required string OriginalFileName { get; set; }

    /// <summary>Path relative to the configured storage root.</summary>
    public required string StoredPath { get; set; }

    public required string ContentType { get; set; }

    public long SizeBytes { get; set; }

    /// <summary>Hex SHA-256 of the content, used to detect duplicate uploads.</summary>
    public required string Sha256 { get; set; }

    /// <summary>Audio length in milliseconds; null for images.</summary>
    public int? DurationMs { get; set; }

    public int? UploadedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }
}
