namespace GitVersion.Configuration.Init.BuildServer
{
    using System.Collections.Generic;
    using GitVersion.Configuration.Init.Wizard;
    using GitVersion.Helpers;

    class SetupBuildScripts : ConfigInitWizardStep
    {
        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory, IFileSystem fileSystem)
        {
            switch (result)
            {
                case "0":
                    steps.Enqueue(new EditConfigStep());
                    return StepResult.Ok();
                case "1":
                    steps.Enqueue(new AppVeyorSetup());
                    return StepResult.Ok();
            }
            return StepResult.Ok();
        }

        protected override string GetPrompt(Config config, string workingDirectory, IFileSystem fileSystem)
        {
            return @"What build server are you using?

Want to see more? Contribute a pull request!

0) Go Back
1) AppVeyor";
        }

        protected override string DefaultResult
        {
            get { return "0"; }
        }
    }
}
