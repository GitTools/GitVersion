namespace GitVersionTask
{
    using System.Collections.Generic;
    using System.IO;
    using GitVersion;
    using GitVersion.Helpers;

    public static class GitVersionTasks
    {
        public static GetVersion.Output GetVersion(GetVersion.Input input, TaskLogger logger)
        {
            if (!GitVersionTaskUtils.GetVersionVariables(input, out var versionVariables))
            {
                return null;
            }

            var outputType = typeof(GetVersion.Output);
            var output = new GetVersion.Output();
            foreach (var variable in versionVariables)
            {
                outputType.GetProperty(variable.Key)?.SetValue(output, variable.Value, null);
            }

            return output;
        }

        public static UpdateAssemblyInfo.Output UpdateAssemblyInfo(UpdateAssemblyInfo.Input input, TaskLogger logger)
        {
            FileHelper.DeleteTempFiles();

            FileHelper.CheckForInvalidFiles(input.CompileFiles, input.ProjectFile);

            if (!GitVersionTaskUtils.GetVersionVariables(input, out var versionVariables))
            {
                return null;
            }

            return CreateTempAssemblyInfo(input, versionVariables);
        }

        public static WriteVersionInfoToBuildLog.Output WriteVersionInfoToBuildLog(WriteVersionInfoToBuildLog.Input input, TaskLogger logger)
        {
            if (!GitVersionTaskUtils.GetVersionVariables(input, out var versionVariables))
            {
                return null;
            }

            return WriteIntegrationParameters(logger, BuildServerList.GetApplicableBuildServers(), versionVariables);
        }

        public static GenerateGitVersionInformation.Output GenerateGitVersionInformation(GenerateGitVersionInformation.Input input, TaskLogger logger)
        {
            if (!GitVersionTaskUtils.GetVersionVariables(input, out var versionVariables))
            {
                return null;
            }

            return CreateGitVersionInfo(input, versionVariables);
        }

        private static UpdateAssemblyInfo.Output CreateTempAssemblyInfo(UpdateAssemblyInfo.Input input, VersionVariables versionVariables)
        {
            var fileWriteInfo = input.IntermediateOutputPath.GetFileWriteInfo(
                input.Language,
                input.ProjectFile,
                (pf, ext) => $"AssemblyInfo.g.{ext}",
                (pf, ext) => $"AssemblyInfo_{Path.GetFileNameWithoutExtension(pf)}_{Path.GetRandomFileName()}.g.{ext}"
            );

            var output = new UpdateAssemblyInfo.Output
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

        private static WriteVersionInfoToBuildLog.Output WriteIntegrationParameters(TaskLogger logger, IEnumerable<IBuildServer> applicableBuildServers, VersionVariables versionVariables)
        {
            foreach (var buildServer in applicableBuildServers)
            {
                logger.LogInfo($"Executing GenerateSetVersionMessage for '{ buildServer.GetType().Name }'.");
                logger.LogInfo(buildServer.GenerateSetVersionMessage(versionVariables));
                logger.LogInfo($"Executing GenerateBuildLogOutput for '{ buildServer.GetType().Name }'.");
                foreach (var buildParameter in BuildOutputFormatter.GenerateBuildLogOutput(buildServer, versionVariables))
                {
                    logger.LogInfo(buildParameter);
                }
            }
            return new WriteVersionInfoToBuildLog.Output();
        }

        private static GenerateGitVersionInformation.Output CreateGitVersionInfo(GenerateGitVersionInformation.Input input, VersionVariables versionVariables)
        {
            var fileWriteInfo = input.IntermediateOutputPath.GetFileWriteInfo(
                input.Language,
                input.ProjectFile,
                (pf, ext) => $"GitVersionInformation.g.{ext}",
                (pf, ext) => $"GitVersionInformation_{Path.GetFileNameWithoutExtension(pf)}_{Path.GetRandomFileName()}.g.{ext}"
            );

            var output = new GenerateGitVersionInformation.Output
            {
                GitVersionInformationFilePath = Path.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName)
            };
            var generator = new GitVersionInformationGenerator(fileWriteInfo.FileName, fileWriteInfo.WorkingDirectory, versionVariables, new FileSystem());
            generator.Generate();

            return output;
        }
    }
}
