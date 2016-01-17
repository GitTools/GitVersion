namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    using GitVersion.Helpers;

    using LibGit2Sharp;

    using YamlDotNet.Serialization;

    public class ExecuteCore
    {
        readonly IFileSystem fileSystem;
        readonly Func<string, Func<string, VersionVariables>, VersionVariables> getOrAddFromCache;

        public ExecuteCore(IFileSystem fileSystem, Func<string, Func<string, VersionVariables>, VersionVariables> getOrAddFromCache = null)
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException("fileSystem");
            }

            this.getOrAddFromCache = getOrAddFromCache;
            this.fileSystem = fileSystem;
        }

        public VersionVariables ExecuteGitVersion(string targetUrl, string dynamicRepositoryLocation, Authentication authentication, string targetBranch, bool noFetch, string workingDirectory, string commitId)
        {
            var gitDir = Repository.Discover(workingDirectory);
            using (var repo = fileSystem.GetRepository(gitDir))
            {
                // Maybe using timestamp in .git/refs directory is enough?
                var ticks = fileSystem.GetLastDirectoryWrite(Path.Combine(gitDir, "refs"));
                var key = string.Format("{0}:{1}:{2}:{3}", gitDir, repo.Head.CanonicalName, repo.Head.Tip.Sha, ticks);

                if (getOrAddFromCache != null)
                {
                    return getOrAddFromCache(key, k =>
                    {
                        Logger.WriteInfo("Version not in memory cache. Attempting to load version from disk cache.");
                        return LoadVersionVariablesFromDiskCache(key, gitDir, targetUrl, dynamicRepositoryLocation, authentication, targetBranch, noFetch, workingDirectory, commitId);
                    });
                }

                return LoadVersionVariablesFromDiskCache(key, gitDir, targetUrl, dynamicRepositoryLocation, authentication, targetBranch, noFetch, workingDirectory, commitId);
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

        VersionVariables LoadVersionVariablesFromDiskCache(string key, string gitDir, string targetUrl, string dynamicRepositoryLocation, Authentication authentication, string targetBranch, bool noFetch, string workingDirectory, string commitId)
        {
            using (Logger.IndentLog("Loading version variables from disk cache"))
            {
                string cacheKey;
                using (var sha1 = SHA1.Create())
                {
                    // Make a shorter key by hashing, to avoid having to long cache filename.
                    cacheKey = BitConverter.ToString(sha1.ComputeHash(Encoding.UTF8.GetBytes(key))).Replace("-", "");
                }

                var cacheDir = Path.Combine(gitDir, "gitversion_cache");
                // If the cacheDir already exists, CreateDirectory just won't do anything (it won't fail). @asbjornu
                fileSystem.CreateDirectory(cacheDir);

                var cacheFileName = string.Concat(Path.Combine(cacheDir, cacheKey), ".yml");
                VersionVariables vv = null;
                if (fileSystem.Exists(cacheFileName))
                {
                    using (Logger.IndentLog("Deserializing version variables from cache file " + cacheFileName))
                    {
                        try
                        {
                            vv = VersionVariables.FromFile(cacheFileName, fileSystem);
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteWarning("Unable to read cache file " + cacheFileName + ", deleting it.");
                            Logger.WriteInfo(ex.ToString());
                            try
                            {
                                fileSystem.Delete(cacheFileName);
                            }
                            catch (Exception deleteEx)
                            {
                                Logger.WriteWarning(string.Format("Unable to delete corrupted version cache file {0}. Got {1} exception.", cacheFileName, deleteEx.GetType().FullName));
                            }
                        }
                    }
                }
                else
                {
                    Logger.WriteInfo("Cache file " + cacheFileName + " not found.");
                }

                if (vv == null)
                {
                    vv = ExecuteInternal(targetUrl, dynamicRepositoryLocation, authentication, targetBranch, noFetch, workingDirectory, commitId);
                    vv.FileName = cacheFileName;

                    using (var stream = fileSystem.OpenWrite(cacheFileName))
                    {
                        using (var sw = new StreamWriter(stream))
                        {
                            Dictionary<string, string> dictionary;
                            using (Logger.IndentLog("Creating dictionary"))
                            {
                                dictionary = vv.ToDictionary(x => x.Key, x => x.Value);
                            }

                            using (Logger.IndentLog("Storing version variables to cache file " + cacheFileName))
                            {
                                var serializer = new Serializer();
                                serializer.Serialize(sw, dictionary);
                            }
                        }
                    }
                }

                return vv;
            }
        }

        VersionVariables ExecuteInternal(string targetUrl, string dynamicRepositoryLocation, Authentication authentication, string targetBranch, bool noFetch, string workingDirectory, string commitId)
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
            VersionVariables variables;
            var versionFinder = new GitVersionFinder();
            var configuration = ConfigurationProvider.Provide(projectRoot, fileSystem);

            using (var repo = fileSystem.GetRepository(dotGitDirectory))
            {
                var gitVersionContext = new GitVersionContext(repo, configuration, commitId : commitId);
                var semanticVersion = versionFinder.FindVersion(gitVersionContext);
                variables = VariableProvider.GetVariablesFor(semanticVersion, gitVersionContext.Configuration, gitVersionContext.IsCurrentCommitTagged);
            }

            return variables;
        }
    }
}