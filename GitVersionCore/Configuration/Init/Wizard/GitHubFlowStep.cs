namespace GitVersion.Configuration.Init.Wizard
{
    using GitVersion.Configuration.Init.SetConfig;

    public class GitHubFlowStep : GlobalModeSetting
    {
        public GitHubFlowStep() : base(new FinishedSetupStep(), true)
        {
        }

        protected override string GetPrompt(Config config)
        {
            return "By default GitVersion will only increment the version when tagged\r\n\r\n" + base.GetPrompt(config);
        }
    }
}