namespace GitVersion.Configuration.Init
{
    public class StepResult
    {
        private StepResult() { }

        public static StepResult Ok() => new StepResult();

        public static StepResult InvalidResponseSelected() => new StepResult
        {
            InvalidResponse = true
        };

        public static StepResult SaveAndExit() => new StepResult
        {
            Save = true,
            Exit = true
        };

        public static StepResult ExitWithoutSaving() => new StepResult
        {
            Save = false,
            Exit = true
        };

        public bool Exit { get; private set; }

        public bool Save { get; private set; }

        public bool InvalidResponse { get; private set; }
    }
}
