using System.Collections.Generic;
using GitVersion.Configuration.Init.Wizard;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion.Configuration.Init.SetConfig
{
    public class AssemblyVersioningSchemeSetting : ConfigInitWizardStep
    {
        public AssemblyVersioningSchemeSetting(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
        {
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
        {
            var editConfigStep = StepFactory.CreateStep<EditConfigStep>();
            switch (result)
            {
                case "0":
                    steps.Enqueue(editConfigStep);
                    return StepResult.Ok();
                case "1":
                    config.AssemblyVersioningScheme = AssemblyVersioningScheme.Major;
                    steps.Enqueue(editConfigStep);
                    return StepResult.Ok();
                case "2":
                    config.AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinor;
                    steps.Enqueue(editConfigStep);
                    return StepResult.Ok();
                case "3":
                    config.AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch;
                    steps.Enqueue(editConfigStep);
                    return StepResult.Ok();
                case "4":
                    config.AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatchTag;
                    steps.Enqueue(editConfigStep);
                    return StepResult.Ok();
                case "5":
                    config.AssemblyVersioningScheme = AssemblyVersioningScheme.None;
                    steps.Enqueue(editConfigStep);
                    return StepResult.Ok();
            }

            return StepResult.InvalidResponseSelected();
        }

        protected override string GetPrompt(Config config, string workingDirectory)
        {
            return @"What assembly versioning scheme do you want to use:

0) Go Back
1) Major.0.0.0
2) Major.Minor.0.0
3) Major.Minor.Patch.0   (default)
4) Major.Minor.Patch.TagCount (Allows different pre-release tags to cause assembly version to change)
5) None (skip's updating AssemblyVersion)";

        }

        protected override string DefaultResult => "0";
    }
}
