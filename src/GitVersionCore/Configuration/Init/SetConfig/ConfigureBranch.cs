namespace GitVersion.Configuration.Init.SetConfig
{
    using System.Collections.Generic;
    using GitVersion.Configuration.Init.Wizard;
    using GitVersion.Helpers;

    public class ConfigureBranch : ConfigInitWizardStep
    {
        string name;
        readonly BranchConfig branchConfig;

        public ConfigureBranch(string name, BranchConfig branchConfig, IConsole console, IFileSystem fileSystem) 
            : base(console, fileSystem)
        {
            this.branchConfig = branchConfig;
            this.name = name;
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
        {
            switch (result)
            {
                case "0":
                    steps.Enqueue(new ConfigureBranches(Console, FileSystem));
                    return StepResult.Ok();
                case "1":
                    steps.Enqueue(new SetBranchTag(name, branchConfig, Console, FileSystem));
                    return StepResult.Ok();
                case "2":
                    steps.Enqueue(new SetBranchIncrementMode(name, branchConfig, Console, FileSystem));
                    return StepResult.Ok();
            }

            return StepResult.InvalidResponseSelected();
        }

        protected override string GetPrompt(Config config, string workingDirectory)
        {
            return string.Format(@"What would you like to change for '{0}':

0) Go Back
1) Branch Pre-release tag (Current: {1})
2) Branch Increment mode (per commit/after tag) (Current: {2})", name, branchConfig.Tag, branchConfig.VersioningMode);
        }

        protected override string DefaultResult
        {
            get { return "0"; }
        }
    }
}