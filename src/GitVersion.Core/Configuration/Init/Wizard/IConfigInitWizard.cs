using GitVersion.Model.Configuration;

namespace GitVersion.Configuration.Init.Wizard;

public interface IConfigInitWizard
{
    GitVersionConfiguration? Run(GitVersionConfiguration configuration, string workingDirectory);
}
