namespace GitVersion.Configuration.Init
{
    using System.Collections.Generic;
    using GitVersion.Configuration.Init.SetConfig;
    using GitVersion.Configuration.Init.Wizard;

    public class EditConfigStep : ConfigInitWizardStep
    {
        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config)
        {
            switch (result)
            {
                case "0":
                    return StepResult.SaveAndExit();
                case "1":
                    return StepResult.ExitWithoutSaving();
                case "2":
                    steps.Enqueue(new ConfigureBranches());
                    return StepResult.Ok();
                case "3":
                    steps.Enqueue(new GlobalModeSetting(new EditConfigStep(), false));
                    return StepResult.Ok();
                case "4":
                    steps.Enqueue(new AssemblyVersioningSchemeSetting());
                    return StepResult.Ok();
            }
            return StepResult.Ok();
        }

        protected override string GetPrompt(Config config)
        {
            return string.Format(@"Which would you like to change?

0) Save changes and exit
1) Exit without saving
2) Branch specific configuration
3) Branch Increment mode (per commit/after tag) (Current: {0})
4) Assembly versioning scheme (Current: {1})", config.VersioningMode, config.AssemblyVersioningScheme);
        }

        protected override string DefaultResult
        {
            get { return null; }
        }
    }
}