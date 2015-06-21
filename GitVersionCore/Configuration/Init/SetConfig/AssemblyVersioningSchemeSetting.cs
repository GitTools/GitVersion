namespace GitVersion.Configuration.Init.SetConfig
{
    using System.Collections.Generic;
    using GitVersion.Helpers;
    using Wizard;

    public class AssemblyVersioningSchemeSetting : ConfigInitWizardStep
    {
        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory, IFileSystem fileSystem)
        {
            switch (result)
            {
                case "0":
                    steps.Enqueue(new EditConfigStep());
                    return StepResult.Ok();
                case "1":
                    config.AssemblyVersioningScheme = AssemblyVersioningScheme.Major;
                    steps.Enqueue(new EditConfigStep());
                    return StepResult.Ok();
                case "2":
                    config.AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinor;
                    steps.Enqueue(new EditConfigStep());
                    return StepResult.Ok();
                case "3":
                    config.AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch;
                    steps.Enqueue(new EditConfigStep());
                    return StepResult.Ok();
                case "4":
                    config.AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatchTag;
                    steps.Enqueue(new EditConfigStep());
                    return StepResult.Ok();
            }

            return StepResult.InvalidResponseSelected();
        }

        protected override string GetPrompt(Config config, string workingDirectory, IFileSystem fileSystem)
        {
            return @"What assembly versioning scheme do you want to use:

0) Back
1) Major.0.0.0
2) Major.Minor.0.0
3) Major.Minor.Patch.0   (default)
4) Major.Minor.Patch.TagCount (Allows different pre-release tags to cause assembly version to change)";
        }

        protected override string DefaultResult
        {
            get { return "0"; }
        }
    }
}