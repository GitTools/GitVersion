namespace GitVersion.Configuration.Init.BuildServer
{
    using System.Collections.Generic;
    using GitVersion.Configuration.Init.Wizard;
    using GitVersion.Helpers;

    class SetupBuildScripts : ConfigInitWizardStep
    {
        public SetupBuildScripts(IConsole console, IFileSystem fileSystem) : base(console, fileSystem)
        {
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
        {
            switch (result)
            {
                case "0":
                    steps.Enqueue(new EditConfigStep(Console, FileSystem));
                    return StepResult.Ok();
                case "1":
                    steps.Enqueue(new AppveyorPublicPrivate(Console, FileSystem));
                    return StepResult.Ok();
            }
            return StepResult.Ok();
        }

        protected override string GetPrompt(Config config, string workingDirectory)
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
