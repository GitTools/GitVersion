using System.Collections.Generic;
using GitVersion.Configuration.Init.Wizard;
using GitVersion.Logging;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration.Init.SetConfig
{
    public class SetBranchIncrementMode : ConfigInitWizardStep
    {
        private string name;
        private BranchConfig branchConfig;

        public SetBranchIncrementMode(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
        {
        }

        public SetBranchIncrementMode WithData(string name, BranchConfig branchConfig)
        {
            this.branchConfig = branchConfig;
            this.name = name;
            return this;
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
        {
            var configureBranchStep = StepFactory.CreateStep<ConfigureBranch>();
            switch (result)
            {
                case "0":
                    steps.Enqueue(configureBranchStep.WithData(name, branchConfig));
                    return StepResult.Ok();
                case "1":
                    branchConfig.VersioningMode = VersioningMode.ContinuousDelivery;
                    steps.Enqueue(configureBranchStep.WithData(name, branchConfig));
                    return StepResult.Ok();
                case "2":
                    branchConfig.VersioningMode = VersioningMode.ContinuousDeployment;
                    steps.Enqueue(configureBranchStep.WithData(name, branchConfig));
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
