using GitVersion.OutputVariables;
using GitVersion.VersionConverters.AssemblyInfo;
using GitVersion.VersionConverters.GitVersionInfo;
using GitVersion.VersionConverters.OutputGenerator;
using GitVersion.VersionConverters.WixUpdater;
using Microsoft.Extensions.Options;

namespace GitVersion;

public class GitVersionOutputTool : IGitVersionOutputTool
{
    private readonly IOptions<GitVersionOptions> options;
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
        this.options = options ?? throw new ArgumentNullException(nameof(options));

        this.outputGenerator = outputGenerator ?? throw new ArgumentNullException(nameof(outputGenerator));

        this.wixVersionFileUpdater = wixVersionFileUpdater ?? throw new ArgumentNullException(nameof(wixVersionFileUpdater));
        this.gitVersionInfoGenerator = gitVersionInfoGenerator ?? throw new ArgumentNullException(nameof(gitVersionInfoGenerator));
        this.assemblyInfoFileUpdater = assemblyInfoFileUpdater ?? throw new ArgumentNullException(nameof(gitVersionInfoGenerator));
        this.projectFileUpdater = projectFileUpdater ?? throw new ArgumentNullException(nameof(projectFileUpdater));
    }

    public void OutputVariables(VersionVariables variables, bool updateBuildNumber)
    {
        var gitVersionOptions = this.options.Value;

        using (this.outputGenerator)
        {
            this.outputGenerator.Execute(variables, new OutputContext(gitVersionOptions.WorkingDirectory, gitVersionOptions.OutputFile, updateBuildNumber));
        }
    }

    public void UpdateAssemblyInfo(VersionVariables variables)
    {
        var gitVersionOptions = this.options.Value!;
        var assemblyInfoContext = new AssemblyInfoContext(gitVersionOptions.WorkingDirectory, gitVersionOptions.AssemblyInfo.EnsureAssemblyInfo, gitVersionOptions.AssemblyInfo.Files.ToArray());

        if (gitVersionOptions.AssemblyInfo.UpdateProjectFiles)
        {
            using (this.projectFileUpdater)
            {
                this.projectFileUpdater.Execute(variables, assemblyInfoContext);
            }
        }
        else if (gitVersionOptions.AssemblyInfo.UpdateAssemblyInfo)
        {
            using (this.assemblyInfoFileUpdater)
            {
                this.assemblyInfoFileUpdater.Execute(variables, assemblyInfoContext);
            }
        }
    }

    public void UpdateWixVersionFile(VersionVariables variables)
    {
        var gitVersionOptions = this.options.Value;

        if (gitVersionOptions.WixInfo.ShouldUpdate)
        {
            using (this.wixVersionFileUpdater)
            {
                this.wixVersionFileUpdater.Execute(variables, new WixVersionContext(gitVersionOptions.WorkingDirectory));
            }
        }
    }

    public void GenerateGitVersionInformation(VersionVariables variables, FileWriteInfo fileWriteInfo)
    {
        var gitVersionOptions = this.options.Value;

        using (this.gitVersionInfoGenerator)
        {
            this.gitVersionInfoGenerator.Execute(variables, new GitVersionInfoContext(gitVersionOptions.WorkingDirectory, fileWriteInfo.FileName, fileWriteInfo.FileExtension));
        }
    }
}
