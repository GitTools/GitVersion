namespace GitVersion
{
    public class FinishedSetupStep : EditConfigStep
    {
        protected override string GetPrompt(Config config)
        {
            return "Questions are all done, you can now edit GitVersion's configuration further\r\n" + base.GetPrompt(config);
        }
    }
}