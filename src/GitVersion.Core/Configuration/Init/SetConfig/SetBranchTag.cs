using GitVersion.Configuration.Init.Wizard;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.Configuration.Init.SetConfig;

public class SetBranchTag : ConfigInitWizardStep
{
    private string name;
    private BranchConfiguration branchConfiguration;

    public SetBranchTag(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
    {
    }

    public SetBranchTag WithData(string configName, BranchConfiguration configuration)
    {
        this.branchConfiguration = configuration;
        this.name = configName;
        return this;
    }

    protected override StepResult HandleResult(string? result, Queue<ConfigInitWizardStep> steps, GitVersionConfiguration configuration, string workingDirectory)
    {
        if (result.IsNullOrWhiteSpace())
        {
            return StepResult.InvalidResponseSelected();
        }

        var configureBranchStep = this.StepFactory.CreateStep<ConfigureBranch>();
        switch (result)
        {
            case "0":
                steps.Enqueue(configureBranchStep.WithData(this.name, this.branchConfiguration));
                return StepResult.Ok();
            case "1":
                this.branchConfiguration.Tag = string.Empty;
                steps.Enqueue(configureBranchStep.WithData(name, this.branchConfiguration));
                return StepResult.Ok();
            default:
                this.branchConfiguration.Tag = result;
                steps.Enqueue(configureBranchStep.WithData(name, this.branchConfiguration));
                return StepResult.Ok();
        }
    }

    protected override string GetPrompt(GitVersionConfiguration configuration, string workingDirectory) => @"This sets the per-release tag which will be used for versions on this branch (beta, rc etc)

0) Go Back
1) No tag

Anything else will be used as the tag";

    protected override string DefaultResult => "0";
}
