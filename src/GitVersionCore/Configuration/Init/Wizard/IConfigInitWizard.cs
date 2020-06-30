using GitVersion.Model.Configuration;

namespace GitVersion.Configuration.Init.Wizard
{
    public interface IConfigInitWizard
    {
        Config Run(Config config, string workingDirectory);
    }
}
