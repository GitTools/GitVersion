using System;
using GitVersion.Configuration;
using GitVersion.OutputVariables;
using GitVersion.Cache;
using GitVersion.Logging;
using Microsoft.Extensions.Options;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion
{
    public class GitVersionCalculator : IGitVersionCalculator
    {
        private readonly ILog log;
        private readonly IConfigProvider configProvider;
        private readonly IBuildServerResolver buildServerResolver;
        private readonly IGitVersionCache gitVersionCache;
        private readonly INextVersionCalculator nextVersionCalculator;
        private readonly IGitPreparer gitPreparer;
        private readonly IVariableProvider variableProvider;
        private readonly IOptions<Arguments> options;
        private readonly IGitVersionCacheKeyFactory cacheKeyFactory;

        public GitVersionCalculator(ILog log, IConfigProvider configProvider, IBuildServerResolver buildServerResolver,
            IGitVersionCache gitVersionCache, INextVersionCalculator nextVersionCalculator, IGitPreparer gitPreparer, IVariableProvider variableProvider,
            IOptions<Arguments> options, IGitVersionCacheKeyFactory cacheKeyFactory)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            this.buildServerResolver = buildServerResolver ?? throw new ArgumentNullException(nameof(buildServerResolver));
            this.gitVersionCache = gitVersionCache ?? throw new ArgumentNullException(nameof(gitVersionCache));
            this.nextVersionCalculator = nextVersionCalculator ?? throw new ArgumentNullException(nameof(nextVersionCalculator));
            this.gitPreparer = gitPreparer ?? throw new ArgumentNullException(nameof(gitPreparer));
            this.variableProvider = variableProvider ?? throw new ArgumentNullException(nameof(variableProvider));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.cacheKeyFactory = cacheKeyFactory ?? throw new ArgumentNullException(nameof(cacheKeyFactory));
        }

        public VersionVariables CalculateVersionVariables()
        {
            var arguments = options.Value;
            var buildServer = buildServerResolver.Resolve();

            // Normalize if we are running on build server
            var normalizeGitDirectory = !arguments.NoNormalize && buildServer != null;
            var shouldCleanUpRemotes = buildServer != null && buildServer.ShouldCleanUpRemotes();

            var currentBranch = ResolveCurrentBranch(buildServer, arguments.TargetBranch, !string.IsNullOrWhiteSpace(arguments.DynamicRepositoryLocation));

            gitPreparer.Prepare(normalizeGitDirectory, currentBranch, shouldCleanUpRemotes);

            var dotGitDirectory = gitPreparer.GetDotGitDirectory();
            var projectRoot = gitPreparer.GetProjectRootDirectory();

            log.Info($"Project root is: {projectRoot}");
            log.Info($"DotGit directory is: {dotGitDirectory}");
            if (string.IsNullOrEmpty(dotGitDirectory) || string.IsNullOrEmpty(projectRoot))
            {
                // TODO Link to wiki article
                throw new Exception($"Failed to prepare or find the .git directory in path '{arguments.TargetPath}'.");
            }

            return GetCachedGitVersionInfo(arguments.TargetBranch, arguments.CommitId, arguments.OverrideConfig, arguments.NoCache);
        }

        private string ResolveCurrentBranch(IBuildServer buildServer, string targetBranch, bool isDynamicRepository)
        {
            if (buildServer == null)
            {
                return targetBranch;
            }

            var currentBranch = buildServer.GetCurrentBranch(isDynamicRepository) ?? targetBranch;
            log.Info("Branch from build environment: " + currentBranch);

            return currentBranch;
        }

        private VersionVariables GetCachedGitVersionInfo(string targetBranch, string commitId, Config overrideConfig, bool noCache)
        {
            var cacheKey = cacheKeyFactory.Create(overrideConfig);
            var versionVariables = noCache ? default : gitVersionCache.LoadVersionVariablesFromDiskCache(cacheKey);
            if (versionVariables == null)
            {
                versionVariables = ExecuteInternal(targetBranch, commitId, overrideConfig);

                if (!noCache)
                {
                    try
                    {
                        gitVersionCache.WriteVariablesToDiskCache(cacheKey, versionVariables);
                    }
                    catch (AggregateException e)
                    {
                        log.Warning($"One or more exceptions during cache write:{System.Environment.NewLine}{e}");
                    }
                }
            }

            return versionVariables;
        }

        private VersionVariables ExecuteInternal(string targetBranch, string commitId, Config overrideConfig)
        {
            var configuration = configProvider.Provide(overrideConfig: overrideConfig);

            return gitPreparer.GetDotGitDirectory().WithRepository(repo =>
            {
                var gitVersionContext = new GitVersionContext(repo, log, targetBranch, configuration, commitId: commitId);
                var semanticVersion = nextVersionCalculator.FindVersion(gitVersionContext);

                return variableProvider.GetVariablesFor(semanticVersion, gitVersionContext.Configuration, gitVersionContext.IsCurrentCommitTagged);
            });
        }
    }
}
