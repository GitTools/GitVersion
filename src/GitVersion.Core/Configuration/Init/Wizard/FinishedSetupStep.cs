using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion.Configuration.Init.Wizard
{
    public class FinishedSetupStep : EditConfigStep
    {
        public FinishedSetupStep(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
        {
        }

        protected override string GetPrompt(Config config, string workingDirectory)
        {
            return $"Questions are all done, you can now edit GitVersion's configuration further{System.Environment.NewLine}" + base.GetPrompt(config, workingDirectory);
        }
    }
}
