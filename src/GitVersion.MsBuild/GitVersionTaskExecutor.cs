using System;
using System.IO;
using GitVersion.Logging;
using GitVersion.MsBuild.Tasks;
using GitVersion.OutputVariables;
using Microsoft.Extensions.Options;

namespace GitVersion.MsBuild
{
    public class GitVersionTaskExecutor : IGitVersionTaskExecutor
    {
        private readonly IFileSystem fileSystem;
        private readonly ILog log;
        private readonly IGitVersionOutputTool gitVersionOutputTool;
        private readonly IOptions<GitVersionOptions> options;
        private VersionVariables versionVariables;

        public GitVersionTaskExecutor(IFileSystem fileSystem, IGitVersionOutputTool gitVersionOutputTool, IOptions<GitVersionOptions> options, ILog log)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.log = log;
            this.gitVersionOutputTool = gitVersionOutputTool ?? throw new ArgumentNullException(nameof(gitVersionOutputTool));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public void GetVersion(GetVersion task)
        {
            versionVariables = VersionVariables.FromFile(task.VersionFile, fileSystem, log);
            var outputType = typeof(GetVersion);
            foreach (var variable in versionVariables)
            {
                outputType.GetProperty(variable.Key)?.SetValue(task, variable.Value, null);
            }
        }

        public void UpdateAssemblyInfo(UpdateAssemblyInfo task)
        {
            versionVariables = VersionVariables.FromFile(task.VersionFile, fileSystem, log);
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
            versionVariables = VersionVariables.FromFile(task.VersionFile, fileSystem, log);
            var fileWriteInfo = task.IntermediateOutputPath.GetFileWriteInfo(task.Language, task.ProjectFile, "GitVersionInformation");
            task.GitVersionInformationFilePath = Path.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName);

            var gitVersionOptions = options.Value;
            gitVersionOptions.WorkingDirectory = fileWriteInfo.WorkingDirectory;

            gitVersionOutputTool.GenerateGitVersionInformation(versionVariables, fileWriteInfo);
        }

        public void WriteVersionInfoToBuildLog(WriteVersionInfoToBuildLog task)
        {
            versionVariables = VersionVariables.FromFile(task.VersionFile, fileSystem, log);
            gitVersionOutputTool.OutputVariables(versionVariables, false);
        }
    }
}
