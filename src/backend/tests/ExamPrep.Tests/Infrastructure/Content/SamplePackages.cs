using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ExamPrep.Tests.Infrastructure.Content;

/// <summary>
/// Builds exam packages (.zip) from the sample <c>paper.json</c> files in <c>setup/exam-package/samples</c>.
/// Every file the paper references is added as a small placeholder, so tests can mutate the JSON freely.
/// </summary>
internal static partial class SamplePackages
{
    public const string ReadingWriting = "a2-key-fs-reading-writing";
    public const string Listening = "a2-key-fs-listening";
    public const string B1Reading = "b1-pfs-reading";

    public static JsonObject Load(string name) =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Samples", name, "paper.json")))!.AsObject();

    public static byte[] Zip(JsonObject paper, IEnumerable<string>? omit = null, IReadOnlyDictionary<string, byte[]>? extraFiles = null)
    {
        var omitted = new HashSet<string>(omit ?? []);
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(zip, "paper.json", Encoding.UTF8.GetBytes(paper.ToJsonString()));
            foreach (var path in ReferencedFiles(paper).Where(p => !omitted.Contains(p)))
            {
                Add(zip, path, Placeholder(path));
            }

            foreach (var (path, content) in extraFiles ?? new Dictionary<string, byte[]>())
            {
                Add(zip, path, content);
            }
        }

        return buffer.ToArray();
    }

    public static byte[] Zip(string name) => Zip(Load(name));

    /// <summary>Writes the paper as an unpacked package folder, the way pdf2package.py leaves its draft.</summary>
    public static void WriteFolder(JsonObject paper, string folder)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "paper.json"), paper.ToJsonString());
        foreach (var path in ReferencedFiles(paper))
        {
            var target = Path.Combine(folder, path);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllBytes(target, Placeholder(path));
        }
    }

    /// <summary>Deterministic content per path, so importing the same package twice gives identical files.</summary>
    public static byte[] Placeholder(string path) => Encoding.UTF8.GetBytes($"placeholder for {path}");

    public static IReadOnlySet<string> ReferencedFiles(JsonObject paper)
    {
        var files = new HashSet<string>();
        Collect(paper, files);
        return files;
    }

    /// <summary>Navigates the sample, e.g. <c>Question(paper, part: 1, number: 1)</c>.</summary>
    public static JsonObject Part(JsonObject paper, int partNo) =>
        paper["parts"]!.AsArray().Select(p => p!.AsObject()).Single(p => (int)p["partNo"]! == partNo);

    public static JsonObject Group(JsonObject paper, int partNo, int groupIndex = 0) =>
        Part(paper, partNo)["groups"]![groupIndex]!.AsObject();

    public static JsonObject Question(JsonObject paper, int number) =>
        paper["parts"]!.AsArray()
            .SelectMany(p => p!["groups"]!.AsArray())
            .SelectMany(g => g!["questions"]!.AsArray())
            .Select(q => q!.AsObject())
            .Single(q => (int)q["number"]! == number);

    private static void Collect(JsonNode? node, HashSet<string> files)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var (name, value) in obj)
                {
                    if (name is "audio" or "image" && value is JsonValue v && v.TryGetValue<string>(out var path))
                    {
                        files.Add(path);
                    }
                    else
                    {
                        Collect(value, files);
                    }
                }

                break;
            case JsonArray array:
                foreach (var item in array)
                {
                    Collect(item, files);
                }

                break;
            case JsonValue value when value.TryGetValue<string>(out var text):
                foreach (Match match in AssetReference().Matches(text))
                {
                    files.Add(match.Groups[1].Value);
                }

                break;
        }
    }

    private static void Add(ZipArchive zip, string path, byte[] content)
    {
        var entry = zip.CreateEntry(path);
        using var stream = entry.Open();
        stream.Write(content);
    }

    [GeneratedRegex("""asset:([^"'\s>]+)""")]
    private static partial Regex AssetReference();
}
