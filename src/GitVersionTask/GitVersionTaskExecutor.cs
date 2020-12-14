using System;
using System.IO;
using GitVersion.MSBuildTask.Tasks;
using GitVersion.OutputVariables;
using Microsoft.Extensions.Options;

namespace GitVersion.MSBuildTask
{
    public class GitVersionTaskExecutor : IGitVersionTaskExecutor
    {
        private readonly IGitVersionOutputTool gitVersionOutputTool;
        private readonly IOptions<GitVersionOptions> options;
        private VersionVariables versionVariables;

        public GitVersionTaskExecutor(IGitVersionCalculateTool gitVersionCalculateTool, IGitVersionOutputTool gitVersionOutputTool, IOptions<GitVersionOptions> options)
        {
            this.gitVersionOutputTool = gitVersionOutputTool ?? throw new ArgumentNullException(nameof(gitVersionOutputTool));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            versionVariables = gitVersionCalculateTool.CalculateVersionVariables();
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
            gitVersionOptions.AssemblyInfo.UpdateAssemblyInfo = true;
            gitVersionOptions.AssemblyInfo.EnsureAssemblyInfo = true;
            gitVersionOptions.WorkingDirectory = fileWriteInfo.WorkingDirectory;
            gitVersionOptions.AssemblyInfo.Files.Add(fileWriteInfo.FileName);

            gitVersionOutputTool.UpdateAssemblyInfo(versionVariables);
        }

        public void GenerateGitVersionInformation(GenerateGitVersionInformation task)
        {
            var fileWriteInfo = task.IntermediateOutputPath.GetFileWriteInfo(task.Language, task.ProjectFile, "GitVersionInformation");
            task.GitVersionInformationFilePath = Path.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName);

            var gitVersionOptions = options.Value;
            gitVersionOptions.WorkingDirectory = fileWriteInfo.WorkingDirectory;

            gitVersionOutputTool.GenerateGitVersionInformation(versionVariables, fileWriteInfo);
        }

        public void WriteVersionInfoToBuildLog(WriteVersionInfoToBuildLog task)
        {
            gitVersionOutputTool.OutputVariables(versionVariables);
        }
    }
}
