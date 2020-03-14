using System;
using GitVersion.Cache;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;
using LibGit2Sharp;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    public class GitVersionCalculator : IGitVersionCalculator
    {
        private readonly ILog log;
        private readonly IConfigProvider configProvider;
        private readonly IGitVersionCache gitVersionCache;
        private readonly INextVersionCalculator nextVersionCalculator;
        private readonly IVariableProvider variableProvider;
        private readonly IOptions<Arguments> options;
        private readonly IGitVersionCacheKeyFactory cacheKeyFactory;

        public GitVersionCalculator(ILog log, IConfigProvider configProvider,
            IGitVersionCache gitVersionCache, INextVersionCalculator nextVersionCalculator, IVariableProvider variableProvider,
            IOptions<Arguments> options, IGitVersionCacheKeyFactory cacheKeyFactory)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            this.gitVersionCache = gitVersionCache ?? throw new ArgumentNullException(nameof(gitVersionCache));
            this.nextVersionCalculator = nextVersionCalculator ?? throw new ArgumentNullException(nameof(nextVersionCalculator));
            this.variableProvider = variableProvider ?? throw new ArgumentNullException(nameof(variableProvider));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.cacheKeyFactory = cacheKeyFactory ?? throw new ArgumentNullException(nameof(cacheKeyFactory));
        }

        public VersionVariables CalculateVersionVariables()
        {
            var arguments = options.Value;

            var cacheKey = cacheKeyFactory.Create(arguments.OverrideConfig);
            var versionVariables = arguments.NoCache ? default : gitVersionCache.LoadVersionVariablesFromDiskCache(cacheKey);

            if (versionVariables != null) return versionVariables;

            versionVariables = ExecuteInternal(arguments);

            if (arguments.NoCache) return versionVariables;
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

        private VersionVariables ExecuteInternal(Arguments arguments)
        {
            var configuration = configProvider.Provide(overrideConfig: arguments.OverrideConfig);

            using var repo = new Repository(arguments.GetDotGitDirectory());
            var targetBranch = repo.GetTargetBranch(arguments.TargetBranch);
            var gitVersionContext = new GitVersionContext(repo, log, targetBranch, configuration, commitId: arguments.CommitId);
            var semanticVersion = nextVersionCalculator.FindVersion(gitVersionContext);

            return variableProvider.GetVariablesFor(semanticVersion, gitVersionContext.Configuration, gitVersionContext.IsCurrentCommitTagged);
        }
    }
}
