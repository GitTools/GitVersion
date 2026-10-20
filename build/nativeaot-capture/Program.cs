using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GitVersion.Git;
using Microsoft.Extensions.Logging.Abstractions;

var checks = new List<string>();
var executor = new GitCliExecutor(NullLogger<GitCliExecutor>.Instance);
var newline = Environment.NewLine;
string? error = null;
try
{
    if (args.Length != 2)
    {
        throw new ArgumentException("Expected repository path and loopback credential fixture URL.");
    }
    var repository = Path.GetFullPath(args[0]);
    Directory.CreateDirectory(repository);
    Verify(executor.Execute(repository, ["init", "--quiet"]), 0, "", "");
    foreach (var (name, script) in new[]
    {
        ("CaptureNormalizesCrLfAndUnterminatedLine", "printf 'first\\r\\n\\r\\nlast'"),
        ("CaptureNormalizesLfAndTerminatedLine", "printf 'first\\n\\nlast\\n'"),
        ("CaptureNormalizesCarriageReturns", "printf 'first\\r\\rlast'")
    })
    {
        Verify(Capture(script), 0, $"first{newline}{newline}last{newline}", "");
        checks.Add(name);
    }
    Verify(Capture(":;"), 0, "", "");
    checks.Add("CapturePreservesEmptyOutput");
    Verify(Capture("printf 'output'; printf 'error' >&2; exit 7"), 7, "output" + newline, "error" + newline);
    checks.Add("CaptureSeparatesBothStreamsAndPreservesFailureExitCode");
    Verify(Capture("i=0; while [ $i -lt 10000 ]; do printf 'stdout-line\\n'; printf 'stderr-line\\n' >&2; i=$((i+1)); done"),
        0, string.Concat(Enumerable.Repeat("stdout-line" + newline, 10000)), string.Concat(Enumerable.Repeat("stderr-line" + newline, 10000)));
    checks.Add("CaptureDrainsLargeInterleavedStreams");
    Verify(executor.Execute(repository,
        ["-c", "alias.capture=!f() { printf '%s\\n' \"$1\" \"$GIT_TERMINAL_PROMPT\" \"$LC_ALL\"; }; f", "capture", "hello 'quoted' world"]),
        0, $"hello 'quoted' world{newline}0{newline}C{newline}", "");
    checks.Add("CapturePreservesArgumentsAndDeterministicEnvironment");
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    var originalEncoding = Console.OutputEncoding;
    try
    {
        foreach (var codePage in new[] { 65001, 1252 })
        {
            Console.OutputEncoding = Encoding.GetEncoding(codePage);
            Verify(Capture("printf 'Grüße 世界'; printf 'échec' >&2"), 0, "Grüße 世界" + newline, "échec" + newline);
            checks.Add("CaptureDecodesUtf8WithConsoleCodePage" + codePage);
        }
    }
    finally
    {
        Console.OutputEncoding = originalEncoding;
    }
    try
    {
        executor.Execute(Path.Combine(repository, "nonexistent"), ["--version"]);
        throw new InvalidOperationException("Expected process startup failure.");
    }
    catch (InvalidOperationException failure) when (failure.InnerException is Win32Exception && failure.Message.Contains("PATH", StringComparison.Ordinal))
    {
        checks.Add("CaptureReportsProcessStartupFailure");
    }
    var credential = executor.Execute(repository,
        ["-c", "credential.helper=!printf 'native-helper-failure' >&2; exit 1", "ls-remote", args[1]]);
    if (credential.IsSuccess || !credential.StandardError.Contains("native-helper-failure", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("Credential helper failure was not captured.");
    }
    checks.Add("CaptureReportsCredentialHelperFailure");

    GitCliResult Capture(string script) => executor.Execute(repository, ["-c", "alias.capture=!" + script, "capture"]);
}
catch (Exception failure)
{
    error = failure.ToString();
}
Console.WriteLine(JsonSerializer.Serialize(new CaptureReport(error == null, checks, error), CaptureJsonContext.Default.CaptureReport));
return error == null ? 0 : 1;

static void Verify(GitCliResult actual, int exitCode, string output, string errors)
{
    if (actual.ExitCode != exitCode || actual.IsSuccess != (exitCode == 0) || actual.StandardOutput != output || actual.StandardError != errors)
    {
        throw new InvalidOperationException($"Capture mismatch: exit={actual.ExitCode}, stdout length={actual.StandardOutput.Length}, stderr length={actual.StandardError.Length}; expected exit={exitCode}, lengths={output.Length}/{errors.Length}.");
    }
}

internal sealed record CaptureReport(bool Passed, List<string> Checks, string? Error);

[JsonSerializable(typeof(CaptureReport))]
internal partial class CaptureJsonContext : JsonSerializerContext;
