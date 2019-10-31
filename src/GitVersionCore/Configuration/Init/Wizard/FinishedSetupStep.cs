using GitVersion.Logging;

namespace GitVersion.Configuration.Init.Wizard
{
    public class FinishedSetupStep : EditConfigStep
    {
        public FinishedSetupStep(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
        {
        }

        protected override string GetPrompt(Config config, string workingDirectory)
        {
            return "Questions are all done, you can now edit GitVersion's configuration further\r\n" + base.GetPrompt(config, workingDirectory);
        }
    }
}
