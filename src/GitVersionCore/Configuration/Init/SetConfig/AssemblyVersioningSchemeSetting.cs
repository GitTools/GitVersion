namespace GitVersion.Configuration.Init.SetConfig
{
    using System.Collections.Generic;
    using GitVersion.Helpers;
    using Wizard;

    public class AssemblyVersioningSchemeSetting : ConfigInitWizardStep
    {
        public AssemblyVersioningSchemeSetting(IConsole console, IFileSystem fileSystem) : base(console, fileSystem)
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
                    config.AssemblyVersioningScheme = AssemblyVersioningScheme.Major;
                    steps.Enqueue(new EditConfigStep(Console, FileSystem));
                    return StepResult.Ok();
                case "2":
                    config.AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinor;
                    steps.Enqueue(new EditConfigStep(Console, FileSystem));
                    return StepResult.Ok();
                case "3":
                    config.AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch;
                    steps.Enqueue(new EditConfigStep(Console, FileSystem));
                    return StepResult.Ok();
                case "4":
                    config.AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatchTag;
                    steps.Enqueue(new EditConfigStep(Console, FileSystem));
                    return StepResult.Ok();
                case "5":
                    config.AssemblyVersioningScheme = AssemblyVersioningScheme.None;
                    steps.Enqueue(new EditConfigStep(Console, FileSystem));
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

        protected override string DefaultResult
        {
            get { return "0"; }
        }
    }
}