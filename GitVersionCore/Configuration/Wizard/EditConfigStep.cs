namespace GitVersion
{
    using System.Collections.Generic;
    using GitVersion.Configuration.Wizard.SetConfig;

    public class EditConfigStep : ConfigInitWizardStep
    {
        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config)
        {
            switch (result)
            {
                case "0":
                    steps.Enqueue(new ConfigureBranches());
                    return StepResult.Ok();
                case "1":
                    steps.Enqueue(new GlobalModeSetting(new EditConfigStep(), false));
                    return StepResult.Ok();
                case "2":
                    steps.Enqueue(new AssemblyVersioningSchemeSetting());
                    return StepResult.Ok();
            }
            return StepResult.Ok();
        }

        protected override string GetPrompt(Config config)
        {
            return @"What parts of the configuration would you like to edit?

0) Branch specific configuration
1) Branch Increment mode (per commit/after tag)
2) Assembly versioning scheme";
        }

        protected override string DefaultResult
        {
            get { return null; }
        }
    }
}