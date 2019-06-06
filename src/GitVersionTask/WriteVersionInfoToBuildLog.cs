namespace GitVersionTask
{
    public class WriteVersionInfoToBuildLog
    {
        public static Output Execute(Input input) => GitVersionTaskUtils.ExecuteGitVersionTask(input, GitVersionTasks.WriteVersionInfoToBuildLog);

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
