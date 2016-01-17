namespace GitVersion
{
    using System;
    using System.Linq;
    using GitVersion.Helpers;

    using LibGit2Sharp;

    public class ExecuteCore
    {
        readonly IFileSystem fileSystem;
        readonly GitVersionCache gitVersionCache;

        public ExecuteCore(IFileSystem fileSystem)
        {
            if (fileSystem == null) throw new ArgumentNullException("fileSystem");
            
            this.fileSystem = fileSystem;
            gitVersionCache = new GitVersionCache(fileSystem);
        }

        public VersionVariables ExecuteGitVersion(string targetUrl, string dynamicRepositoryLocation, Authentication authentication, string targetBranch, bool noFetch, string workingDirectory, string commitId)
        {
            var gitDir = Repository.Discover(workingDirectory);
            using (var repo = GetRepository(gitDir))
            {
                var versionVariables = gitVersionCache.LoadVersionVariablesFromDiskCache(repo, gitDir);
                if (versionVariables == null)
                {
                    versionVariables = ExecuteInternal(targetUrl, dynamicRepositoryLocation, authentication, targetBranch, noFetch, workingDirectory, commitId, repo);
                    gitVersionCache.WriteVariablesToDiskCache(repo, gitDir, versionVariables);
                }

                return versionVariables;
            }
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

        static string ResolveCurrentBranch(IBuildServer buildServer, string targetBranch)
        {
            if (buildServer == null)
            {
                return targetBranch;
            }

            var currentBranch = buildServer.GetCurrentBranch() ?? targetBranch;
            Logger.WriteInfo("Branch from build environment: " + currentBranch);

            return currentBranch;
        }

        VersionVariables ExecuteInternal(string targetUrl, string dynamicRepositoryLocation, Authentication authentication, string targetBranch, bool noFetch, string workingDirectory, string commitId, IRepository repo)
        {
            // Normalise if we are running on build server
            var gitPreparer = new GitPreparer(targetUrl, dynamicRepositoryLocation, authentication, noFetch, workingDirectory);
            var applicableBuildServers = BuildServerList.GetApplicableBuildServers();
            var buildServer = applicableBuildServers.FirstOrDefault();

            gitPreparer.Initialise(buildServer != null, ResolveCurrentBranch(buildServer, targetBranch));

            var dotGitDirectory = gitPreparer.GetDotGitDirectory();
            var projectRoot = gitPreparer.GetProjectRootDirectory();
            Logger.WriteInfo(string.Format("Project root is: " + projectRoot));
            if (string.IsNullOrEmpty(dotGitDirectory) || string.IsNullOrEmpty(projectRoot))
            {
                // TODO Link to wiki article
                throw new Exception(string.Format("Failed to prepare or find the .git directory in path '{0}'.", workingDirectory));
            }
            var versionFinder = new GitVersionFinder();
            var configuration = ConfigurationProvider.Provide(projectRoot, fileSystem);

            var gitVersionContext = new GitVersionContext(repo, configuration, commitId : commitId);
            var semanticVersion = versionFinder.FindVersion(gitVersionContext);

            return VariableProvider.GetVariablesFor(semanticVersion, gitVersionContext.Configuration, gitVersionContext.IsCurrentCommitTagged);
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