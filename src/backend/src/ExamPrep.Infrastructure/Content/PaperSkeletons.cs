using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ExamPrep.Infrastructure.Content;

/// <summary>
/// VE-25: a blank paper laid out like the real exam, so a paper can be written entirely on the web. Every text that
/// still has to be written starts with <see cref="Marker"/>, pictures and recordings are placeholder files, and the
/// answer keys point at the first option; publishing refuses a paper that still has any of them.
/// </summary>
public static class PaperSkeletons
{
    /// <summary>Starts every placeholder text; publishing refuses a paper that still contains it.</summary>
    public const string Marker = "[to write";

    /// <summary>A 1×1 PNG standing in for a picture until the real one is uploaded.</summary>
    public static readonly byte[] PlaceholderImage = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    /// <summary>One second of silence standing in for a Listening track.</summary>
    public static readonly byte[] PlaceholderAudio = Silence();

    /// <summary>SHA-256 of the placeholder files, to recognise them among stored assets.</summary>
    public static readonly IReadOnlySet<string> PlaceholderHashes = new HashSet<string>(
        [Convert.ToHexStringLower(SHA256.HashData(PlaceholderImage)), Convert.ToHexStringLower(SHA256.HashData(PlaceholderAudio))]);

    private const string ImagePath = "images/placeholder.png";

    private static string Text(string what) => $"{Marker}: {what}]";

    private static string Html(string what) => $"<p>{Text(what)}</p>";

    /// <summary>Whether a blank layout exists for this section of this exam type.</summary>
    public static bool Has(string examTypeCode, int sectionOrder) => examTypeCode == "A2_KEY_FS" && sectionOrder is 1 or 2;

    /// <summary>A package (.zip) with the blank layout of the section, or null when none is known yet (e.g. B1).</summary>
    public static byte[]? Zip(string examTypeCode, int sectionOrder, string title)
    {
        var parts = (examTypeCode, sectionOrder) switch
        {
            ("A2_KEY_FS", 1) => A2ReadingWriting(),
            ("A2_KEY_FS", 2) => A2Listening(),
            _ => null,
        };
        if (parts is null)
        {
            return null;
        }

        var paper = new JsonObject
        {
            ["formatVersion"] = 1,
            ["examType"] = examTypeCode,
            ["section"] = sectionOrder,
            ["title"] = title,
            ["parts"] = new JsonArray(parts.ToArray<JsonNode?>()),
        };

        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(zip, "paper.json", Encoding.UTF8.GetBytes(paper.ToJsonString()));
            Add(zip, ImagePath, PlaceholderImage);
            if (sectionOrder == 2)
            {
                for (var part = 1; part <= parts.Count; part++)
                {
                    Add(zip, $"audio/part{part}.wav", PlaceholderAudio);
                }
            }
        }

