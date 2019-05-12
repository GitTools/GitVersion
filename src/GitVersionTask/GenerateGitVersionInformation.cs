namespace GitVersionTask
{
    using System;
    using System.IO;
    using GitVersion;
    using GitVersion.Helpers;

    public static class GenerateGitVersionInformation
    {
        public static Output Execute(
            Input input
            )
        {
            return GitVersionTaskBase.ExecuteGitVersionTask(
                input,
                InnerExecute
                );
        }

        private static Output InnerExecute(
            Input input,
            TaskLogger logger
            )
        {
            var execute = GitVersionTaskBase.CreateExecuteCore();
            if (!execute.TryGetVersion(input.SolutionDirectory, out var versionVariables, input.NoFetch, new Authentication()))
            {
                return null;
            }

            var fileWriteInfo = input.IntermediateOutputPath.GetWorkingDirectoryAndFileNameAndExtension(
                input.Language,
                input.ProjectFile,
                (pf, ext) => $"GitVersionInformation.g.{ext}",
                (pf, ext) => $"GitVersionInformation_{Path.GetFileNameWithoutExtension(pf)}_{Path.GetRandomFileName()}.g.{ext}"
                );

            var output = new Output()
            {
                GitVersionInformationFilePath = Path.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName)
            };
            var generator = new GitVersionInformationGenerator(fileWriteInfo.FileName, fileWriteInfo.WorkingDirectory, versionVariables, new FileSystem());
            generator.Generate();

            return output;
        }


        public sealed class Input : GitVersionTaskBase.InputWithCommonAdditionalProperties
        {
        }

        public sealed class Output
        {
            public string GitVersionInformationFilePath { get; set; }
        }

    }
}
