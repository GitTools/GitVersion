namespace GitVersion.Configuration.Init.Wizard;

public interface IConfigInitWizard
{
    IGitVersionConfiguration? Run(IGitVersionConfiguration configuration, string workingDirectory);
}
