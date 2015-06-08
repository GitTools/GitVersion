namespace GitVersion
{
    using System.Collections.Generic;

    public class ConfigureBranch : ConfigInitWizardStep
    {
        string name;
        readonly BranchConfig branchConfig;

        public ConfigureBranch(string name, BranchConfig branchConfig)
        {
            this.branchConfig = branchConfig;
            this.name = name;
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config)
        {
            switch (result)
            {
                case "0":
                    steps.Enqueue(new ConfigureBranches());
                    return StepResult.Ok();
                case "1":
                    steps.Enqueue(new SetBranchTag(name, branchConfig));
                    return StepResult.Ok();
                case "2":
                    steps.Enqueue(new SetBranchIncrementMode(name, branchConfig));
                    return StepResult.Ok();
            }

            return StepResult.InvalidResponseSelected();
        }

        protected override string GetPrompt(Config config)
        {
            return string.Format(@"What would you like to change for '{0}':

0) Back
1) Branch Pre-release tag (Current: {1})
2) Branch Increment mode (per commit/after tag) (Current: {2})", name, branchConfig.Tag, branchConfig.VersioningMode);
        }

        protected override string DefaultResult
        {
            get { return "0"; }
        }
    }
}