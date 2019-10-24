using System.Collections.Generic;
using GitVersion.Logging;

namespace GitVersion.Configuration.Init.Wizard
{
    public class PickBranchingStrategyStep : ConfigInitWizardStep
    {
        public PickBranchingStrategyStep(IConsole console, IFileSystem fileSystem, ILog log) : base(console, fileSystem, log)
        {
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
        {
            var returnToStep = new FinishedSetupStep(Console, FileSystem, Log);
            switch (result)
            {
                case "1":
                    steps.Enqueue(new GitFlowSetupStep(Console, FileSystem, Log).WithData(returnToStep, true));
                    break;
                case "2":
                    steps.Enqueue(new GitHubFlowStep(Console, FileSystem, Log).WithData(returnToStep, true));
                    break;
                case "3":
                    steps.Enqueue(new PickBranchingStrategy1Step(Console, FileSystem, Log));
                    break;
                default:
                    return StepResult.InvalidResponseSelected();
            }

            return StepResult.Ok();
        }

        protected override string GetPrompt(Config config, string workingDirectory)
        {
            return @"The way you will use GitVersion will change a lot based on your branching strategy. What branching strategy will you be using:

1) GitFlow (or similar)
2) GitHubFlow
3) Unsure, tell me more";
        }

        protected override string DefaultResult => null;
    }
}
