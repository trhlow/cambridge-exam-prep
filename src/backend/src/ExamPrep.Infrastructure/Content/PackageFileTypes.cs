using System.Text.RegularExpressions;
using ExamPrep.Domain.Enums;

namespace ExamPrep.Infrastructure.Content;

/// <summary>File types an exam package may contain, and how HTML refers to them.</summary>
public static partial class PackageFileTypes
{
    public static readonly IReadOnlyDictionary<string, string> Images = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
    };

    public static readonly IReadOnlyDictionary<string, string> Audio = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".mp3"] = "audio/mpeg",
        [".m4a"] = "audio/mp4",
        [".ogg"] = "audio/ogg",
        [".wav"] = "audio/wav",
    };

    public static IReadOnlyDictionary<string, string> For(AssetKind kind) => kind == AssetKind.Audio ? Audio : Images;

    public static string ContentType(string path, AssetKind kind) => For(kind)[Path.GetExtension(path)];

    /// <summary>Pictures inside HTML are written as <c>src='asset:images/notice.png'</c>.</summary>
    [GeneratedRegex("""asset:([^"'\s>]+)""")]
    public static partial Regex AssetReference();

    public static IEnumerable<string> ReferencesIn(string? html) =>
        html is null ? [] : AssetReference().Matches(html).Select(m => m.Groups[1].Value);
}
