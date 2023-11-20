namespace GitVersion.Configuration.Init.Wizard;

internal interface IConfigInitWizard
{
    IGitVersionConfiguration? Run(IGitVersionConfiguration configuration, string workingDirectory);
}
