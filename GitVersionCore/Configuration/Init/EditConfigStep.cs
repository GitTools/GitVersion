namespace GitVersion.Configuration.Init
{
    using System.Collections.Generic;
    using GitVersion.Configuration.Init.BuildServer;
    using GitVersion.Configuration.Init.SetConfig;
    using GitVersion.Configuration.Init.Wizard;
    using GitVersion.Helpers;

    public class EditConfigStep : ConfigInitWizardStep
    {
        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory, IFileSystem fileSystem)
        {
            switch (result)
            {
                case "0":
                    return StepResult.SaveAndExit();
                case "1":
                    return StepResult.ExitWithoutSaving();

                case "2":
                    steps.Enqueue(new PickBranchingStrategyStep());
                    return StepResult.Ok();

                case "3":
                    steps.Enqueue(new ConfigureBranches());
                    return StepResult.Ok();
                case "4":
                    steps.Enqueue(new GlobalModeSetting(new EditConfigStep(), false));
                    return StepResult.Ok();
                case "5":
                    steps.Enqueue(new AssemblyVersioningSchemeSetting());
                    return StepResult.Ok();
                case "6":
                    steps.Enqueue(new SetupBuildScripts());
                    return StepResult.Ok();
            }
            return StepResult.InvalidResponseSelected();
        }

        protected override string GetPrompt(Config config, string workingDirectory, IFileSystem fileSystem)
        {
            return string.Format(@"Which would you like to change?

0) Save changes and exit
1) Exit without saving

2) Run getting started wizard

3) Branch specific configuration
4) Branch Increment mode (per commit/after tag) (Current: {0})
5) Assembly versioning scheme (Current: {1})
6) Setup build scripts", config.VersioningMode, config.AssemblyVersioningScheme);
        }

        protected override string DefaultResult
        {
            get { return null; }
        }
    }
}