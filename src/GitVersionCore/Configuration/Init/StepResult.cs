namespace GitVersion.Configuration.Init
{
    public class StepResult
    {
        private StepResult() { }

        public static StepResult Ok()
        {
            return new StepResult();
        }

        public static StepResult InvalidResponseSelected()
        {
            return new StepResult
            {
                InvalidResponse = true
            };
        }

        public static StepResult SaveAndExit()
        {
            return new StepResult
            {
                Save = true,
                Exit = true
            };
        }

        public static StepResult ExitWithoutSaving()
        {
            return new StepResult
            {
                Save = false,
                Exit = true
            };
        }

        public bool Exit { get; private set; }

        public bool Save { get; private set; }

        public bool InvalidResponse { get; private set; }
    }
}
