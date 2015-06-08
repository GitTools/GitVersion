namespace GitVersion
{
    using System;
    using System.Collections.Generic;

    public class PickBranchingStrategy3Step : ConfigInitWizardStep
    {
        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config)
        {
            switch (result.ToLower())
            {
                case "y":
                    Console.WriteLine("GitFlow could be a better fit than GitHubFlow for you.");
                    Console.WriteLine();
                    Console.WriteLine("GitVersion increments the SemVer for each commit on the develop branch by default, " +
                                      "this means all packages build from develop can be published to a single NuGet feed.");
                    break;
                case "n":
                    Console.WriteLine("We recommend the GitHubFlow branching strategy, it sounds like you will " +
                                      "not benefit from the additional complexity that GitFlow introduces");
                    break;
                default:
                    return StepResult.InvalidResponseSelected();
            }

            steps.Enqueue(new PickBranchingStrategyStep());
            return StepResult.Ok();
        }

        protected override string GetPrompt(Config config)
        {
            return "Do you need to build nightlies or consume packages the CI build creates without releasing those versions? (y/n)";
        }

        protected override string DefaultResult
        {
            get { return null; }
        }
    }
}