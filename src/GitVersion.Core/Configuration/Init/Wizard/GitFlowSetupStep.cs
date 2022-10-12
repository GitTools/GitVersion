using GitVersion.Configuration.Init.SetConfig;
using GitVersion.Logging;

namespace GitVersion.Configuration.Init.Wizard;

public class GitFlowSetupStep : GlobalModeSetting
{
    public GitFlowSetupStep(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
    {
    }

    protected override string GetPrompt(Model.Configuration.GitVersionConfiguration configuration, string workingDirectory) => $"By default GitVersion will only increment the version of the 'develop' branch every commit, all other branches will increment when tagged{System.Environment.NewLine}{System.Environment.NewLine}" +
                                                                                   base.GetPrompt(configuration, workingDirectory);
}
