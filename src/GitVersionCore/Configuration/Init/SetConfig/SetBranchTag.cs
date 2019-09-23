using System.Collections.Generic;
using GitVersion.Configuration.Init.Wizard;
using GitVersion.Common;
using GitVersion.Log;

namespace GitVersion.Configuration.Init.SetConfig
{
    public class SetBranchTag : ConfigInitWizardStep
    {
        string name;
        readonly BranchConfig branchConfig;

        public SetBranchTag(string name, BranchConfig branchConfig, IConsole console, IFileSystem fileSystem, ILog log)
            : base(console, fileSystem, log)
        {
            this.name = name;
            this.branchConfig = branchConfig;
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
        {
            if (string.IsNullOrWhiteSpace(result))
            {
                return StepResult.InvalidResponseSelected();
            }

            switch (result)
            {
                case "0":
                    steps.Enqueue(new ConfigureBranch(name, branchConfig, Console, FileSystem, Log));
                    return StepResult.Ok();
                case "1":
                    branchConfig.Tag = string.Empty;
                    steps.Enqueue(new ConfigureBranch(name, branchConfig, Console, FileSystem, Log));
                    return StepResult.Ok();
                default:
                    branchConfig.Tag = result;
                    steps.Enqueue(new ConfigureBranch(name, branchConfig, Console, FileSystem, Log));
                    return StepResult.Ok();
            }
        }

        protected override string GetPrompt(Config config, string workingDirectory)
        {
            return @"This sets the pre-release tag which will be used for versions on this branch (beta, rc etc)

0) Go Back
1) No tag

Anything else will be used as the tag";
        }

        protected override string DefaultResult => "0";
    }
}
