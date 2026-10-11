using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExamPrep.Infrastructure.Content;

/// <summary>
/// Opens an exam package (.zip) and parses its <c>paper.json</c>. Structural problems (not a zip, missing or
/// malformed paper.json, misspelled properties, file names escaping the package) are returned as errors;
/// content rules are checked afterwards by <see cref="PaperPackageValidator"/>.
/// </summary>
public static class PaperPackageReader
{
    public const string PaperFileName = "paper.json";
    public const int MaxFiles = 500;
    public const long MaxUncompressedBytes = 300L * 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
    };

    public static PaperPackageReadResult Read(Stream zip)
    {
        ZipArchive archive;
        try
        {
            archive = new ZipArchive(zip, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (InvalidDataException)
        {
            return Failed("The file is not a valid .zip package.");
        }

        using (archive)
        {
            if (archive.Entries.Count > MaxFiles)
            {
                return Failed($"The package has more than {MaxFiles} files.");
            }

            var (files, errors) = ReadFiles(archive);
            if (errors.Count > 0)
            {
                return new PaperPackageReadResult(null, errors);
            }

            if (!files.Remove(PaperFileName, out var json))
            {
                return Failed($"The package must contain {PaperFileName} at the top level.");
            }

            return Parse(json, files);
        }
    }

    private static (Dictionary<string, byte[]> Files, List<string> Errors) ReadFiles(ZipArchive archive)
    {
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var errors = new List<string>();
        long remaining = MaxUncompressedBytes;

        foreach (var entry in archive.Entries.Where(e => !e.FullName.EndsWith('/')))
        {
            var name = entry.FullName.Replace('\\', '/');
            if (!IsInsidePackage(name))
            {
                errors.Add($"File name '{entry.FullName}' is not allowed: files must stay inside the package.");
                continue;
            }

            var content = ReadEntry(entry, remaining);
            if (content is null)
            {
                return (files, [$"The package is larger than {MaxUncompressedBytes / (1024 * 1024)} MB when unpacked."]);
            }

            remaining -= content.Length;
            files[name] = content;
        }

        return (files, errors);
    }

    /// <summary>Reads at most <paramref name="limit"/> bytes; null when the entry is bigger, whatever its header claims.</summary>
    private static byte[]? ReadEntry(ZipArchiveEntry entry, long limit)
    {
        using var source = entry.Open();
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = source.Read(chunk, 0, chunk.Length)) > 0)
        {
            if (buffer.Length + read > limit)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    private static bool IsInsidePackage(string name) =>
        name.Length > 0
        && !name.StartsWith('/')
        && !name.Contains(':')
        && name.Split('/').All(segment => segment is not ("" or "." or ".."));

    private static PaperPackageReadResult Parse(byte[] json, Dictionary<string, byte[]> files)
    {
        try
        {
            var paper = JsonSerializer.Deserialize<PackagePaper>(json, JsonOptions);
            return paper is null
                ? Failed($"{PaperFileName} is empty.")
                : new PaperPackageReadResult(new PaperPackage(paper, files), []);
        }
        catch (JsonException ex)
        {
            return Failed($"{PaperFileName}: {ex.Message}");
        }
    }

    private static PaperPackageReadResult Failed(string error) => new(null, [error]);
}
