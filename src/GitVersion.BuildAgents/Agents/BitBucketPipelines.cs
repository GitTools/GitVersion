using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal class BitBucketPipelines : BuildAgentBase
{
    public const string EnvironmentVariableName = "BITBUCKET_WORKSPACE";
    public const string BranchEnvironmentVariableName = "BITBUCKET_BRANCH";
    public const string TagEnvironmentVariableName = "BITBUCKET_TAG";
    public const string PullRequestEnvironmentVariableName = "BITBUCKET_PR_ID";
    private string? propertyFile;
    private string? ps1File;

    public BitBucketPipelines(IEnvironment environment, ILog log) : base(environment, log)
    {
        WithPropertyFile("gitversion.properties");
        WithPowershellFile("gitversion.ps1");
    }

    protected override string EnvironmentVariable => EnvironmentVariableName;

    public override string GenerateSetVersionMessage(GitVersionVariables variables) => variables.FullSemVer;

    public void WithPropertyFile(string propertiesFileName) => this.propertyFile = propertiesFileName;

    public void WithPowershellFile(string powershellFileName) => this.ps1File = powershellFileName;

    public override string[] GenerateSetParameterMessage(string name, string? value) => new[] { $"GITVERSION_{name.ToUpperInvariant()}={value}" };

    public override void WriteIntegration(Action<string?> writer, GitVersionVariables variables, bool updateBuildNumber = true)
    {
        if (this.propertyFile is null || this.ps1File is null)
            return;

        base.WriteIntegration(writer, variables, updateBuildNumber);
        writer($"Outputting variables to '{this.propertyFile}' for Bash,");
        writer($"and to '{this.ps1File}' for Powershell ... ");
        writer("To import the file into your build environment, add the following line to your build step:");
        writer($"Bash:");
        writer($"  - source {this.propertyFile}");
        writer($"Powershell:");
        writer($"  - . .\\{this.ps1File}");
        writer("");
        writer("To reuse the file across build steps, add the file as a build artifact:");
        writer($"Bash:");
        writer("  artifacts:");
        writer($"    - {this.propertyFile}");
        writer($"Powershell:");
        writer("  artifacts:");
        writer($"    - {this.ps1File}");

        var exports = variables
            .Select(variable => $"export GITVERSION_{variable.Key.ToUpperInvariant()}={variable.Value}")
            .ToList();

        File.WriteAllLines(this.propertyFile, exports);

        var psExports = variables
            .Select(variable => $"$GITVERSION_{variable.Key.ToUpperInvariant()} = \"{variable.Value}\"")
            .ToList();

        File.WriteAllLines(this.ps1File, psExports);
    }

    public override string? GetCurrentBranch(bool usingDynamicRepos)
    {
        var branchName = EvaluateEnvironmentVariable(BranchEnvironmentVariableName);
        if (branchName?.StartsWith("refs/heads/") == true)
        {
            return branchName;
        }

        return null;
    }

    private string? EvaluateEnvironmentVariable(string variableName)
    {
        var branchName = Environment.GetEnvironmentVariable(variableName);
        this.Log.Info("Evaluating environment variable {0} : {1}", variableName, branchName ?? "(null)");
        return branchName;
    }
}
