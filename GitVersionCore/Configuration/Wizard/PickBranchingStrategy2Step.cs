namespace GitVersion
{
    using System;
    using System.Collections.Generic;

    public class PickBranchingStrategy2Step : ConfigInitWizardStep
    {
        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config)
        {
            switch (result.ToLower())
            {
                case "y":
                    Console.WriteLine("GitFlow is likely a good fit, the 'develop' branch can be used " +
                                      "for active development while stabilising the next release.");
                    Console.WriteLine();
                    Console.WriteLine("GitHubFlow is designed for a lightwieght workflow where master is always " +
                                      "good to deploy to production and feature branches are used to stabilise " +
                                      "features, once stable they can be released in the next release");
                    steps.Enqueue(new PickBranchingStrategyStep());
                    return StepResult.Ok();
                case "n":
                    steps.Enqueue(new PickBranchingStrategy3Step());
                    return StepResult.Ok();
            }

            return StepResult.InvalidResponseSelected();
        }

        protected override string Prompt
        {
            get { return "Do you stabilise releases while continuing work on the next version? (y/n)"; }
        }

        protected override string DefaultResult
        {
            get { return null; }
        }
    }
}