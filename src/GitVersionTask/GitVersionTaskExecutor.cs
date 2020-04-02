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
        private readonly IOptions<Arguments> options;
        private VersionVariables versionVariables;

        public GitVersionTaskExecutor(IGitVersionTool gitVersionTool, IOptions<Arguments> options)
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

            var arguments = options.Value;
            arguments.UpdateAssemblyInfo = true;
            arguments.EnsureAssemblyInfo = true;
            arguments.TargetPath = fileWriteInfo.WorkingDirectory;
            arguments.AddAssemblyInfoFileName(fileWriteInfo.FileName);
            gitVersionTool.UpdateAssemblyInfo(versionVariables);
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
