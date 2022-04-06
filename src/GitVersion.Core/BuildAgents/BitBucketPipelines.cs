using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.BuildAgents;

public class BitBucketPipelines : BuildAgentBase
{
    public BitBucketPipelines(IEnvironment environment, ILog log) : base(environment, log)
    {
    }

    protected override string EnvironmentVariable => "BITBUCKET_WORKSPACE";

    public override string? GenerateSetVersionMessage(VersionVariables variables) => variables.FullSemVer;

    public override string[] GenerateSetParameterMessage(string name, string value) => new[]
    {
        $"GITVERSION_{name.ToUpperInvariant()}={value}"
    };

    public override string? GetCurrentBranch(bool usingDynamicRepos)
    {
        var branchName = EvaluateEnvironmentVariable("BITBUCKET_BRANCH");
        if (branchName != null && branchName.StartsWith("refs/heads/"))
        {
            return branchName;
        }

        return null;
    }

    private string? EvaluateEnvironmentVariable(string variableName)
    {
        var branchName = Environment.GetEnvironmentVariable(variableName);
        Log.Info("Evaluating environment variable {0} : {1}", variableName, branchName!);
        return branchName;
    }
}
