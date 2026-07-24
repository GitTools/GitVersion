using GitVersion.Extensions;

namespace GitVersion.Git;

/// <summary>Executes the <c>git</c> command-line executable and captures its output.</summary>
internal interface IGitCliExecutor
{
    /// <summary>Runs <c>git</c> with the given arguments and returns the completed result without throwing on failure.</summary>
    GitCliResult Execute(string? workingDirectory, IReadOnlyList<string> arguments);
}

/// <summary>The outcome of a single <c>git</c> invocation.</summary>
internal sealed record GitCliResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool IsSuccess => ExitCode == 0;
}

internal sealed class GitCliExecutor(ILogger<GitCliExecutor> logger) : IGitCliExecutor
{
    private readonly ILogger<GitCliExecutor> logger = logger.NotNull();

    public GitCliResult Execute(string? workingDirectory, IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (workingDirectory != null)
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // Deterministic, non-interactive invocations: never prompt for credentials
        // and keep any parsed output locale-independent.
        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
        startInfo.Environment["LC_ALL"] = "C";

        // The repository is targeted via the working directory alone, like libgit2
        // targets it via the discovered path. Inherited repo-targeting variables
        // (git exports GIT_DIR when running hooks) would redirect the invocation
        // to a different repository than the one the managed reader discovered.
        string[] repoTargetingVariables =
        [
            "GIT_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE", "GIT_COMMON_DIR",
            "GIT_NAMESPACE", "GIT_OBJECT_DIRECTORY", "GIT_ALTERNATE_OBJECT_DIRECTORIES"
        ];
        foreach (var variable in repoTargetingVariables)
        {
            startInfo.Environment.Remove(variable);
        }

        this.logger.LogDebug("Executing 'git {Arguments}' in '{WorkingDirectory}'", FormatArgumentsForLog(arguments), workingDirectory);

        using var process = new Process();
        process.StartInfo = startInfo;

        var standardOutput = new StringBuilder();
        var standardError = new StringBuilder();
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                standardOutput.AppendLine(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                standardError.AppendLine(e.Data);
            }
        };

        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or PlatformNotSupportedException)
        {
            throw new InvalidOperationException(
                "The 'git' executable could not be started. Ensure Git is installed and available on the PATH.", ex);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        var result = new GitCliResult(process.ExitCode, standardOutput.ToString(), standardError.ToString());
        if (!result.IsSuccess)
        {
            this.logger.LogDebug("'git {Arguments}' exited with code {ExitCode}: {StandardError}", FormatArgumentsForLog(arguments), result.ExitCode, result.StandardError);
        }

        return result;
    }

    /// <summary>
    /// Joins the arguments for logging, redacting credential-bearing values such as the
    /// per-invocation <c>http.*.extraHeader=Authorization: …</c> configuration.
    /// </summary>
    private static string FormatArgumentsForLog(IReadOnlyList<string> arguments) =>
        string.Join(' ', arguments.Select(static argument =>
        {
            const string marker = ".extraHeader=Authorization:";
            var index = argument.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            return index < 0 ? argument : argument[..(index + marker.Length)] + " ********";
        }));
}
