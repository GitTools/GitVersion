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
            return @"What would you like to change for config:
0) Back
1) Branch Pre-release tag
2) Branch Increment mode (per commit/after tag)";
        }

        protected override string DefaultResult
        {
            get { return "0"; }
        }
    }
}