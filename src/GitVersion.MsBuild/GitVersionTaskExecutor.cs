using System.IO.Abstractions;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.MsBuild.Tasks;
using GitVersion.OutputVariables;
using Microsoft.Extensions.Options;

namespace GitVersion.MsBuild;

internal class GitVersionTaskExecutor(
    IFileSystem fileSystem,
    IGitVersionOutputTool gitVersionOutputTool,
    IVersionVariableSerializer serializer,
    IConfigurationProvider configurationProvider,
    IOptions<GitVersionOptions> options)
    : IGitVersionTaskExecutor
{
    private readonly IFileSystem fileSystem = fileSystem.NotNull();
    private readonly IGitVersionOutputTool gitVersionOutputTool = gitVersionOutputTool.NotNull();
    private readonly IVersionVariableSerializer serializer = serializer.NotNull();
    private readonly IConfigurationProvider configurationProvider = configurationProvider.NotNull();
    private readonly IOptions<GitVersionOptions> options = options.NotNull();

    public void GetVersion(GetVersion task)
    {
        var versionVariables = GitVersionVariables(task);
        var outputType = typeof(GetVersion);
        foreach (var (key, value) in versionVariables)
        {
            outputType.GetProperty(key)?.SetValue(task, value, null);
        }
    }

    public void UpdateAssemblyInfo(UpdateAssemblyInfo task)
    {
        var versionVariables = GitVersionVariables(task);
        AssemblyInfoFileHelper.CheckForInvalidFiles(this.fileSystem, task.CompileFiles, task.ProjectFile);

        if (!string.IsNullOrEmpty(task.IntermediateOutputPath))
        {
            // Ensure provided output path exists first. Fixes issue #2815.
            fileSystem.Directory.CreateDirectory(task.IntermediateOutputPath);
        }

        var fileWriteInfo = task.IntermediateOutputPath.GetFileWriteInfo(task.Language, task.ProjectFile, "AssemblyInfo");
        task.AssemblyInfoTempFilePath = PathHelper.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName);

        if (!this.fileSystem.Directory.Exists(fileWriteInfo.WorkingDirectory))
        {
            this.fileSystem.Directory.CreateDirectory(fileWriteInfo.WorkingDirectory);
        }
        var gitVersionOptions = this.options.Value;
        gitVersionOptions.WorkingDirectory = fileWriteInfo.WorkingDirectory;
        gitVersionOptions.AssemblySettingsInfo.UpdateAssemblyInfo = true;
        gitVersionOptions.AssemblySettingsInfo.EnsureAssemblyInfo = true;
        gitVersionOptions.AssemblySettingsInfo.Files.Add(fileWriteInfo.FileName);

        gitVersionOutputTool.UpdateAssemblyInfo(versionVariables);
    }

    public void GenerateGitVersionInformation(GenerateGitVersionInformation task)
    {
        var versionVariables = GitVersionVariables(task);

        if (!string.IsNullOrEmpty(task.IntermediateOutputPath))
        {
            // Ensure provided output path exists first. Fixes issue #2815.
            fileSystem.Directory.CreateDirectory(task.IntermediateOutputPath);
        }

        var fileWriteInfo = task.IntermediateOutputPath.GetFileWriteInfo(task.Language, task.ProjectFile, "GitVersionInformation");
        task.GitVersionInformationFilePath = PathHelper.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName);

        if (!this.fileSystem.Directory.Exists(fileWriteInfo.WorkingDirectory))
        {
            this.fileSystem.Directory.CreateDirectory(fileWriteInfo.WorkingDirectory);
        }
        var gitVersionOptions = this.options.Value;
        gitVersionOptions.WorkingDirectory = fileWriteInfo.WorkingDirectory;
        var targetNamespace = GetTargetNamespace(task);
        gitVersionOutputTool.GenerateGitVersionInformation(versionVariables, fileWriteInfo, targetNamespace);
        return;

        static string? GetTargetNamespace(GenerateGitVersionInformation task)
        {
            string? targetNamespace = null;
            if (bool.TryParse(task.UseProjectNamespaceForGitVersionInformation, out var useTargetPathAsRootNamespace) && useTargetPathAsRootNamespace)
            {
                targetNamespace = task.RootNamespace;
                if (string.IsNullOrWhiteSpace(targetNamespace))
                {
                    targetNamespace = Path.GetFileNameWithoutExtension(task.ProjectFile);
                }
            }

            return targetNamespace;
        }
    }

    public void WriteVersionInfoToBuildLog(WriteVersionInfoToBuildLog task)
    {
        var versionVariables = GitVersionVariables(task);

        var gitVersionOptions = this.options.Value;
        var configuration = this.configurationProvider.Provide(gitVersionOptions.ConfigurationInfo.OverrideConfiguration);

        gitVersionOutputTool.OutputVariables(versionVariables, configuration.UpdateBuildNumber);
    }

    private GitVersionVariables GitVersionVariables(GitVersionTaskBase task) => serializer.FromFile(task.VersionFile);
}
