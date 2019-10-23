using System.Collections.Generic;
using GitVersion.Configuration.Init.Wizard;
using GitVersion.Logging;

namespace GitVersion.Configuration.Init.BuildServer
{
    internal class SetupBuildScripts : ConfigInitWizardStep
    {
        public SetupBuildScripts(IConsole console, IFileSystem fileSystem, ILog log) : base(console, fileSystem, log)
        {
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
        {
            switch (result)
            {
                case "0":
                    steps.Enqueue(new EditConfigStep(Console, FileSystem, Log));
                    return StepResult.Ok();
                case "1":
                    steps.Enqueue(new AppveyorPublicPrivate(Console, FileSystem, Log));
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

        protected override string DefaultResult => "0";
    }
}
