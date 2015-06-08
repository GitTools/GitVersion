namespace GitVersion
{
    using System.Collections.Generic;

    public class PickBranchingStrategyStep : ConfigInitWizardStep
    {
        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config)
        {
            switch (result)
            {
                case "1":
                    steps.Enqueue(new GitFlowSetupStep());
                    break;
                case "2":
                    steps.Enqueue(new GitHubFlowStep());
                    break;
                case "3":
                    steps.Enqueue(new PickBranchingStrategy1Step());
                    break;
                default:
                    return StepResult.InvalidResponseSelected();
            }

            return StepResult.Ok();
        }

        protected override string GetPrompt(Config config)
        {
            return @"The way you will use GitVersion will change a lot based on your branching strategy. What branching strategy will you be using:

1) GitFlow (or similar)
2) GitHubFlow
3) Unsure, tell me more";
        }

        protected override string DefaultResult
        {
            get { return null; }
        }
    }
}