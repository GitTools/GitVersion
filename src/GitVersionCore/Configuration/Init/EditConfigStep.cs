using System.Collections.Generic;
using GitVersion.Configuration.Init.BuildServer;
using GitVersion.Configuration.Init.SetConfig;
using GitVersion.Configuration.Init.Wizard;
using GitVersion.Common;
using GitVersion.Logging;

namespace GitVersion.Configuration.Init
{
    public class EditConfigStep : ConfigInitWizardStep
    {
        public EditConfigStep(IConsole console, IFileSystem fileSystem, ILog log) : base(console, fileSystem, log)
        {
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
        {
            switch (result)
            {
                case "0":
                    return StepResult.SaveAndExit();
                case "1":
                    return StepResult.ExitWithoutSaving();

                case "2":
                    steps.Enqueue(new PickBranchingStrategyStep(Console, FileSystem, Log));
                    return StepResult.Ok();

                case "3":
                    steps.Enqueue(new SetNextVersion(Console, FileSystem, Log));
                    return StepResult.Ok();

                case "4":
                    steps.Enqueue(new ConfigureBranches(Console, FileSystem, Log));
                    return StepResult.Ok();
                case "5":
                    steps.Enqueue(new GlobalModeSetting(new EditConfigStep(Console, FileSystem, Log), false, Console, FileSystem, Log));
                    return StepResult.Ok();
                case "6":
                    steps.Enqueue(new AssemblyVersioningSchemeSetting(Console, FileSystem, Log));
                    return StepResult.Ok();
                case "7":
                    steps.Enqueue(new SetupBuildScripts(Console, FileSystem, Log));
                    return StepResult.Ok();
            }
            return StepResult.InvalidResponseSelected();
        }

        protected override string GetPrompt(Config config, string workingDirectory)
        {
            return $@"Which would you like to change?

0) Save changes and exit
1) Exit without saving

2) Run getting started wizard

3) Set next version number
4) Branch specific configuration
5) Branch Increment mode (per commit/after tag) (Current: {config.VersioningMode})
6) Assembly versioning scheme (Current: {config.AssemblyVersioningScheme})
7) Setup build scripts";
        }

        protected override string DefaultResult => null;
    }
}
