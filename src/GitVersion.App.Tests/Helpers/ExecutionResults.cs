using GitVersion.Extensions;
using GitVersion.OutputVariables;
using GitVersion.Tests;

namespace GitVersion.App.Tests;

public class ExecutionResults(int exitCode, string? output, string? logContents = null)
{
    public int ExitCode { get; } = exitCode;
    public string? Output { get; } = output;
    public string? Log { get; init; } = logContents;
    public string? StandardOutput { get; init; }
    public string? StandardError { get; init; }

    public GitVersionVariables? OutputVariables
    {
        get
        {
            // Process diagnostics may interleave with JSON in the combined output.
            // In-process fixtures do not capture stdout separately, so retain their fallback.
            var capturedOutput = StandardOutput ?? Output;
            if (capturedOutput.IsNullOrWhiteSpace())
            {
                return null;
            }

            var jsonStartIndex = capturedOutput.IndexOf('{');
            var jsonEndIndex = capturedOutput.IndexOf('}');
            var json = capturedOutput.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1);

            return json.ToGitVersionVariables();
        }
    }
}
