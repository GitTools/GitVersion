using System.Collections.Generic;
using GitVersion.Configuration.Init.Wizard;
using GitVersion.Common;
using GitVersion.Logging;

namespace GitVersion.Configuration.Init.SetConfig
{
    public class ConfigureBranch : ConfigInitWizardStep
    {
        string name;
        readonly BranchConfig branchConfig;

        public ConfigureBranch(string name, BranchConfig branchConfig, IConsole console, IFileSystem fileSystem, ILog log) 
            : base(console, fileSystem, log)
        {
            this.branchConfig = branchConfig;
            this.name = name;
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
        {
            switch (result)
            {
                case "0":
                    steps.Enqueue(new ConfigureBranches(Console, FileSystem, Log));
                    return StepResult.Ok();
                case "1":
                    steps.Enqueue(new SetBranchTag(name, branchConfig, Console, FileSystem, Log));
                    return StepResult.Ok();
                case "2":
                    steps.Enqueue(new SetBranchIncrementMode(name, branchConfig, Console, FileSystem, Log));
                    return StepResult.Ok();
            }

            return StepResult.InvalidResponseSelected();
        }

        protected override string GetPrompt(Config config, string workingDirectory)
        {
            return $@"What would you like to change for '{name}':

0) Go Back
1) Branch Pre-release tag (Current: {branchConfig.Tag})
2) Branch Increment mode (per commit/after tag) (Current: {branchConfig.VersioningMode})";
        }

        protected override string DefaultResult => "0";
    }
}
