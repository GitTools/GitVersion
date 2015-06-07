namespace GitVersion
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

        public bool InvalidResponse { get; private set; }
    }
}