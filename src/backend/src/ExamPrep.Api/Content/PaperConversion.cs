using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ExamPrep.Api.Content;

/// <summary>
/// Files saved from the upload, and where the converter writes the draft package folder. <c>AnswerKeyPdf</c> is null
/// when none was uploaded (B1 Writing has no key; the converter reports it missing for every other paper).
/// </summary>
public sealed record PaperConversionInput(string QuestionsPdf, string? AnswerKeyPdf, string AudioFolder, string Title, string OutputFolder);

/// <summary>
/// What the converter found: <c>Errors</c> must be fixed before the paper can be imported; <c>Checks</c> are points
/// a person should look at (the REVIEW.md list of pdf2package.py).
/// </summary>
public sealed record PaperConversionReport(IReadOnlyList<string> Errors, IReadOnlyList<string> Checks);

/// <summary>Turns a Cambridge question paper and answer key PDF (plus Listening tracks) into a package folder.</summary>
public interface IPaperConverter
{
    /// <exception cref="PaperConverterUnavailableException">The server has no converter set up.</exception>
    Task<PaperConversionReport> ConvertAsync(PaperConversionInput input, CancellationToken cancellationToken);
}

public sealed class PaperConverterUnavailableException(string message) : Exception(message);

/// <summary>Section <c>Converter</c>: where Python and setup/exam-package/pdf2package.py are.</summary>
public sealed class ConverterOptions
{
    /// <summary>Python executable; <c>python</c> on Windows, <c>python3</c> elsewhere when not set.</summary>
    public string? Python { get; set; }

    /// <summary>pdf2package.py; relative paths are resolved against the API's content root. Unset: no conversion.</summary>
    public string? Script { get; set; }

    public int TimeoutSeconds { get; set; } = 180;
}

/// <summary>Runs <c>pdf2package.py</c> in its own process and reads the report it writes.</summary>
public sealed class PythonPaperConverter(IOptions<ConverterOptions> options, IHostEnvironment environment, ILogger<PythonPaperConverter> logger) : IPaperConverter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed record ScriptReport(List<string>? Warnings, List<string>? Checks);

    public async Task<PaperConversionReport> ConvertAsync(PaperConversionInput input, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Script))
        {
            throw new PaperConverterUnavailableException("PDF conversion is not set up on this server (Converter:Script).");
        }

        var script = Path.GetFullPath(Path.Combine(environment.ContentRootPath, settings.Script));
        if (!File.Exists(script))
        {
            throw new PaperConverterUnavailableException("PDF conversion is not set up on this server: the converter script is missing.");
        }

        var reportPath = Path.Combine(Path.GetDirectoryName(input.OutputFolder)!, "report.json");
        var start = new ProcessStartInfo(settings.Python ?? (OperatingSystem.IsWindows() ? "python" : "python3"))
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        // An argument list, never a command line: the title and paths reach Python as they are.
        string[] key = input.AnswerKeyPdf is null ? [] : [input.AnswerKeyPdf];
        foreach (var argument in new[] { "-I", script, input.QuestionsPdf }.Concat(key).Concat(
                 [
                     "--title", input.Title, "--audio-dir", input.AudioFolder, "-o", input.OutputFolder, "--report", reportPath,
                 ]))
        {
            start.ArgumentList.Add(argument);
        }

        Process process;
        try
        {
            process = Process.Start(start) ?? throw new PaperConverterUnavailableException("PDF conversion could not start.");
        }
        catch (Win32Exception e)
        {
            throw new PaperConverterUnavailableException($"PDF conversion is not set up on this server: Python could not be started ({e.Message}).");
        }

        using (process)
        {
            var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorOutput = process.StandardError.ReadToEndAsync(cancellationToken);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                cancellationToken.ThrowIfCancellationRequested();
                return new PaperConversionReport([$"The conversion took longer than {settings.TimeoutSeconds} seconds and was stopped."], []);
            }

            var stderr = await errorOutput;
            await output;
            if (File.Exists(reportPath))
            {
                var report = JsonSerializer.Deserialize<ScriptReport>(await File.ReadAllTextAsync(reportPath, cancellationToken), Json)!;
                return new PaperConversionReport(report.Warnings ?? [], report.Checks ?? []);
            }

            // No report: the script crashed. The last line of a Python traceback is the error itself.
            logger.LogWarning("pdf2package.py exited with {ExitCode} without a report: {Stderr}", process.ExitCode, stderr);
            var last = stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault() ?? $"exit code {process.ExitCode}";
            return new PaperConversionReport([$"The converter stopped with an error: {last}"], []);
        }
    }
}
