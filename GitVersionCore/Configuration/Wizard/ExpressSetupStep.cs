namespace GitVersion
{
    using System;
    using System.Collections.Generic;

    public class ExpressSetupStep : ConfigInitWizardStep
    {
        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config)
        {
            Console.WriteLine("Not done yet...");
            return StepResult.Ok();
        }

        protected override string Prompt
        {
            get { return "Not done yet"; }
        }

        protected override string DefaultResult
        {
            get { return null; }
        }
    }
}