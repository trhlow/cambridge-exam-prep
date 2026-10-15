using System.ComponentModel;
using System.Diagnostics;
using ExamPrep.Api.Content;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ExamPrep.Tests.Api.Content;

/// <summary>
/// Running the converter process: arguments reach Python unchanged, its report is read back, a crash becomes an
/// error the administrator can read. A stand-in script replaces pdf2package.py; skipped when Python is missing.
/// </summary>
public sealed class PythonPaperConverterTests : IDisposable
{
    private const string FakeScript = """
        import argparse, json, pathlib
        parser = argparse.ArgumentParser()
        for name in ("questions", "key"):
            parser.add_argument(name)
        for name in ("--title", "--audio-dir", "-o", "--report"):
            parser.add_argument(name)
        args = parser.parse_args()
        if args.title == "crash":
            raise ValueError("bad layout")
        pathlib.Path(args.o).mkdir(parents=True)
        (pathlib.Path(args.o) / "paper.json").write_text("{}", encoding="utf-8")
        audio = sorted(p.name for p in pathlib.Path(args.audio_dir).iterdir())
        with open(args.report, "w", encoding="utf-8") as report:
            json.dump({"warnings": [], "checks": ["title=" + args.title, "audio=" + ",".join(audio)]}, report)
        """;

    private readonly string folder = Directory.CreateTempSubdirectory("converter-test-").FullName;
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    public void Dispose() => Directory.Delete(folder, recursive: true);

    private static string Python => OperatingSystem.IsWindows() ? "python" : "python3";

    private static void SkipWithoutPython()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(Python, "--version") { RedirectStandardOutput = true, UseShellExecute = false })!;
            process.WaitForExit();
        }
        catch (Win32Exception)
        {
            Assert.Skip("Python is not installed.");
        }
    }

    private PythonPaperConverter Converter(string? script)
    {
        var options = Options.Create(new ConverterOptions { Script = script, TimeoutSeconds = 60 });
        return new PythonPaperConverter(options, new HostingEnvironment { ContentRootPath = folder }, NullLogger<PythonPaperConverter>.Instance);
    }

    private PaperConversionInput Input(string title)
    {
        var audio = Directory.CreateDirectory(Path.Combine(folder, "audio")).FullName;
        File.WriteAllText(Path.Combine(audio, "part1.mp3"), "track");
        return new PaperConversionInput(Path.Combine(folder, "q.pdf"), Path.Combine(folder, "k.pdf"), audio, title, Path.Combine(folder, "package"));
    }

    [Fact]
    public async Task Convert_PassesTheTitleAndFoldersAsTheyAreAndReadsTheReport()
    {
        SkipWithoutPython();
        await File.WriteAllTextAsync(Path.Combine(folder, "fake.py"), FakeScript, ct);
        const string title = "Đề \"thử\" & <1> --audio-dir x";

        var report = await Converter("fake.py").ConvertAsync(Input(title), ct);

        Assert.Empty(report.Errors);
        Assert.Equal([$"title={title}", "audio=part1.mp3"], report.Checks);
        Assert.True(File.Exists(Path.Combine(folder, "package", "paper.json")));
    }

    [Fact]
    public async Task Convert_TurnsACrashIntoTheLastLineOfTheError()
    {
        SkipWithoutPython();
        await File.WriteAllTextAsync(Path.Combine(folder, "fake.py"), FakeScript, ct);

        var report = await Converter("fake.py").ConvertAsync(Input("crash"), ct);

        Assert.Equal(["The converter stopped with an error: ValueError: bad layout"], report.Errors);
    }

    [Fact]
    public async Task Convert_IsUnavailableWithoutAScript()
    {
        await Assert.ThrowsAsync<PaperConverterUnavailableException>(() => Converter(null).ConvertAsync(Input("x"), ct));
        await Assert.ThrowsAsync<PaperConverterUnavailableException>(() => Converter("missing.py").ConvertAsync(Input("x"), ct));
    }
}
