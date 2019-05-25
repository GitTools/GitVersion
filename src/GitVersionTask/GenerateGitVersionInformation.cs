namespace GitVersionTask
{
    using System.IO;
    using GitVersion;
    using GitVersion.Helpers;

    public static class GenerateGitVersionInformation
    {
        // This method is entrypoint for the task declared in .props file
        public static Output Execute(Input input) => GitVersionTaskUtils.ExecuteGitVersionTask(input, InnerExecute);

        private static Output InnerExecute(Input input, TaskLogger logger)
        {
            if (!GitVersionTaskUtils.GetVersionVariables(input, out var versionVariables))
            {
                return null;
            }

            return CreateGitVersionInfo(input, versionVariables);
        }

        private static Output CreateGitVersionInfo(Input input, VersionVariables versionVariables)
        {
            var fileWriteInfo = input.IntermediateOutputPath.GetFileWriteInfo(
                input.Language,
                input.ProjectFile,
                (pf, ext) => $"GitVersionInformation.g.{ext}",
                (pf, ext) => $"GitVersionInformation_{Path.GetFileNameWithoutExtension(pf)}_{Path.GetRandomFileName()}.g.{ext}"
            );

            var output = new Output
            {
                GitVersionInformationFilePath = Path.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName)
            };
            var generator = new GitVersionInformationGenerator(fileWriteInfo.FileName, fileWriteInfo.WorkingDirectory, versionVariables, new FileSystem());
            generator.Generate();

            return output;
        }

        public sealed class Input : InputWithCommonAdditionalProperties
        {
        }

        public sealed class Output
        {
            public string GitVersionInformationFilePath { get; set; }
        }
    }
}
