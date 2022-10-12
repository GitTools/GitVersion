namespace GitVersion.Configuration.Init.Wizard;

public interface IConfigInitWizard
{
    Model.Configuration.GitVersionConfiguration? Run(Model.Configuration.GitVersionConfiguration configuration, string workingDirectory);
}
