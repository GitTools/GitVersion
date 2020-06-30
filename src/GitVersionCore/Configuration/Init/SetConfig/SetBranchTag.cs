using System.Collections.Generic;
using GitVersion.Configuration.Init.Wizard;
using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion.Configuration.Init.SetConfig
{
    public class SetBranchTag : ConfigInitWizardStep
    {
        private string name;
        private BranchConfig branchConfig;

        public SetBranchTag(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
        {
        }

        public SetBranchTag WithData(string name, BranchConfig branchConfig)
        {
            this.branchConfig = branchConfig;
            this.name = name;
            return this;
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
        {
            if (string.IsNullOrWhiteSpace(result))
            {
                return StepResult.InvalidResponseSelected();
            }

            var configureBranchStep = StepFactory.CreateStep<ConfigureBranch>();
            switch (result)
            {
                case "0":
                    steps.Enqueue(configureBranchStep.WithData(name, branchConfig));
                    return StepResult.Ok();
                case "1":
                    branchConfig.Tag = string.Empty;
                    steps.Enqueue(configureBranchStep.WithData(name, branchConfig));
                    return StepResult.Ok();
                default:
                    branchConfig.Tag = result;
                    steps.Enqueue(configureBranchStep.WithData(name, branchConfig));
                    return StepResult.Ok();
            }
        }

        protected override string GetPrompt(Config config, string workingDirectory)
        {
            return @"This sets the pre-release tag which will be used for versions on this branch (beta, rc etc)

0) Go Back
1) No tag

Anything else will be used as the tag";
        }

        protected override string DefaultResult => "0";
    }
}
