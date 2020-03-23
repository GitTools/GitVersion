using System;
using System.IO;
using GitVersion.Extensions.VersionAssemblyInfoResources;
using GitVersion.Logging;
using GitVersion.MSBuildTask.Tasks;
using GitVersion.OutputVariables;

namespace GitVersion.MSBuildTask
{
    public class GitVersionTaskExecutor : IGitVersionTaskExecutor
    {
        private readonly IFileSystem fileSystem;
        private readonly ILog log;
        private readonly IGitVersionTool gitVersionTool;
        private VersionVariables versionVariables;

        public GitVersionTaskExecutor(IFileSystem fileSystem, ILog log, IGitVersionTool gitVersionTool)
        {
            this.fileSystem = fileSystem;
            this.log = log;
            this.gitVersionTool = gitVersionTool ?? throw new ArgumentNullException(nameof(gitVersionTool));
            versionVariables = gitVersionTool.CalculateVersionVariables();
        }

        public void GetVersion(GetVersion task)
        {
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

            var fileWriteInfo = task.IntermediateOutputPath.GetFileWriteInfo(task.Language, task.ProjectFile, "AssemblyInfo");

            task.AssemblyInfoTempFilePath = Path.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName);

            using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(fileWriteInfo.FileName, fileWriteInfo.WorkingDirectory, versionVariables, fileSystem, log, true);
            assemblyInfoFileUpdater.Update();
            assemblyInfoFileUpdater.CommitChanges();

            // gitVersionTool.UpdateAssemblyInfo(versionVariables);
        }

        public void GenerateGitVersionInformation(GenerateGitVersionInformation task)
        {
            var fileWriteInfo = task.IntermediateOutputPath.GetFileWriteInfo(task.Language, task.ProjectFile, "GitVersionInformation");
            task.GitVersionInformationFilePath = Path.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName);

            gitVersionTool.GenerateGitVersionInformation(versionVariables, fileWriteInfo);
        }

        public void WriteVersionInfoToBuildLog(WriteVersionInfoToBuildLog task)
        {
            gitVersionTool.OutputVariables(versionVariables, m => task.Log.LogMessage(m));
        }
    }
}
