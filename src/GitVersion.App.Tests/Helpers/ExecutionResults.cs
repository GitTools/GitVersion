using GitVersion.Core.Tests;
using GitVersion.Extensions;
using GitVersion.OutputVariables;

namespace GitVersion.App.Tests;

public class ExecutionResults(int exitCode, string? output, string? logContents = null)
{
    public int ExitCode { get; init; } = exitCode;
    public string? Output { get; init; } = output;
    public string? Log { get; init; } = logContents;

    public GitVersionVariables? OutputVariables
    {
        get
        {
            if (Output.IsNullOrWhiteSpace()) return null;

            var jsonStartIndex = Output.IndexOf('{');
            var jsonEndIndex = Output.IndexOf('}');
            var json = Output.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1);

            return json.ToGitVersionVariables();
        }
    }
}
