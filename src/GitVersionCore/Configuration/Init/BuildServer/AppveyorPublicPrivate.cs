using System.Collections.Generic;
using GitVersion.Configuration.Init.Wizard;
using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion.Configuration.Init.BuildServer
{
    internal class AppveyorPublicPrivate : ConfigInitWizardStep
    {
        public AppveyorPublicPrivate(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
        {
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
        {
            switch (result)
            {
                case "0":
                    steps.Enqueue(StepFactory.CreateStep<EditConfigStep>());
                    return StepResult.Ok();
                case "1":
                    steps.Enqueue(StepFactory.CreateStep<AppVeyorSetup>().WithData(ProjectVisibility.Public));
                    return StepResult.Ok();
                case "2":
                    steps.Enqueue(StepFactory.CreateStep<AppVeyorSetup>().WithData(ProjectVisibility.Private));
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

        protected override string DefaultResult => "0";
    }
}
