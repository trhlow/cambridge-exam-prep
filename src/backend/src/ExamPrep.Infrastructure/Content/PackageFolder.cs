using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;

namespace ExamPrep.Infrastructure.Content;

/// <summary>
/// Builds an exam package (.zip) from a folder holding <c>paper.json</c> and the files it references — the C#
/// twin of <c>setup/exam-package/pack.py</c>, used by the development demo seeder.
/// </summary>
public static class PackageFolder
{
    /// <param name="fillMissing">
    /// Put a placeholder (grey pixel, one second of silence) in place of referenced files that are missing,
    /// so a sample without recorded audio can still be imported for a try-out. Never for a real exam.
    /// </param>
    public static byte[] Zip(string folder, bool fillMissing)
    {
        var root = Path.GetFullPath(folder);
        var paperJson = File.ReadAllBytes(Path.Combine(root, PaperPackageReader.PaperFileName));

        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(zip, PaperPackageReader.PaperFileName, paperJson);
            foreach (var (path, isAudio) in ReferencedFiles(JsonNode.Parse(paperJson)))
            {
                var source = Path.GetFullPath(Path.Combine(root, path));
                if (source.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && File.Exists(source))
                {
                    Add(zip, path, File.ReadAllBytes(source));
                }
                else if (fillMissing)
                {
                    Add(zip, path, isAudio ? SilentWav() : GreyPixelPng);
                }
            }
        }

        return buffer.ToArray();
    }

    private static Dictionary<string, bool> ReferencedFiles(JsonNode? node)
    {
        var files = new Dictionary<string, bool>(StringComparer.Ordinal);
        Collect(node, files);
        return files;
    }

    private static void Collect(JsonNode? node, Dictionary<string, bool> files)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var (name, value) in obj)
                {
                    if (name is "audio" or "image" && value is JsonValue v && v.TryGetValue<string>(out var path))
                    {
                        files[path] = name == "audio";
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
                foreach (var path in PackageFileTypes.ReferencesIn(text))
                {
                    files.TryAdd(path, false);
                }

                break;
        }
    }

    private static void Add(ZipArchive zip, string path, byte[] content)
    {
        using var stream = zip.CreateEntry(path).Open();
        stream.Write(content);
    }

    private static readonly byte[] GreyPixelPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAAAAAA6fptVAAAACklEQVR4nGP4DwABAQEAWk1v8QAAAABJRU5ErkJggg==");

    private static byte[] SilentWav(int seconds = 1, int rate = 8000)
    {
        var samples = new byte[rate * seconds * 2];
        using var buffer = new MemoryStream();
        using var writer = new BinaryWriter(buffer, Encoding.ASCII);
        writer.Write("RIFF"u8);
        writer.Write(36 + samples.Length);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(rate);
        writer.Write(rate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(samples.Length);
        writer.Write(samples);
        writer.Flush();
        return buffer.ToArray();
    }
}
