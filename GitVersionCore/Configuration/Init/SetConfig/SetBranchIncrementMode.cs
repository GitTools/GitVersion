namespace GitVersion.Configuration.Init.SetConfig
{
    using System.Collections.Generic;
    using GitVersion.Configuration.Init.Wizard;
    using GitVersion.Helpers;

    public class SetBranchIncrementMode : ConfigInitWizardStep
    {
        readonly string name;
        readonly BranchConfig branchConfig;

        public SetBranchIncrementMode(string name, BranchConfig branchConfig)
        {
            this.name = name;
            this.branchConfig = branchConfig;
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory, IFileSystem fileSystem)
        {
            switch (result)
            {
                case "0":
                    steps.Enqueue(new ConfigureBranch(name, branchConfig));
                    return StepResult.Ok();
                case "1":
                    branchConfig.VersioningMode = VersioningMode.ContinuousDelivery;
                    steps.Enqueue(new ConfigureBranch(name, branchConfig));
                    return StepResult.Ok();
                case "2":
                    branchConfig.VersioningMode = VersioningMode.ContinuousDeployment;
                    steps.Enqueue(new ConfigureBranch(name, branchConfig));
                    return StepResult.Ok();
            }

            return StepResult.InvalidResponseSelected();
        }

        protected override string GetPrompt(Config config, string workingDirectory, IFileSystem fileSystem)
        {
            return string.Format(@"What do you want the increment mode for {0} to be?

0) Go Back
1) Follow SemVer and only increment when a release has been tagged (continuous delivery mode)
2) Increment based on branch config every commit (continuous deployment mode)", name);
        }

        protected override string DefaultResult
        {
            get { return "0"; }
        }
    }
}