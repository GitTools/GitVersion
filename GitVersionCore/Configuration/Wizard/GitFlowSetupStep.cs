namespace GitVersion
{
    using Configuration.Wizard.SetConfig;

    public class GitFlowSetupStep : GlobalModeSetting
    {
        public GitFlowSetupStep() : base(new FinishedSetupStep(), true)
        {
        }

        protected override string GetPrompt(Config config)
        {
            return "By default GitVersion will only increment the version of the 'develop' branch every commit, all other branches will increment when tagged\r\n\r\n" + base.GetPrompt(config);
        }
    }
}