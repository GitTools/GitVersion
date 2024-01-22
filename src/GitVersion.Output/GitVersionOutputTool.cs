using GitVersion.Extensions;
using GitVersion.Output.AssemblyInfo;
using GitVersion.Output.GitVersionInfo;
using GitVersion.Output.OutputGenerator;
using GitVersion.Output.WixUpdater;
using GitVersion.OutputVariables;
using Microsoft.Extensions.Options;

namespace GitVersion;

internal class GitVersionOutputTool(
    IOptions<GitVersionOptions> options,
    IOutputGenerator outputGenerator,
    IWixVersionFileUpdater wixVersionFileUpdater,
    IGitVersionInfoGenerator gitVersionInfoGenerator,
    IAssemblyInfoFileUpdater assemblyInfoFileUpdater,
    IProjectFileUpdater projectFileUpdater)
    : IGitVersionOutputTool
{
    private readonly GitVersionOptions gitVersionOptions = options.Value.NotNull();
    private readonly IOutputGenerator outputGenerator = outputGenerator.NotNull();
    private readonly IWixVersionFileUpdater wixVersionFileUpdater = wixVersionFileUpdater.NotNull();
    private readonly IGitVersionInfoGenerator gitVersionInfoGenerator = gitVersionInfoGenerator.NotNull();
    private readonly IAssemblyInfoFileUpdater assemblyInfoFileUpdater = assemblyInfoFileUpdater.NotNull();
    private readonly IProjectFileUpdater projectFileUpdater = projectFileUpdater.NotNull();

    public void OutputVariables(GitVersionVariables variables, bool updateBuildNumber)
    {
        using (this.outputGenerator)
        {
            this.outputGenerator.Execute(variables, new OutputContext(gitVersionOptions.WorkingDirectory, gitVersionOptions.OutputFile, updateBuildNumber));
        }
    }

    public void UpdateAssemblyInfo(GitVersionVariables variables)
    {
        var assemblyInfoContext = new AssemblyInfoContext(gitVersionOptions.WorkingDirectory, gitVersionOptions.AssemblySettingsInfo.EnsureAssemblyInfo, [.. gitVersionOptions.AssemblySettingsInfo.Files]);

        if (gitVersionOptions.AssemblySettingsInfo.UpdateProjectFiles)
        {
            using (this.projectFileUpdater)
            {
                this.projectFileUpdater.Execute(variables, assemblyInfoContext);
            }
        }
        else if (gitVersionOptions.AssemblySettingsInfo.UpdateAssemblyInfo)
        {
            using (this.assemblyInfoFileUpdater)
            {
                this.assemblyInfoFileUpdater.Execute(variables, assemblyInfoContext);
            }
        }
    }

    public void UpdateWixVersionFile(GitVersionVariables variables)
    {
        if (gitVersionOptions.WixInfo.UpdateWixVersionFile)
        {
            using (this.wixVersionFileUpdater)
            {
                this.wixVersionFileUpdater.Execute(variables, new WixVersionContext(gitVersionOptions.WorkingDirectory));
            }
        }
    }

    public void GenerateGitVersionInformation(GitVersionVariables variables, FileWriteInfo fileWriteInfo, string? targetNamespace = null)
    {
        using (this.gitVersionInfoGenerator)
        {
            this.gitVersionInfoGenerator.Execute(variables, new GitVersionInfoContext(gitVersionOptions.WorkingDirectory, fileWriteInfo.FileName, fileWriteInfo.FileExtension, targetNamespace));
        }
    }
}
