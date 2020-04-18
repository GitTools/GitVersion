using System;
using System.Linq;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.Cache;
using GitVersion.VersionConverters.AssemblyInfo;
using GitVersion.VersionConverters.GitVersionInfo;
using GitVersion.VersionConverters.OutputGenerator;
using GitVersion.VersionConverters.WixUpdater;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    public class GitVersionTool : IGitVersionTool
    {
        private readonly ILog log;
        private readonly IGitVersionCache gitVersionCache;
        private readonly INextVersionCalculator nextVersionCalculator;
        private readonly IVariableProvider variableProvider;
        private readonly IGitPreparer gitPreparer;
        private readonly IGitVersionCacheKeyFactory cacheKeyFactory;
        private readonly IOutputGenerator outputGenerator;
        private readonly IWixVersionFileUpdater wixVersionFileUpdater;
        private readonly IGitVersionInfoGenerator gitVersionInfoGenerator;
        private readonly IAssemblyInfoFileUpdater assemblyInfoFileUpdater;

        private readonly IOptions<GitVersionOptions> options;
        private readonly Lazy<GitVersionContext> versionContext;
        private GitVersionContext context => versionContext.Value;

        public GitVersionTool(ILog log, INextVersionCalculator nextVersionCalculator, IVariableProvider variableProvider, IGitPreparer gitPreparer,
            IGitVersionCache gitVersionCache, IGitVersionCacheKeyFactory cacheKeyFactory,
            IOutputGenerator outputGenerator, IWixVersionFileUpdater wixVersionFileUpdater, IGitVersionInfoGenerator gitVersionInfoGenerator, IAssemblyInfoFileUpdater assemblyInfoFileUpdater,
            IOptions<GitVersionOptions> options, Lazy<GitVersionContext> versionContext)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));

            this.nextVersionCalculator = nextVersionCalculator ?? throw new ArgumentNullException(nameof(nextVersionCalculator));
            this.variableProvider = variableProvider ?? throw new ArgumentNullException(nameof(variableProvider));
            this.gitPreparer = gitPreparer ?? throw new ArgumentNullException(nameof(gitPreparer));

            this.cacheKeyFactory = cacheKeyFactory ?? throw new ArgumentNullException(nameof(cacheKeyFactory));
            this.gitVersionCache = gitVersionCache ?? throw new ArgumentNullException(nameof(gitVersionCache));

            this.outputGenerator = outputGenerator ?? throw new ArgumentNullException(nameof(outputGenerator));
            this.wixVersionFileUpdater = wixVersionFileUpdater ?? throw new ArgumentNullException(nameof(wixVersionFileUpdater));
            this.gitVersionInfoGenerator = gitVersionInfoGenerator ?? throw new ArgumentNullException(nameof(gitVersionInfoGenerator));
            this.assemblyInfoFileUpdater = assemblyInfoFileUpdater ?? throw new ArgumentNullException(nameof(gitVersionInfoGenerator));

            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.versionContext = versionContext ?? throw new ArgumentNullException(nameof(versionContext));
        }

        public VersionVariables CalculateVersionVariables()
        {
            gitPreparer.Prepare(); //we need to prepare the repository before using it for version calculation

            var gitVersionOptions = options.Value;

            var cacheKey = cacheKeyFactory.Create(gitVersionOptions.ConfigInfo.OverrideConfig);
            var versionVariables = gitVersionOptions.Settings.NoCache ? default : gitVersionCache.LoadVersionVariablesFromDiskCache(cacheKey);

            if (versionVariables != null) return versionVariables;

            var semanticVersion = nextVersionCalculator.FindVersion();
            versionVariables = variableProvider.GetVariablesFor(semanticVersion, context.Configuration, context.IsCurrentCommitTagged);

            if (gitVersionOptions.Settings.NoCache) return versionVariables;
            try
            {
                gitVersionCache.WriteVariablesToDiskCache(cacheKey, versionVariables);
            }
            catch (AggregateException e)
            {
                log.Warning($"One or more exceptions during cache write:{System.Environment.NewLine}{e}");
            }

            return versionVariables;
        }

        public void OutputVariables(VersionVariables variables)
        {
            var gitVersionOptions = options.Value;

            using (outputGenerator)
            {
                outputGenerator.Execute(variables, new OutputContext(gitVersionOptions.WorkingDirectory));
            }
        }

        public void UpdateAssemblyInfo(VersionVariables variables)
        {
            var gitVersionOptions = options.Value;

            if (gitVersionOptions.AssemblyInfo.ShouldUpdate)
            {
                using (assemblyInfoFileUpdater)
                {
                    assemblyInfoFileUpdater.Execute(variables, new AssemblyInfoContext(gitVersionOptions.WorkingDirectory, gitVersionOptions.AssemblyInfo.EnsureAssemblyInfo, gitVersionOptions.AssemblyInfo.Files.ToArray()));
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
