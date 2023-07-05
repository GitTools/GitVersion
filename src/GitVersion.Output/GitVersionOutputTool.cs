using GitVersion.Extensions;
using GitVersion.Output.AssemblyInfo;
using GitVersion.Output.GitVersionInfo;
using GitVersion.Output.OutputGenerator;
using GitVersion.Output.WixUpdater;
using GitVersion.OutputVariables;

using Microsoft.Extensions.Options;

namespace GitVersion;

internal class GitVersionOutputTool : IGitVersionOutputTool
{
    private readonly GitVersionOptions gitVersionOptions;
    private readonly IOutputGenerator outputGenerator;
    private readonly IWixVersionFileUpdater wixVersionFileUpdater;
    private readonly IGitVersionInfoGenerator gitVersionInfoGenerator;
    private readonly IAssemblyInfoFileUpdater assemblyInfoFileUpdater;
    private readonly IProjectFileUpdater projectFileUpdater;

    public GitVersionOutputTool(IOptions<GitVersionOptions> options,
        IOutputGenerator outputGenerator, IWixVersionFileUpdater wixVersionFileUpdater,
        IGitVersionInfoGenerator gitVersionInfoGenerator, IAssemblyInfoFileUpdater assemblyInfoFileUpdater,
        IProjectFileUpdater projectFileUpdater)
    {
        gitVersionOptions = options.Value.NotNull();

        this.outputGenerator = outputGenerator.NotNull();

        this.wixVersionFileUpdater = wixVersionFileUpdater.NotNull();
        this.gitVersionInfoGenerator = gitVersionInfoGenerator.NotNull();
        this.assemblyInfoFileUpdater = assemblyInfoFileUpdater.NotNull();
        this.projectFileUpdater = projectFileUpdater.NotNull();
    }

    public void OutputVariables(GitVersionVariables variables, bool updateBuildNumber)
    {
        using (outputGenerator)
        {
            outputGenerator.Execute(variables, new OutputContext(gitVersionOptions.WorkingDirectory, gitVersionOptions.OutputFile, updateBuildNumber));
        }
    }

    public void UpdateAssemblyInfo(GitVersionVariables variables)
    {
        var assemblyInfoContext = new AssemblyInfoContext(gitVersionOptions.WorkingDirectory, gitVersionOptions.AssemblySettingsInfo.EnsureAssemblyInfo, gitVersionOptions.AssemblySettingsInfo.Files.ToArray());

        if (gitVersionOptions.AssemblySettingsInfo.UpdateProjectFiles)
        {
            using (projectFileUpdater)
            {
                projectFileUpdater.Execute(variables, assemblyInfoContext);
            }
        }
        else if (gitVersionOptions.AssemblySettingsInfo.UpdateAssemblyInfo)
        {
            using (assemblyInfoFileUpdater)
            {
                assemblyInfoFileUpdater.Execute(variables, assemblyInfoContext);
            }
        }
    }

    public void UpdateWixVersionFile(GitVersionVariables variables)
    {
        if (gitVersionOptions.WixInfo.UpdateWixVersionFile)
        {
            using (wixVersionFileUpdater)
            {
                wixVersionFileUpdater.Execute(variables, new WixVersionContext(gitVersionOptions.WorkingDirectory));
            }
        }
    }

    public void GenerateGitVersionInformation(GitVersionVariables variables, FileWriteInfo fileWriteInfo, string? targetNamespace = null)
    {
        using (gitVersionInfoGenerator)
        {
            gitVersionInfoGenerator.Execute(variables, new GitVersionInfoContext(gitVersionOptions.WorkingDirectory, fileWriteInfo.FileName, fileWriteInfo.FileExtension, targetNamespace));
        }
    }

    public void GenerateGitVersionInformation(GitVersionVariables variables, FileWriteInfo fileWriteInfo) => GenerateGitVersionInformation(variables, fileWriteInfo, null);
}
