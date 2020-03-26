using System.Collections.Generic;
using GitVersion.Configuration.Init.Wizard;
using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion.Configuration.Init
{
    public class SetNextVersion : ConfigInitWizardStep
    {
        public SetNextVersion(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
        {
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
        {
            var editConfigStep = StepFactory.CreateStep<EditConfigStep>();
            if (string.IsNullOrEmpty(result))
            {
                steps.Enqueue(editConfigStep);
                return StepResult.Ok();
            }

            if (!SemanticVersion.TryParse(result, string.Empty, out var semVer))
                return StepResult.InvalidResponseSelected();

            config.NextVersion = semVer.ToString("t");
            steps.Enqueue(editConfigStep);
            return StepResult.Ok();
        }

        protected override string GetPrompt(Config config, string workingDirectory)
        {
            return @"What would you like to set the next version to (enter nothing to cancel)?";
        }

        protected override string DefaultResult => null;
    }
}
