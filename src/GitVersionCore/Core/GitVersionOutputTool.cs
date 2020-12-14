using System;
using System.Linq;
using GitVersion.Configuration;
using GitVersion.OutputVariables;
using GitVersion.VersionConverters.AssemblyInfo;
using GitVersion.VersionConverters.GitVersionInfo;
using GitVersion.VersionConverters.OutputGenerator;
using GitVersion.VersionConverters.WixUpdater;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    public class GitVersionOutputTool : IGitVersionOutputTool
    {
        private readonly IOptions<GitVersionOptions> options;
        private readonly IConfigProvider configProvider;
        private readonly IOutputGenerator outputGenerator;
        private readonly IWixVersionFileUpdater wixVersionFileUpdater;
        private readonly IGitVersionInfoGenerator gitVersionInfoGenerator;
        private readonly IAssemblyInfoFileUpdater assemblyInfoFileUpdater;
        private readonly IProjectFileUpdater projectFileUpdater;

        public GitVersionOutputTool(IOptions<GitVersionOptions> options, IConfigProvider configProvider,
            IOutputGenerator outputGenerator, IWixVersionFileUpdater wixVersionFileUpdater,
            IGitVersionInfoGenerator gitVersionInfoGenerator, IAssemblyInfoFileUpdater assemblyInfoFileUpdater,
            IProjectFileUpdater projectFileUpdater)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));

            this.outputGenerator = outputGenerator ?? throw new ArgumentNullException(nameof(outputGenerator));

            this.wixVersionFileUpdater = wixVersionFileUpdater ?? throw new ArgumentNullException(nameof(wixVersionFileUpdater));
            this.gitVersionInfoGenerator = gitVersionInfoGenerator ?? throw new ArgumentNullException(nameof(gitVersionInfoGenerator));
            this.assemblyInfoFileUpdater = assemblyInfoFileUpdater ?? throw new ArgumentNullException(nameof(gitVersionInfoGenerator));
            this.projectFileUpdater = projectFileUpdater ?? throw new ArgumentNullException(nameof(projectFileUpdater));
        }

        public void OutputVariables(VersionVariables variables)
        {
            var gitVersionOptions = options.Value;

            using (outputGenerator)
            {
                var configuration = configProvider.Provide(overrideConfig: options.Value.ConfigInfo.OverrideConfig);
                outputGenerator.Execute(variables, new OutputContext(gitVersionOptions.WorkingDirectory, gitVersionOptions.OutputFile, configuration.UpdateBuildNumber));
            }
        }

        public void UpdateAssemblyInfo(VersionVariables variables)
        {
            var gitVersionOptions = options.Value;
            var assemblyInfoContext = new AssemblyInfoContext(gitVersionOptions.WorkingDirectory, gitVersionOptions.AssemblyInfo.EnsureAssemblyInfo, gitVersionOptions.AssemblyInfo.Files.ToArray());

            if (gitVersionOptions.AssemblyInfo.UpdateProjectFiles)
            {
                using (projectFileUpdater)
                {
                    projectFileUpdater.Execute(variables, assemblyInfoContext);
                }
            }
            else if (gitVersionOptions.AssemblyInfo.UpdateAssemblyInfo)
            {
                using (assemblyInfoFileUpdater)
                {
                    assemblyInfoFileUpdater.Execute(variables, assemblyInfoContext);
                }
            }
        }

        public void UpdateWixVersionFile(VersionVariables variables)
        {
            var gitVersionOptions = options.Value;

            if (gitVersionOptions.WixInfo.ShouldUpdate)
            {
                using (wixVersionFileUpdater)
                {
                    wixVersionFileUpdater.Execute(variables, new WixVersionContext(gitVersionOptions.WorkingDirectory));
                }
            }
        }

        public void GenerateGitVersionInformation(VersionVariables variables, FileWriteInfo fileWriteInfo)
        {
            var gitVersionOptions = options.Value;

            using (gitVersionInfoGenerator)
            {
                gitVersionInfoGenerator.Execute(variables, new GitVersionInfoContext(gitVersionOptions.WorkingDirectory, fileWriteInfo.FileName, fileWriteInfo.FileExtension));
            }
        }
    }
}
