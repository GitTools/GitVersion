namespace GitVersion.Configurations.Init.Wizard;

public interface IConfigInitWizard
{
    Model.Configurations.Configuration? Run(Model.Configurations.Configuration config, string workingDirectory);
}
