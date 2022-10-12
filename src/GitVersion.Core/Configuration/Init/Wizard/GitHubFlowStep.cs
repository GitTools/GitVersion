using GitVersion.Configurations.Init.SetConfig;
using GitVersion.Logging;

namespace GitVersion.Configurations.Init.Wizard;

public class GitHubFlowStep : GlobalModeSetting
{
    public GitHubFlowStep(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
    {
    }

    protected override string GetPrompt(Model.Configurations.Configuration config, string workingDirectory) => $"By default GitVersion will only increment the version when tagged{System.Environment.NewLine}{System.Environment.NewLine}" + base.GetPrompt(config, workingDirectory);
}
