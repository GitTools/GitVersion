namespace GitVersion.Configuration.Init.Wizard
{
    using GitVersion.Configuration.Init.SetConfig;
    using GitVersion.Helpers;

    public class GitHubFlowStep : GlobalModeSetting
    {
        public GitHubFlowStep() : base(new FinishedSetupStep(), true)
        {
        }

        protected override string GetPrompt(Config config, string workingDirectory, IFileSystem fileSystem)
        {
            return "By default GitVersion will only increment the version when tagged\r\n\r\n" + base.GetPrompt(config, workingDirectory, fileSystem);
        }
    }
}