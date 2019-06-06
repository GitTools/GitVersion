namespace GitVersionTask
{
    public class WriteVersionInfoToBuildLog
    {
        public static Output Execute(Input input) => GitVersionTasks.WriteVersionInfoToBuildLog(input);

        public sealed class Input : InputBase
        {
            // No additional inputs for this task
        }

        public sealed class Output
        {
            // No output for this task
        }
    }
}
