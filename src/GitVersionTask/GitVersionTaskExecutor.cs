using System;
using System.IO;
using GitVersion.MSBuildTask.Tasks;
using GitVersion.OutputVariables;
using Microsoft.Extensions.Options;

namespace GitVersion.MSBuildTask
{
    public class GitVersionTaskExecutor : IGitVersionTaskExecutor
    {
        private readonly IGitVersionTool gitVersionTool;
        private readonly IOptions<GitVersionOptions> options;
        private VersionVariables versionVariables;

        public GitVersionTaskExecutor(IGitVersionTool gitVersionTool, IOptions<GitVersionOptions> options)
        {
            this.gitVersionTool = gitVersionTool ?? throw new ArgumentNullException(nameof(gitVersionTool));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
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
            if (task.CompileFiles != null) FileHelper.CheckForInvalidFiles(task.CompileFiles, task.ProjectFile);

            var fileWriteInfo = task.IntermediateOutputPath.GetFileWriteInfo(task.Language, task.ProjectFile, "AssemblyInfo");
            task.AssemblyInfoTempFilePath = Path.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName);

            var gitVersionOptions = options.Value;
            gitVersionOptions.AssemblyInfo.ShouldUpdate = true;
            gitVersionOptions.AssemblyInfo.EnsureAssemblyInfo = true;
            gitVersionOptions.WorkingDirectory = fileWriteInfo.WorkingDirectory;
            gitVersionOptions.AssemblyInfo.Files.Add(fileWriteInfo.FileName);
            gitVersionTool.UpdateAssemblyInfo(versionVariables);
        }

        public void GenerateGitVersionInformation(GenerateGitVersionInformation task)
        {
            var fileWriteInfo = task.IntermediateOutputPath.GetFileWriteInfo(task.Language, task.ProjectFile, "GitVersionInformation");
            task.GitVersionInformationFilePath = Path.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName);

            var gitVersionOptions = options.Value;
            gitVersionOptions.WorkingDirectory = fileWriteInfo.WorkingDirectory;

            gitVersionTool.GenerateGitVersionInformation(versionVariables, fileWriteInfo);
        }

        public void WriteVersionInfoToBuildLog(WriteVersionInfoToBuildLog task)
        {
            gitVersionTool.OutputVariables(versionVariables);
        }
    }
}
