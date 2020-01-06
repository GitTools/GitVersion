using System;
using System.IO;
using GitVersion.Extensions.GitVersionInformationResources;
using GitVersion.Extensions.VersionAssemblyInfoResources;
using GitVersion.Logging;
using GitVersion.OutputFormatters;
using GitVersion.MSBuildTask.Tasks;

namespace GitVersion.MSBuildTask
{
    public class GitVersionTaskExecutor : IGitVersionTaskExecutor
    {
        private readonly IFileSystem fileSystem;
        private readonly ILog log;
        private readonly IBuildServerResolver buildServerResolver;
        private readonly IGitVersionCalculator gitVersionCalculator;

        public GitVersionTaskExecutor(IFileSystem fileSystem, ILog log, IBuildServerResolver buildServerResolver, IGitVersionCalculator gitVersionCalculator)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.buildServerResolver = buildServerResolver ?? throw new ArgumentNullException(nameof(buildServerResolver));
            this.gitVersionCalculator = gitVersionCalculator ?? throw new ArgumentNullException(nameof(gitVersionCalculator));
        }

        public void GetVersion(GetVersion task)
        {
            var versionVariables = gitVersionCalculator.CalculateVersionVariables();
            var outputType = typeof(GetVersion);
            foreach (var variable in versionVariables)
            {
                outputType.GetProperty(variable.Key)?.SetValue(task, variable.Value, null);
            }
        }

        public void UpdateAssemblyInfo(UpdateAssemblyInfo task)
        {
            FileHelper.DeleteTempFiles();
            FileHelper.CheckForInvalidFiles(task.CompileFiles, task.ProjectFile);

            var versionVariables = gitVersionCalculator.CalculateVersionVariables();
            var fileWriteInfo = task.IntermediateOutputPath.GetFileWriteInfo(task.Language, task.ProjectFile, "AssemblyInfo");

            task.AssemblyInfoTempFilePath = Path.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName);

            using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(fileWriteInfo.FileName, fileWriteInfo.WorkingDirectory, versionVariables, fileSystem, log, true);
            assemblyInfoFileUpdater.Update();
            assemblyInfoFileUpdater.CommitChanges();
        }

        public void GenerateGitVersionInformation(GenerateGitVersionInformation task)
        {
            var versionVariables = gitVersionCalculator.CalculateVersionVariables();
            var fileWriteInfo = task.IntermediateOutputPath.GetFileWriteInfo(task.Language, task.ProjectFile, "GitVersionInformation");

            task.GitVersionInformationFilePath = Path.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName);
            var generator = new GitVersionInformationGenerator(fileWriteInfo.FileName, fileWriteInfo.WorkingDirectory, versionVariables, fileSystem);
            generator.Generate();
        }

        public void WriteVersionInfoToBuildLog(WriteVersionInfoToBuildLog task)
        {
            var logger = task.Log;

            var versionVariables = gitVersionCalculator.CalculateVersionVariables();
            var buildServer = buildServerResolver.Resolve();
            if (buildServer != null)
            {
                logger.LogMessage($"Executing GenerateSetVersionMessage for '{buildServer.GetType().Name}'.");
                logger.LogMessage(buildServer.GenerateSetVersionMessage(versionVariables));
                logger.LogMessage($"Executing GenerateBuildLogOutput for '{buildServer.GetType().Name}'.");
                foreach (var buildParameter in BuildOutputFormatter.GenerateBuildLogOutput(buildServer, versionVariables))
                {
                    logger.LogMessage(buildParameter);
                }
            }
        }
    }
}
