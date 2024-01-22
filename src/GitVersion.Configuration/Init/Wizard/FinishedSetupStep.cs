using GitVersion.Helpers;
using GitVersion.Logging;

namespace GitVersion.Configuration.Init.Wizard;

internal class FinishedSetupStep(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory)
    : EditConfigStep(console, fileSystem, log, stepFactory)
{
    protected override string GetPrompt(ConfigurationBuilder configurationBuilder, string workingDirectory) => $"Questions are all done, you can now edit GitVersion's configuration further{PathHelper.NewLine}" + base.GetPrompt(configurationBuilder, workingDirectory);
}
