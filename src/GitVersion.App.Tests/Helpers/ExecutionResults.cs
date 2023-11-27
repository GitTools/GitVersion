using GitVersion.Core.Tests;
using GitVersion.Extensions;
using GitVersion.OutputVariables;

namespace GitVersion.App.Tests;

public class ExecutionResults
{
    public ExecutionResults(int exitCode, string? output, string? logContents = null)
    {
        ExitCode = exitCode;
        Output = output;
        Log = logContents;
    }

    public int ExitCode { get; init; }
    public string? Output { get; init; }
    public string? Log { get; init; }

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
