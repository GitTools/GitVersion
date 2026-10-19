using Microsoft.Extensions.Logging.Abstractions;

namespace GitVersion.Git.Managed.Tests;

[TestFixture]
public class GitCliExecutorTests
{
    /// <summary>Normalizes line separators and retains blank and unterminated lines.</summary>
    [TestCase("printf 'first\\r\\n\\r\\nlast'", TestName = "CaptureNormalizesCrLfAndUnterminatedLine")]
    [TestCase("printf 'first\\n\\nlast\\n'", TestName = "CaptureNormalizesLfAndTerminatedLine")]
    [TestCase("printf 'first\\r\\rlast'", TestName = "CaptureNormalizesCarriageReturns")]
    public void CaptureNormalizesLines(string script)
    {
        using var repository = new GitTestRepository();
        var result = Capture(repository, script);

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldBe($"first{SysEnv.NewLine}{SysEnv.NewLine}last{SysEnv.NewLine}");
        result.StandardError.ShouldBeEmpty();
    }

    /// <summary>Returns empty strings when Git writes neither output stream.</summary>
    [Test]
    public void CapturePreservesEmptyOutput()
    {
        using var repository = new GitTestRepository();
        var result = Capture(repository, ":;");

        result.IsSuccess.ShouldBeTrue();
        result.StandardOutput.ShouldBeEmpty();
        result.StandardError.ShouldBeEmpty();
    }

    /// <summary>Keeps stdout and stderr separate and preserves a failing exit code.</summary>
    [Test]
    public void CaptureSeparatesBothStreamsAndPreservesFailureExitCode()
    {
        using var repository = new GitTestRepository();
        var result = Capture(repository, "printf 'output'; printf 'error' >&2; exit 7");

        result.ExitCode.ShouldBe(7);
        result.IsSuccess.ShouldBeFalse();
        result.StandardOutput.ShouldBe($"output{SysEnv.NewLine}");
        result.StandardError.ShouldBe($"error{SysEnv.NewLine}");
    }

    /// <summary>Drains both pipes without deadlock or data loss when output exceeds pipe capacity.</summary>
    [Test]
    public void CaptureDrainsLargeInterleavedStreams()
    {
        using var repository = new GitTestRepository();
        var result = Capture(repository, "i=0; while [ $i -lt 10000 ]; do printf 'stdout-line\\n'; printf 'stderr-line\\n' >&2; i=$((i+1)); done");

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldBe(string.Concat(Enumerable.Repeat($"stdout-line{SysEnv.NewLine}", 10000)));
        result.StandardError.ShouldBe(string.Concat(Enumerable.Repeat($"stderr-line{SysEnv.NewLine}", 10000)));
    }

    /// <summary>Preserves quoted arguments and supplies non-interactive, locale-independent Git settings.</summary>
    [Test]
    public void CapturePreservesArgumentsAndDeterministicEnvironment()
    {
        using var repository = new GitTestRepository();
        var executor = new GitCliExecutor(NullLogger<GitCliExecutor>.Instance);
        var result = executor.Execute(repository.RepositoryPath,
            ["-c", "alias.capture=!f() { printf '%s\\n' \"$1\" \"$GIT_TERMINAL_PROMPT\" \"$LC_ALL\"; }; f", "capture", "hello 'quoted' world"]);

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldBe($"hello 'quoted' world{SysEnv.NewLine}0{SysEnv.NewLine}C{SysEnv.NewLine}");
        result.StandardError.ShouldBeEmpty();
    }

    /// <summary>Decodes both Git streams as UTF-8 independently of the parent's console code page.</summary>
    [TestCase(65001, TestName = "CapturePreservesUnicodeWithUtf8Console")]
    [TestCase(1252, TestName = "CapturePreservesUnicodeWithWesternConsole")]
    [NonParallelizable]
    public void CapturePreservesUnicode(int codePage)
    {
        using var repository = new GitTestRepository();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var originalEncoding = Console.OutputEncoding;
        GitCliResult result;
        try
        {
            Console.OutputEncoding = Encoding.GetEncoding(codePage);
            result = Capture(repository, "printf 'Grüße 世界'; printf 'échec' >&2");
        }
        finally
        {
            Console.OutputEncoding = originalEncoding;
        }

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldBe($"Grüße 世界{SysEnv.NewLine}");
        result.StandardError.ShouldBe($"échec{SysEnv.NewLine}");
    }

    /// <summary>Wraps process-start failures with the actionable Git installation message.</summary>
    [Test]
    public void CaptureWrapsProcessStartFailure()
    {
        using var repository = new GitTestRepository();
        var executor = new GitCliExecutor(NullLogger<GitCliExecutor>.Instance);

        var exception = Should.Throw<InvalidOperationException>(() =>
            executor.Execute(Path.Combine(repository.RepositoryPath, "missing-directory"), ["--version"]));

        exception.Message.ShouldBe("The 'git' executable could not be started. Ensure Git is installed and available on the PATH.");
        exception.InnerException.ShouldBeOfType<System.ComponentModel.Win32Exception>();
    }

    /// <summary>Runs a Git shell alias in the fixture repository to exercise redirected process output.</summary>
    private static GitCliResult Capture(GitTestRepository repository, string script) =>
        new GitCliExecutor(NullLogger<GitCliExecutor>.Instance)
            .Execute(repository.RepositoryPath, ["-c", $"alias.capture=!{script}", "capture"]);
}
