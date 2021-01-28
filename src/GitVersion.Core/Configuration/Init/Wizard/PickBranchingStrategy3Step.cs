using System.Collections.Generic;
using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion.Configuration.Init.Wizard
{
    public class PickBranchingStrategy3Step : ConfigInitWizardStep
    {
        public PickBranchingStrategy3Step(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
        {
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
        {
            switch (result.ToLower())
            {
                case "y":
                    Console.WriteLine("GitFlow could be a better fit than GitHubFlow for you.");
                    Console.WriteLine();
                    Console.WriteLine("GitVersion increments the SemVer for each commit on the develop branch by default, " +
                                      "this means all packages built from develop can be published to a single NuGet feed.");
                    break;
                case "n":
                    Console.WriteLine("We recommend the GitHubFlow branching strategy, it sounds like you will " +
                                      "not benefit from the additional complexity that GitFlow introduces");
                    break;
                default:
                    return StepResult.InvalidResponseSelected();
            }

            steps.Enqueue(StepFactory.CreateStep<PickBranchingStrategyStep>());
            return StepResult.Ok();
        }

        protected override string GetPrompt(Config config, string workingDirectory)
        {
            return "Do you need to build nightlies or consume packages the CI build creates without releasing those versions? (y/n)";
        }

        protected override string DefaultResult => null;
    }
}
