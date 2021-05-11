using System.Collections.Generic;
using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion.Configuration.Init.Wizard
{
    public class PickBranchingStrategy1Step : ConfigInitWizardStep
    {
        public PickBranchingStrategy1Step(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
        {
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
        {
            switch (result.ToLower())
            {
                case "y":
                    Console.Write(@"Because you need to maintain multiple versions of your product in production at the same time, GitFlow is likely a good fit.

GitFlow allows you to have new development happening on the 'develop' branch, patch issues in old minor versions with 'hotfix/' branches and support old major versions with 'support/' branches");
                    steps.Enqueue(StepFactory.CreateStep<PickBranchingStrategyStep>());
                    return StepResult.Ok();
                case "n":
                    steps.Enqueue(StepFactory.CreateStep<PickBranchingStrategy2Step>());
                    return StepResult.Ok();
            }

            return StepResult.InvalidResponseSelected();
        }

        protected override string GetPrompt(Config config, string workingDirectory)
        {
            return @"GitVersion can try to recommend you a branching strategy based on a few questions. 

Do you need to maintain multiple versions of your application simultaneously in production? (y/n)";
        }

        protected override string DefaultResult => null;
    }
}
