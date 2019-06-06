namespace GitVersionTask
{
    public static class GenerateGitVersionInformation
    {
        // This method is entrypoint for the task declared in .props file
        public static Output Execute(Input input) => GitVersionTaskUtils.ExecuteGitVersionTask(input, GitVersionTasks.GenerateGitVersionInformation);

        public sealed class Input : InputWithCommonAdditionalProperties
        {
        }

        public sealed class Output
        {
            public string GitVersionInformationFilePath { get; set; }
        }
    }
}
