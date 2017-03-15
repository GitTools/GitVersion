namespace GitVersion.Configuration.Init.BuildServer
{
    using System.Collections.Generic;
    using GitVersion.Configuration.Init.Wizard;
    using GitVersion.Helpers;

    class AppveyorPublicPrivate : ConfigInitWizardStep
    {
        public AppveyorPublicPrivate(IConsole console, IFileSystem fileSystem) : base(console, fileSystem)
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
                    steps.Enqueue(new AppVeyorSetup(Console, FileSystem, ProjectVisibility.Public));
                    return StepResult.Ok();
                case "2":
                    steps.Enqueue(new AppVeyorSetup(Console, FileSystem, ProjectVisibility.Private));
                    return StepResult.Ok();
            }
            return StepResult.Ok();
        }

        protected override string GetPrompt(Config config, string workingDirectory)
        {
            return @"Is your project public or private?

That is ... does it require authentication to clone/pull?

0) Go Back
1) Public
2) Private";
        }

        protected override string DefaultResult
        {
            get { return "0"; }
        }
    }
}
