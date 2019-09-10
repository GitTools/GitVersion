using GitVersion.Common;

namespace GitVersion.Configuration.Init.Wizard
{
    public class FinishedSetupStep : EditConfigStep
    {
        public FinishedSetupStep(IConsole console, IFileSystem fileSystem) : base(console, fileSystem)
        {
        }

        protected override string GetPrompt(Config config, string workingDirectory)
        {
            return "Questions are all done, you can now edit GitVersion's configuration further\r\n" + base.GetPrompt(config, workingDirectory);
        }
    }
}