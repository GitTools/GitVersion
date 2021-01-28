using System;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.Cache;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    public class GitVersionCalculateTool : IGitVersionCalculateTool
    {
        private readonly ILog log;
        private readonly IGitVersionCache gitVersionCache;
        private readonly INextVersionCalculator nextVersionCalculator;
        private readonly IVariableProvider variableProvider;
        private readonly IGitPreparer gitPreparer;
        private readonly IGitVersionCacheKeyFactory cacheKeyFactory;

        private readonly IOptions<GitVersionOptions> options;
        private readonly Lazy<GitVersionContext> versionContext;
        private GitVersionContext context => versionContext.Value;

        public GitVersionCalculateTool(ILog log, INextVersionCalculator nextVersionCalculator,
            IVariableProvider variableProvider, IGitPreparer gitPreparer,
            IGitVersionCache gitVersionCache, IGitVersionCacheKeyFactory cacheKeyFactory,
            IOptions<GitVersionOptions> options, Lazy<GitVersionContext> versionContext)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));

            this.nextVersionCalculator = nextVersionCalculator ?? throw new ArgumentNullException(nameof(nextVersionCalculator));
            this.variableProvider = variableProvider ?? throw new ArgumentNullException(nameof(variableProvider));
            this.gitPreparer = gitPreparer ?? throw new ArgumentNullException(nameof(gitPreparer));

            this.cacheKeyFactory = cacheKeyFactory ?? throw new ArgumentNullException(nameof(cacheKeyFactory));
            this.gitVersionCache = gitVersionCache ?? throw new ArgumentNullException(nameof(gitVersionCache));

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
    }
}
