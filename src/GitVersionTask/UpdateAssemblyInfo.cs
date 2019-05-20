namespace GitVersionTask
{
    using System;
    using System.IO;

    using GitVersion;
    using GitVersion.Helpers;

    public static class UpdateAssemblyInfo
    {
        public static Output Execute(Input input)
        {
            return GitVersionTaskCommonFunctionality.ExecuteGitVersionTask(input, InnerExecute);
        }

        private static Output InnerExecute(Input input, TaskLogger logger)
        {
            var execute = GitVersionTaskCommonFunctionality.CreateExecuteCore();

            TempFileTracker.DeleteTempFiles();

            InvalidFileChecker.CheckForInvalidFiles(input.CompileFiles, input.ProjectFile);

            if (!execute.TryGetVersion(input.SolutionDirectory, out var versionVariables, input.NoFetch, new Authentication()))
            {
                return null;
            }

            return CreateTempAssemblyInfo(input, versionVariables);
        }

        private static Output CreateTempAssemblyInfo(Input input, VersionVariables versionVariables)
        {
            var fileWriteInfo = input.IntermediateOutputPath.GetFileWriteInfo(
                input.Language,
                input.ProjectFile,
                (pf, ext) => $"GitVersionTaskAssemblyInfo.g.{ext}",
                (pf, ext) => $"AssemblyInfo_{Path.GetFileNameWithoutExtension(pf)}_{Path.GetRandomFileName()}.g.{ext}"
                );

            var output = new Output()
            {
                AssemblyInfoTempFilePath = Path.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName)
            };

            using (var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(fileWriteInfo.FileName, fileWriteInfo.WorkingDirectory, versionVariables, new FileSystem(), true))
            {
                assemblyInfoFileUpdater.Update();
                assemblyInfoFileUpdater.CommitChanges();
            }

            return output;
        }

        public sealed class Input : InputWithCommonAdditionalProperties
        {
            public string[] CompileFiles { get; set; }

            protected override Boolean ValidateInput()
            {
                return base.ValidateInput()
                    && this.CompileFiles != null;
            }

        }

        public sealed class Output
        {
            public string AssemblyInfoTempFilePath { get; set; }
        }
    }
}