        return buffer.ToArray();
    }

    private static void Add(ZipArchive zip, string name, byte[] content)
    {
        using var stream = zip.CreateEntry(name).Open();
        stream.Write(content);
    }

    private static List<JsonObject> A2ReadingWriting()
    {
        var number = 1;
        const string choose = "For each question, choose the correct answer.";
        return
        [
            Part(1, "Single", null, null, Group("SingleChoice", choose, Questions(ref number, 6, n => Choice(n, prompt: null, stimulus: Html($"notice for question {n}"))))),
            Part(2, "Split", Text("title"), Html("texts about three people"), Group("SingleChoice", choose, Questions(ref number, 7, n => Choice(n, Html($"question {n}"))))),
            Part(3, "Split", Text("title"), Html("article"), Group("SingleChoice", choose, Questions(ref number, 5, n => Choice(n, Html($"question {n}"))))),
            Part(4, "Single", Text("title"), null, Gapped("InlineChoice", choose, ref number, 6, n => Choice(n, prompt: null))),
            Part(5, "Single", null, null, Gapped("InlineText", "For each question, write the correct answer. Write ONE word for each gap.", ref number, 6, Typed), maxWords: 1),
            Part(6, "Split", null, Html("writing task (an email or a note)"), Group("Essay", "Write 25 words or more.", Questions(ref number, 1, n => Essay(n, 25)))),
            Part(7, "Single", null, Html("story task: look at the three pictures"), Group("Essay", "Write 35 words or more.", Questions(ref number, 1, n => Essay(n, 35)))),
        ];
    }

    private static List<JsonObject> A2Listening()
    {
        var number = 1;
        const string choose = "For each question, choose the correct answer.";
        var parts = new List<JsonObject>
        {
            Part(1, "Single", null, null, Group("ImageChoice", choose, Questions(ref number, 5, n => Choice(n, Html($"question {n}"), pictures: true))), audio: 1),
            Part(2, "Single", Text("title"), null, Gapped("InlineText", "For each question, write the correct answer in the gap. Write one word or a number or a date or a time.", ref number, 5, Typed), maxWords: 3, audio: 2),
            Part(3, "Single", null, null, Group("SingleChoice", choose, Questions(ref number, 5, n => Choice(n, Html($"question {n}")))), audio: 3),
            Part(4, "Single", null, null, Group("SingleChoice", choose, Questions(ref number, 5, n => Choice(n, Html($"question {n}")))), audio: 4),
        };

        // Part 5: five people matched with eight cards; each placeholder answer is a different card.
        var first = number;
        var drag = Group("DragDropMatch", choose, Questions(ref number, 5, n =>
        {
            var question = Base(n, Html($"person for question {n}"));
            question["answers"] = new JsonArray(((char)('A' + n - first)).ToString());
            return question;
        }));
        drag["optionPool"] = new JsonArray("ABCDEFGH".Select(k => (JsonNode)Option(k.ToString(), $"card {k}")).ToArray());
        parts.Add(Part(5, "Single", null, null, drag, audio: 5));
        return parts;
    }

    private static JsonObject Part(int partNo, string layout, string? title, string? stimulus, JsonObject group, int? maxWords = null, int? audio = null)
    {
        if (maxWords is not null)
        {
            group["maxWordsPerAnswer"] = maxWords;
        }

        var part = new JsonObject { ["partNo"] = partNo, ["layout"] = layout, ["groups"] = new JsonArray(group) };
        if (title is not null)
        {
            part["title"] = title;
        }

        if (stimulus is not null)
        {
            part["stimulusHtml"] = stimulus;
        }

        if (audio is not null)
        {
            part["audio"] = $"audio/part{audio}.wav";
        }

        return part;
    }

    private static JsonObject Group(string type, string instructions, JsonArray questions) =>
        new() { ["interactionType"] = type, ["instructions"] = instructions, ["questions"] = questions };

    /// <summary>A text with one gap per question, written as {{q:n}}.</summary>
    private static JsonObject Gapped(string type, string instructions, ref int number, int count, Func<int, JsonObject> question)
    {
        var first = number;
        var questions = Questions(ref number, count, question);
        var gaps = string.Join(" ", Enumerable.Range(first, count).Select(n => $"{{{{q:{n}}}}}"));
        var group = Group(type, instructions, questions);
        group["bodyHtml"] = $"<p>{Text("text with the gaps")} {gaps}</p>";
        return group;
    }

    private static JsonArray Questions(ref int number, int count, Func<int, JsonObject> question)
    {
        var questions = new JsonArray();
        for (var i = 0; i < count; i++)
        {
            questions.Add(question(number++));
        }

        return questions;
    }

    private static JsonObject Base(int number, string? prompt)
    {
        var question = new JsonObject { ["number"] = number };
        if (prompt is not null)
        {
            question["promptHtml"] = prompt;
        }

        return question;
    }

    private static JsonObject Choice(int number, string? prompt, string? stimulus = null, bool pictures = false)
    {
        var question = Base(number, prompt);
        if (stimulus is not null)
        {
            question["stimulusHtml"] = stimulus;
        }

        question["options"] = new JsonArray(new[] { "A", "B", "C" }
            .Select(key => (JsonNode)Option(key, $"option {key}", pictures ? ImagePath : null))
            .ToArray());
        question["answers"] = new JsonArray("A");
        return question;
    }

    private static JsonObject Typed(int number)
    {
        var question = Base(number, null);
        question["answers"] = new JsonArray(Text("answer"));
        return question;
    }

    private static JsonObject Essay(int number, int minWords)
    {
        var question = Base(number, null);
        question["points"] = 15;
        question["minWords"] = minWords;
        return question;
    }

    private static JsonObject Option(string key, string what, string? image = null)
    {
        var option = new JsonObject { ["key"] = key, ["text"] = Text(what) };
        if (image is not null)
        {
            option["image"] = image;
        }

        return option;
    }

    /// <summary>A minimal WAV file: 1 second of 8 kHz, 8-bit mono silence.</summary>
    private static byte[] Silence()
    {
        const int rate = 8000;
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(36 + rate);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(rate);
        writer.Write(rate);
        writer.Write((short)1);
        writer.Write((short)8);
        writer.Write("data"u8);
        writer.Write(rate);
        writer.Write(Enumerable.Repeat((byte)0x80, rate).ToArray());
        writer.Flush();
        return stream.ToArray();
    }
}
