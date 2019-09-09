using GitVersion.Configuration.Init.SetConfig;
using GitVersion.Common;

namespace GitVersion.Configuration.Init.Wizard
{
    public class GitHubFlowStep : GlobalModeSetting
    {
        public GitHubFlowStep(IConsole console, IFileSystem fileSystem) : base(new FinishedSetupStep(console, fileSystem), true, console, fileSystem)
        {
        }

        protected override string GetPrompt(Config config, string workingDirectory)
        {
            return "By default GitVersion will only increment the version when tagged\r\n\r\n" + base.GetPrompt(config, workingDirectory);
        }
    }
}