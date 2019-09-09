using GitVersion.Helpers;
using LibGit2Sharp;
using System;
using System.Linq;
using GitVersion.BuildServers;
using GitVersion.Configuration;
using GitVersion.Exceptions;
using GitVersion.OutputVariables;

namespace GitVersion
{
    public class ExecuteCore
    {
        readonly IFileSystem fileSystem;
        readonly ConfigFileLocator configFileLocator;
        readonly GitVersionCache gitVersionCache;

        public ExecuteCore(IFileSystem fileSystem, ConfigFileLocator configFileLocator = null)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.configFileLocator = configFileLocator ?? ConfigFileLocator.Default;
            gitVersionCache = new GitVersionCache(fileSystem);
        }

        public VersionVariables ExecuteGitVersion(string targetUrl, string dynamicRepositoryLocation, Authentication authentication, string targetBranch, bool noFetch, string workingDirectory, string commitId, Config overrideConfig = null, bool noCache = false, bool noNormalize = false)
        {
            // Normalise if we are running on build server
            var applicableBuildServers = BuildServerList.GetApplicableBuildServers();
            var buildServer = applicableBuildServers.FirstOrDefault();
            bool normaliseGitDirectory = !noNormalize && (buildServer != null);
            var fetch = noFetch || (buildServer != null && buildServer.PreventFetch());
            var shouldCleanUpRemotes = buildServer != null && buildServer.ShouldCleanUpRemotes();
            var gitPreparer = new GitPreparer(targetUrl, dynamicRepositoryLocation, authentication, fetch, workingDirectory);

            gitPreparer.Initialise(normaliseGitDirectory, ResolveCurrentBranch(buildServer, targetBranch, !string.IsNullOrWhiteSpace(dynamicRepositoryLocation)), shouldCleanUpRemotes);
            var dotGitDirectory = gitPreparer.GetDotGitDirectory();
            var projectRoot = gitPreparer.GetProjectRootDirectory();

            // TODO Can't use this, it still needs work
            //var gitRepository = GitRepositoryFactory.CreateRepository(new RepositoryInfo
            //{
            //    Url = targetUrl,
            //    Branch = targetBranch,
            //    Authentication = new AuthenticationInfo
            //    {
            //        Username = authentication.Username,
            //        Password = authentication.Password
            //    },
            //    Directory = workingDirectory
            //});
            Logger.WriteInfo($"Project root is: {projectRoot}");
            Logger.WriteInfo($"DotGit directory is: {dotGitDirectory}");
            if (string.IsNullOrEmpty(dotGitDirectory) || string.IsNullOrEmpty(projectRoot))
            {
                // TODO Link to wiki article
                throw new Exception($"Failed to prepare or find the .git directory in path '{workingDirectory}'.");
            }

            var cacheKey = GitVersionCacheKeyFactory.Create(fileSystem, gitPreparer, overrideConfig, configFileLocator);
            var versionVariables = noCache ? default(VersionVariables) : gitVersionCache.LoadVersionVariablesFromDiskCache(gitPreparer, cacheKey);
            if (versionVariables == null)
            {
                versionVariables = ExecuteInternal(targetBranch, commitId, gitPreparer, buildServer, overrideConfig);

                if (!noCache)
                {
                    try
                    {
                      gitVersionCache.WriteVariablesToDiskCache(gitPreparer, cacheKey, versionVariables);
                    }
                    catch (AggregateException e)
                    {
                        Logger.WriteWarning($"One or more exceptions during cache write:{Environment.NewLine}{e}");
                    }
                }
            }

            return versionVariables;
        }

        public bool TryGetVersion(string directory, out VersionVariables versionVariables, bool noFetch, Authentication authentication)
        {
            try
            {
                versionVariables = ExecuteGitVersion(null, null, authentication, null, noFetch, directory, null);
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteWarning("Could not determine assembly version: " + ex);
                versionVariables = null;
                return false;
            }
        }

        static string ResolveCurrentBranch(IBuildServer buildServer, string targetBranch, bool isDynamicRepository)
        {
            if (buildServer == null)
            {
                return targetBranch;
            }

            var currentBranch = buildServer.GetCurrentBranch(isDynamicRepository) ?? targetBranch;
            Logger.WriteInfo("Branch from build environment: " + currentBranch);

            return currentBranch;
        }

        VersionVariables ExecuteInternal(string targetBranch, string commitId, GitPreparer gitPreparer, IBuildServer buildServer, Config overrideConfig = null)
        {
            var versionFinder = new GitVersionFinder();
            var configuration = ConfigurationProvider.Provide(gitPreparer, fileSystem, overrideConfig: overrideConfig, configFileLocator: configFileLocator);

            return gitPreparer.WithRepository(repo =>
            {
                var gitVersionContext = new GitVersionContext(repo, targetBranch, configuration, commitId: commitId);
                var semanticVersion = versionFinder.FindVersion(gitVersionContext);

                return VariableProvider.GetVariablesFor(semanticVersion, gitVersionContext.Configuration, gitVersionContext.IsCurrentCommitTagged);
            });
        }

        IRepository GetRepository(string gitDirectory)
        {
            try
            {
                var repository = new Repository(gitDirectory);

                var branch = repository.Head;
                if (branch.Tip == null)
                {
                    throw new WarningException("No Tip found. Has repo been initialized?");
                }
                return repository;
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains("LibGit2Sharp.Core.NativeMethods") || exception.Message.Contains("FilePathMarshaler"))
                {
                    throw new WarningException("Restart of the process may be required to load an updated version of LibGit2Sharp.");
                }
                throw;
            }
        }
    }
}
