using System.Text;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Content;

namespace ExamPrep.Tests.Infrastructure.Content;

/// <summary>VE-5: reading an exam package (.zip with paper.json, images and audio).</summary>
public sealed class PaperPackageReaderTests
{
    private static PaperPackageReadResult Read(byte[] zip) => PaperPackageReader.Read(new MemoryStream(zip));

    [Fact]
    public void Read_ParsesSampleReadingWritingPackage()
    {
        var result = Read(SamplePackages.Zip(SamplePackages.ReadingWriting));

        Assert.Empty(result.Errors);
        var package = result.Package!;
        Assert.Equal("A2_KEY_FS", package.Paper.ExamType);
        Assert.Equal(1, package.Paper.Section);
        Assert.Equal(7, package.Paper.Parts.Count);
        Assert.Equal(InteractionType.InlineChoice, package.Paper.Parts[3].Groups[0].InteractionType);
        Assert.Equal(["will", "'ll"], package.Paper.Parts[4].Groups[0].Questions[2].Answers);
        Assert.Contains("images/rw-p1-q1.png", package.Files.Keys);
    }

    [Fact]
    public void Read_ReportsError_WhenFileIsNotAZip()
    {
        var result = Read(Encoding.UTF8.GetBytes("this is not a zip file"));

        Assert.Null(result.Package);
        Assert.Contains(result.Errors, e => e.Contains("not a valid .zip"));
    }

    [Fact]
    public void Read_ReportsError_WhenPaperJsonIsMissing()
    {
        var paper = SamplePackages.Load(SamplePackages.ReadingWriting);
        var zip = SamplePackages.Zip(paper, omit: [], extraFiles: null);
        var withoutPaper = RemoveEntry(zip, "paper.json");

        var result = Read(withoutPaper);

        Assert.Null(result.Package);
        Assert.Contains(result.Errors, e => e.Contains("paper.json"));
    }

    [Fact]
    public void Read_ReportsMisspelledProperty()
    {
        var paper = SamplePackages.Load(SamplePackages.ReadingWriting);
        var question = SamplePackages.Question(paper, 1);
        question["anwsers"] = question["answers"]!.DeepClone();

        var result = Read(SamplePackages.Zip(paper));

        Assert.Null(result.Package);
        Assert.Contains(result.Errors, e => e.Contains("anwsers"));
    }

    [Fact]
    public void Read_ReportsBrokenJson()
    {
        var zip = SamplePackages.Zip(SamplePackages.Load(SamplePackages.ReadingWriting));
        var broken = ReplaceEntry(zip, "paper.json", Encoding.UTF8.GetBytes("""{ "formatVersion": 1, "parts": [ """));

        var result = Read(broken);

        Assert.Null(result.Package);
        Assert.Contains(result.Errors, e => e.StartsWith("paper.json"));
    }

    [Theory]
    [InlineData("../evil.png")]
    [InlineData("images/../../evil.png")]
    [InlineData("/etc/evil.png")]
    public void Read_RejectsFileNamesOutsideThePackage(string name)
    {
        var paper = SamplePackages.Load(SamplePackages.ReadingWriting);
        var zip = SamplePackages.Zip(paper, extraFiles: new Dictionary<string, byte[]> { [name] = [1, 2, 3] });

        var result = Read(zip);

        Assert.Null(result.Package);
        Assert.Contains(result.Errors, e => e.Contains(name));
    }

    private static byte[] RemoveEntry(byte[] zip, string name) => Rewrite(zip, name, replacement: null);

    private static byte[] ReplaceEntry(byte[] zip, string name, byte[] content) => Rewrite(zip, name, content);

    private static byte[] Rewrite(byte[] zip, string name, byte[]? replacement)
    {
        var buffer = new MemoryStream();
        buffer.Write(zip);
        using (var archive = new System.IO.Compression.ZipArchive(buffer, System.IO.Compression.ZipArchiveMode.Update, leaveOpen: true))
        {
            archive.GetEntry(name)!.Delete();
            if (replacement is not null)
            {
                using var stream = archive.CreateEntry(name).Open();
                stream.Write(replacement);
            }
        }

        return buffer.ToArray();
    }
}
