namespace GitVersion.Configuration.Init.Wizard
{
    using GitVersion.Helpers;

    public class FinishedSetupStep : EditConfigStep
    {
        protected override string GetPrompt(Config config, string workingDirectory, IFileSystem fileSystem)
        {
            return "Questions are all done, you can now edit GitVersion's configuration further\r\n" + base.GetPrompt(config, workingDirectory, fileSystem);
        }
    }
}