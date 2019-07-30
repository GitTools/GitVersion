namespace GitVersion.Configuration.Init.SetConfig
{
    using System.Collections.Generic;
    using GitVersion.Configuration.Init.Wizard;
    using GitVersion.Helpers;

    public class SetBranchIncrementMode : ConfigInitWizardStep
    {
        readonly string name;
        readonly BranchConfig branchConfig;

        public SetBranchIncrementMode(string name, BranchConfig branchConfig, IConsole console, IFileSystem fileSystem)
            : base(console, fileSystem)
        {
            this.name = name;
            this.branchConfig = branchConfig;
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
        {
            switch (result)
            {
                case "0":
                    steps.Enqueue(new ConfigureBranch(name, branchConfig, Console, FileSystem));
                    return StepResult.Ok();
                case "1":
                    branchConfig.VersioningMode = VersioningMode.ContinuousDelivery;
                    steps.Enqueue(new ConfigureBranch(name, branchConfig, Console, FileSystem));
                    return StepResult.Ok();
                case "2":
                    branchConfig.VersioningMode = VersioningMode.ContinuousDeployment;
                    steps.Enqueue(new ConfigureBranch(name, branchConfig, Console, FileSystem));
                    return StepResult.Ok();
            }

            return StepResult.InvalidResponseSelected();
        }

        protected override string GetPrompt(Config config, string workingDirectory)
        {
            return $@"What do you want the increment mode for {name} to be?

0) Go Back
1) Follow SemVer and only increment when a release has been tagged (continuous delivery mode)
2) Increment based on branch config every commit (continuous deployment mode)";
        }

        protected override string DefaultResult => "0";
    }
}