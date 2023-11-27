using GitVersion.Core.Tests;
using GitVersion.OutputVariables;

namespace GitVersion.App.Tests;

public class ExecutionResults
{
    public ExecutionResults(int exitCode, string output, string? logContents)
    {
        ExitCode = exitCode;
        Output = output;
        Log = logContents;
    }

    public int ExitCode { get; }
    public string Output { get; }
    public string? Log { get; }

    public virtual GitVersionVariables OutputVariables
    {
        get
        {
            var jsonStartIndex = Output.IndexOf('{');
            var jsonEndIndex = Output.IndexOf('}');
            var json = Output.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1);

            return json.ToGitVersionVariables();
        }
    }
}
