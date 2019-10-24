using System.Collections.Generic;
using GitVersion.Configuration.Init.Wizard;
using GitVersion.VersioningModes;
using GitVersion.Logging;

namespace GitVersion.Configuration.Init.SetConfig
{
    public class GlobalModeSetting : ConfigInitWizardStep
    {
        private ConfigInitWizardStep returnToStep;
        private bool isPartOfWizard;

        public GlobalModeSetting(IConsole console, IFileSystem fileSystem, ILog log) : base(console, fileSystem, log)
        {
        }

        public GlobalModeSetting WithData(ConfigInitWizardStep _returnToStep, bool _isPartOfWizard)
        {
            returnToStep = _returnToStep;
            isPartOfWizard = _isPartOfWizard;
            return this;
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
        {
            switch (result)
            {
                case "1":
                    config.VersioningMode = VersioningMode.ContinuousDelivery;
                    steps.Enqueue(returnToStep);
                    return StepResult.Ok();
                case "2":
                    config.VersioningMode = VersioningMode.ContinuousDeployment;
                    steps.Enqueue(returnToStep);
                    return StepResult.Ok();
                case "3":
                    config.VersioningMode = VersioningMode.Mainline;
                    steps.Enqueue(returnToStep);
                    return StepResult.Ok();
                case "0":
                case "4":
                    steps.Enqueue(returnToStep);
                    return StepResult.Ok();
            }

            return StepResult.InvalidResponseSelected();
        }

        protected override string GetPrompt(Config config, string workingDirectory)
        {
            return $@"What do you want the default increment mode to be (can be overriden per branch):
{(!isPartOfWizard ? "0) Go Back" : string.Empty)}
1) Follow SemVer and only increment when a release has been tagged (continuous delivery mode)
2) Increment based on branch config every commit (continuous deployment mode)
3) Each merged branch against master will increment the version (mainline mode)
{(isPartOfWizard ? "4) Skip" : string.Empty)}";
        }

        protected override string DefaultResult => "4";
    }
}
