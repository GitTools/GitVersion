using GitVersion.Configuration.Init.Wizard;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.Configuration.Init.SetConfig;

public class AssemblyVersioningSchemeSetting : ConfigInitWizardStep
{
    public AssemblyVersioningSchemeSetting(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
    {
    }

    protected override StepResult HandleResult(string? result, Queue<ConfigInitWizardStep> steps, Model.Configuration.GitVersionConfiguration configuration, string workingDirectory)
    {
        var editConfigStep = this.StepFactory.CreateStep<EditConfigStep>();
        switch (result)
        {
            case "0":
                steps.Enqueue(editConfigStep);
                return StepResult.Ok();
            case "1":
                configuration.AssemblyVersioningScheme = AssemblyVersioningScheme.Major;
                steps.Enqueue(editConfigStep);
                return StepResult.Ok();
            case "2":
                configuration.AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinor;
                steps.Enqueue(editConfigStep);
                return StepResult.Ok();
            case "3":
                configuration.AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch;
                steps.Enqueue(editConfigStep);
                return StepResult.Ok();
            case "4":
                configuration.AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatchTag;
                steps.Enqueue(editConfigStep);
                return StepResult.Ok();
            case "5":
                configuration.AssemblyVersioningScheme = AssemblyVersioningScheme.None;
                steps.Enqueue(editConfigStep);
                return StepResult.Ok();
        }

        return StepResult.InvalidResponseSelected();
    }

    protected override string GetPrompt(Model.Configuration.GitVersionConfiguration configuration, string workingDirectory) => @"What assembly versioning scheme do you want to use:

0) Go Back
1) Major.0.0.0
2) Major.Minor.0.0
3) Major.Minor.Patch.0   (default)
4) Major.Minor.Patch.TagCount (Allows different pre-release tags to cause assembly version to change)
5) None (skip's updating AssemblyVersion)";

    protected override string DefaultResult => "0";
}
