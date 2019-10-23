using System;
using GitVersion.Configuration;
using GitVersion.OutputVariables;
using GitVersion.Cache;
using GitVersion.Logging;

namespace GitVersion
{
    public class GitVersionCalculator : IGitVersionCalculator
    {
        private readonly IFileSystem fileSystem;
        private readonly ILog log;
        private readonly IConfigFileLocator configFileLocator;
        private readonly IBuildServerResolver buildServerResolver;
        private readonly IGitVersionCache gitVersionCache;

        public GitVersionCalculator(IFileSystem fileSystem, ILog log, IConfigFileLocator configFileLocator, IBuildServerResolver buildServerResolver, IGitVersionCache gitVersionCache)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.log = log;
            this.configFileLocator = configFileLocator;
            this.buildServerResolver = buildServerResolver;
            this.gitVersionCache = gitVersionCache;
        }

        public VersionVariables CalculateVersionVariables(Arguments arguments)
        {
            var buildServer = buildServerResolver.Resolve();

            // Normalize if we are running on build server
            var normalizeGitDirectory = !arguments.NoNormalize && buildServer != null;
            arguments.NoFetch = arguments.NoFetch || buildServer != null && buildServer.PreventFetch();
            var shouldCleanUpRemotes = buildServer != null && buildServer.ShouldCleanUpRemotes();

            var gitPreparer = new GitPreparer(log, arguments);

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

            return GetCachedGitVersionInfo(arguments.TargetBranch, arguments.CommitId, arguments.OverrideConfig, arguments.NoCache, gitPreparer);
        }

        public bool TryCalculateVersionVariables(string directory, bool noFetch, out VersionVariables versionVariables)
        {
            try
            {
                var arguments = new Arguments
                {
                    NoFetch = noFetch,
                    TargetPath = directory
                };
                versionVariables = CalculateVersionVariables(arguments);
                return true;
            }
            catch (Exception ex)
            {
                log.Warning("Could not determine assembly version: " + ex);
                versionVariables = null;
                return false;
            }
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

        private VersionVariables GetCachedGitVersionInfo(string targetBranch, string commitId, Config overrideConfig, bool noCache, IGitPreparer gitPreparer)
        {
            var cacheKey = GitVersionCacheKeyFactory.Create(fileSystem, log, gitPreparer, configFileLocator, overrideConfig);
            var versionVariables = noCache ? default : gitVersionCache.LoadVersionVariablesFromDiskCache(gitPreparer, cacheKey);
            if (versionVariables == null)
            {
                versionVariables = ExecuteInternal(targetBranch, commitId, gitPreparer, overrideConfig);

                if (!noCache)
                {
                    try
                    {
                        gitVersionCache.WriteVariablesToDiskCache(gitPreparer, cacheKey, versionVariables);
                    }
                    catch (AggregateException e)
                    {
                        log.Warning($"One or more exceptions during cache write:{System.Environment.NewLine}{e}");
                    }
                }
            }

            return versionVariables;
        }

        private VersionVariables ExecuteInternal(string targetBranch, string commitId, IGitPreparer gitPreparer, Config overrideConfig)
        {
            var versionFinder = new GitVersionFinder();
            var configuration = ConfigurationProvider.Provide(gitPreparer, overrideConfig: overrideConfig, configFileLocator: configFileLocator);

            return gitPreparer.WithRepository(repo =>
            {
                var gitVersionContext = new GitVersionContext(repo, log, targetBranch, configuration, commitId: commitId);
                var semanticVersion = versionFinder.FindVersion(log, gitVersionContext);

                var variableProvider = new VariableProvider(log);
                return variableProvider.GetVariablesFor(semanticVersion, gitVersionContext.Configuration, gitVersionContext.IsCurrentCommitTagged);
            });
        }
    }
}
