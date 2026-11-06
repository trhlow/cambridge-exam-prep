using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ExamPrep.Infrastructure.Content;

/// <summary>
/// VE-25: a blank paper laid out like the real exam, so a paper can be written entirely on the web. Every text that
/// still has to be written starts with <see cref="Marker"/>, pictures and recordings are placeholder files, and
/// no question has an answer yet; publishing refuses a paper that still has any of them.
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
    public static bool Has(string examTypeCode, int sectionOrder) => Parts(examTypeCode, sectionOrder) is not null;

    /// <summary>
    /// The parts of the blank layout: A2 Key for Schools Reading &amp; Writing and Listening, and B1 Preliminary for Schools
    /// Reading, Writing and Listening (VE-71, laid out like the self-written B1 samples); null for any other section.
    /// </summary>
    private static List<JsonObject>? Parts(string examTypeCode, int sectionOrder) => (examTypeCode, sectionOrder) switch
    {
        ("A2_KEY_FS", 1) => A2ReadingWriting(),
        ("A2_KEY_FS", 2) => A2Listening(),
        ("B1_PRELIM_FS", 1) => B1Reading(),
        ("B1_PRELIM_FS", 2) => B1Writing(),
        ("B1_PRELIM_FS", 3) => B1Listening(),
        _ => null,
    };

    /// <summary>A package (.zip) with the blank layout of the section, or null when none is known.</summary>
    public static byte[]? Zip(string examTypeCode, int sectionOrder, string title)
    {
        var parts = Parts(examTypeCode, sectionOrder);
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
            foreach (var audio in parts.Select(p => (string?)p["audio"]).OfType<string>())
            {
                Add(zip, audio, PlaceholderAudio);
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

        // Part 5: five people matched with eight cards.
        var drag = Group("DragDropMatch", choose, Questions(ref number, 5, n => Base(n, Html($"person for question {n}"))));
        drag["optionPool"] = new JsonArray("ABCDEFGH".Select(k => (JsonNode)Option(k.ToString(), $"card {k}")).ToArray());
        parts.Add(Part(5, "Single", null, null, drag, audio: 5));
        return parts;
    }

    private static List<JsonObject> B1Reading()
    {
        var number = 1;
        const string choose = "For each question, choose the correct answer.";
        var parts = new List<JsonObject>
        {
            Part(1, "Single", null, null, Group("SingleChoice", choose, Questions(ref number, 5, n => Choice(n, prompt: null, stimulus: Html($"notice or message for question {n}"))))),
        };

        // Part 2: five people (picture and description) matched with eight texts; Part 4: five sentences dragged into
        // the gaps of a text, three extra.
        var people = Group("DragDropMatch", Text("instructions: what the people want and what the texts are"), Questions(ref number, 5, n => Card(n, Html($"person for question {n}"), Html($"picture of person {n}"))));
        people["itemsHeading"] = Text("heading for the people");
        people["poolHeading"] = Text("heading for the texts");
        people["optionPool"] = Pool(8, titled: true);
        parts.Add(Part(2, "Single", null, null, people));
        parts.Add(Part(3, "Split", Text("title"), Html("text"), Group("SingleChoice", choose, Questions(ref number, 5, n => Choice(n, Html($"question {n}"), options: 4)))));

        var sentences = Gapped("DragDropMatch", "Five sentences have been removed from the text below. For each question, choose the correct answer. There are three extra sentences which you do not need to use.", ref number, 5, n => Card(n, null));
        sentences["optionPool"] = Pool(8, titled: false);
        parts.Add(Part(4, "Single", Text("title"), null, sentences));
        parts.Add(Part(5, "Single", Text("title"), null, Gapped("InlineChoice", choose, ref number, 6, n => Choice(n, prompt: null, options: 4))));
        parts.Add(Part(6, "Single", Text("title"), null, Gapped("InlineText", "For each question, write the correct answer. Write one word for each gap.", ref number, 6, Typed), maxWords: 1));
        return parts;
    }

    private static List<JsonObject> B1Writing()
    {
        var number = 1;
        var email = Group("Essay", "You must answer this question. Write your answer in about 100 words.", Questions(ref number, 1, n => Essay(n, 100, 20, Html("email from a friend, with your notes"))));
        var choice = Group("Essay", "Choose one of these questions. Write your answer in about 100 words.", Questions(ref number, 2, n => Essay(n, 100, 20, Html(n == 2 ? "article task" : "story task"))));
        choice["chooseCount"] = 1;
        var second = Part(2, "Split", null, null, choice);
        second["questionPerPage"] = true;
        return [Part(1, "Split", null, null, email), second];
    }

    private static List<JsonObject> B1Listening()
    {
        var number = 1;
        const string choose = "For each question, choose the correct answer.";
        var pictures = Part(1, "Single", null, null, Group("ImageChoice", choose, Questions(ref number, 7, n => Choice(n, Html($"question {n}"), pictures: true))), audio: 1);
        var interviews = Part(2, "Single", null, null, Group("SingleChoice", choose, Questions(ref number, 6, n => Choice(n, Html($"situation and question {n}")))), audio: 2);
        interviews["questionPerPage"] = true;
        var notes = Part(3, "Single", Text("title"), null, Gapped("InlineText", "For each question, write the correct answer in the gap. Write one or two words or a number or a date or a time.", ref number, 6, Typed), maxWords: 2, audio: 3);
        var interview = Part(4, "Single", null, null, Group("SingleChoice", choose, Questions(ref number, 6, n => Choice(n, Html($"question {n}")))), audio: 4);
        return [pictures, interviews, notes, interview];
    }

    /// <summary>A drag-and-drop question; the card it takes is chosen in the editor.</summary>
    private static JsonObject Card(int number, string? prompt, string? stimulus = null)
    {
        var question = new JsonObject { ["number"] = number };
        if (stimulus is not null)
        {
            question["stimulusHtml"] = stimulus;
        }

        if (prompt is not null)
        {
            question["promptHtml"] = prompt;
        }

        return question;
    }

    private static JsonArray Pool(int count, bool titled) =>
        new(Enumerable.Range(0, count).Select(i =>
        {
            var key = ((char)('A' + i)).ToString();
            var option = Option(key, titled ? $"text {key}" : $"sentence {key}");
            if (titled)
            {
                option["title"] = Text($"title of text {key}");
            }

            return (JsonNode)option;
        }).ToArray());

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

    private static JsonObject Choice(int number, string? prompt, string? stimulus = null, bool pictures = false, int options = 3)
    {
        var question = Base(number, prompt);
        if (stimulus is not null)
        {
            question["stimulusHtml"] = stimulus;
        }

        question["options"] = new JsonArray(new[] { "A", "B", "C", "D" }.Take(options)
            .Select(key => (JsonNode)Option(key, $"option {key}", pictures ? ImagePath : null))
            .ToArray());
        return question;
    }

    private static JsonObject Typed(int number) => Base(number, null);

    private static JsonObject Essay(int number, int minWords, int points = 15, string? task = null)
    {
        var question = Base(number, null);
        if (task is not null)
        {
            question["stimulusHtml"] = task;
        }

        question["points"] = points;
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
