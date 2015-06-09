namespace GitVersion
{
    using System.Collections.Generic;

    public class SetBranchIncrementMode : ConfigInitWizardStep
    {
        readonly string name;
        readonly BranchConfig branchConfig;

        public SetBranchIncrementMode(string name, BranchConfig branchConfig)
        {
            this.name = name;
            this.branchConfig = branchConfig;
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config)
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

        protected override string GetPrompt(Config config)
        {
            return string.Format(@"What do you want the increment mode for {0} to be?

0) Back
1) Follow SemVer and only increment when a release has been tagged (continuous delivery mode)
2) Increment based on branch config every commit (continuous deployment mode)", name);
        }

        protected override string DefaultResult
        {
            get { return "0"; }
        }
    }
}