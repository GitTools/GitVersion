using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.MsBuild.Tasks;
using GitVersion.OutputVariables;
using Microsoft.Extensions.Options;

namespace GitVersion.MsBuild;

public class GitVersionTaskExecutor : IGitVersionTaskExecutor
{
    private readonly IFileSystem fileSystem;
    private readonly IGitVersionOutputTool gitVersionOutputTool;
    private readonly IOptions<GitVersionOptions> options;

    public GitVersionTaskExecutor(IFileSystem fileSystem, IGitVersionOutputTool gitVersionOutputTool, IOptions<GitVersionOptions> options)
    {
        this.fileSystem = fileSystem.NotNull();
        this.gitVersionOutputTool = gitVersionOutputTool.NotNull();
        this.options = options.NotNull();
    }

    public void GetVersion(GetVersion task)
    {
        var versionVariables = VersionVariables.FromFile(task.VersionFile, fileSystem);
        var outputType = typeof(GetVersion);
        foreach (var (key, value) in versionVariables)
        {
            outputType.GetProperty(key)?.SetValue(task, value, null);
        }
    }

    public void UpdateAssemblyInfo(UpdateAssemblyInfo task)
    {
        var versionVariables = VersionVariables.FromFile(task.VersionFile, fileSystem);
        FileHelper.DeleteTempFiles();
        FileHelper.CheckForInvalidFiles(task.CompileFiles, task.ProjectFile);

        if (!string.IsNullOrEmpty(task.IntermediateOutputPath))
        {
            // Ensure provided output path exists first. Fixes issue #2815.
            fileSystem.CreateDirectory(task.IntermediateOutputPath);
        }

        var fileWriteInfo = task.IntermediateOutputPath.GetFileWriteInfo(task.Language, task.ProjectFile, "AssemblyInfo");
        task.AssemblyInfoTempFilePath = PathHelper.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName);

        var gitVersionOptions = this.options.Value;
        gitVersionOptions.AssemblyInfo.UpdateAssemblyInfo = true;
        gitVersionOptions.AssemblyInfo.EnsureAssemblyInfo = true;
        gitVersionOptions.WorkingDirectory = fileWriteInfo.WorkingDirectory;
        gitVersionOptions.AssemblyInfo.Files.Add(fileWriteInfo.FileName);

        gitVersionOutputTool.UpdateAssemblyInfo(versionVariables);
    }

    public void GenerateGitVersionInformation(GenerateGitVersionInformation task)
    {
        var versionVariables = VersionVariables.FromFile(task.VersionFile, fileSystem);

        if (!string.IsNullOrEmpty(task.IntermediateOutputPath))
        {
            // Ensure provided output path exists first. Fixes issue #2815.
            fileSystem.CreateDirectory(task.IntermediateOutputPath);
        }

        var fileWriteInfo = task.IntermediateOutputPath.GetFileWriteInfo(task.Language, task.ProjectFile, "GitVersionInformation");
        task.GitVersionInformationFilePath = PathHelper.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName);

        var gitVersionOptions = this.options.Value;
        gitVersionOptions.WorkingDirectory = fileWriteInfo.WorkingDirectory;

        gitVersionOutputTool.GenerateGitVersionInformation(versionVariables, fileWriteInfo);
    }

    public void WriteVersionInfoToBuildLog(WriteVersionInfoToBuildLog task)
    {
        var versionVariables = VersionVariables.FromFile(task.VersionFile, fileSystem);
        gitVersionOutputTool.OutputVariables(versionVariables, false);
    }
}
