using System;
using System.IO;
using System.Linq;
using GitVersion.Extensions;
using GitVersion.Logging;
using LibGit2Sharp;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    public class GitPreparer : IGitPreparer
    {
        private readonly ILog log;
        private readonly IEnvironment environment;
        private readonly IOptions<GitVersionOptions> options;
        private readonly ICurrentBuildAgent buildAgent;

        private const string DefaultRemoteName = "origin";

        public GitPreparer(ILog log, IEnvironment environment, ICurrentBuildAgent buildAgent, IOptions<GitVersionOptions> options)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.buildAgent = buildAgent;
        }

        public void Prepare()
        {
            var gitVersionOptions = options.Value;

            // Normalize if we are running on build server
            var normalizeGitDirectory = !gitVersionOptions.Settings.NoNormalize && buildAgent != null;
            var shouldCleanUpRemotes = buildAgent != null && buildAgent.ShouldCleanUpRemotes();
            var currentBranch = ResolveCurrentBranch();

            var dotGitDirectory = gitVersionOptions.DotGitDirectory;
            var projectRoot = gitVersionOptions.ProjectRootDirectory;

            log.Info($"Project root is: {projectRoot}");
            log.Info($"DotGit directory is: {dotGitDirectory}");
            if (string.IsNullOrEmpty(dotGitDirectory) || string.IsNullOrEmpty(projectRoot))
            {
                throw new Exception($"Failed to prepare or find the .git directory in path '{gitVersionOptions.WorkingDirectory}'.");
            }

            PrepareInternal(normalizeGitDirectory, currentBranch, shouldCleanUpRemotes);
        }

        private void PrepareInternal(bool normalizeGitDirectory, string currentBranch, bool shouldCleanUpRemotes = false)
        {
            var gitVersionOptions = options.Value;
            if (!string.IsNullOrWhiteSpace(gitVersionOptions.RepositoryInfo.TargetUrl))
            {
                CreateDynamicRepository(currentBranch);
            }
            else
            {
                if (normalizeGitDirectory)
                {
                    if (shouldCleanUpRemotes)
                    {
                        CleanupDuplicateOrigin();
                    }

                    NormalizeGitDirectory(currentBranch, gitVersionOptions.DotGitDirectory, false);
                }
            }
        }

        private string ResolveCurrentBranch()
        {
            var gitVersionOptions = options.Value;
            var targetBranch = gitVersionOptions.RepositoryInfo.TargetBranch;
            if (buildAgent == null)
            {
                return targetBranch;
            }

            var isDynamicRepository = !string.IsNullOrWhiteSpace(gitVersionOptions.RepositoryInfo.DynamicRepositoryClonePath);
            var currentBranch = buildAgent.GetCurrentBranch(isDynamicRepository) ?? targetBranch;
            log.Info("Branch from build environment: " + currentBranch);

            return currentBranch;
        }

        private void CleanupDuplicateOrigin()
        {
            var remoteToKeep = DefaultRemoteName;

            using var repo = new Repository(options.Value.DotGitDirectory);

            // check that we have a remote that matches defaultRemoteName if not take the first remote
            if (!repo.Network.Remotes.Any(remote => remote.Name.Equals(DefaultRemoteName, StringComparison.InvariantCultureIgnoreCase)))
            {
                remoteToKeep = repo.Network.Remotes.First().Name;
            }

            var duplicateRepos = repo.Network
                                     .Remotes
                                     .Where(remote => !remote.Name.Equals(remoteToKeep, StringComparison.InvariantCultureIgnoreCase))
                                     .Select(remote => remote.Name);

            // remove all remotes that are considered duplicates
            foreach (var repoName in duplicateRepos)
            {
                repo.Network.Remotes.Remove(repoName);
            }
        }

        private void CreateDynamicRepository(string targetBranch)
        {
            var gitVersionOptions = options.Value;
            if (string.IsNullOrWhiteSpace(targetBranch))
            {
                throw new Exception("Dynamic Git repositories must have a target branch (/b)");
            }

            var repositoryInfo = gitVersionOptions.RepositoryInfo;
            var gitDirectory = gitVersionOptions.DynamicGitRepositoryPath;

            using (log.IndentLog($"Creating dynamic repository at '{gitDirectory}'"))
            {
                var authentication = gitVersionOptions.Authentication;
                if (!Directory.Exists(gitDirectory))
                {
                    CloneRepository(repositoryInfo.TargetUrl, gitDirectory, authentication);
                }
                else
                {
                    log.Info("Git repository already exists");
                }
                NormalizeGitDirectory(targetBranch, gitDirectory, true);
            }
        }

        private void NormalizeGitDirectory(string targetBranch, string gitDirectory, bool isDynamicRepository)
        {
            using (log.IndentLog($"Normalizing git directory for branch '{targetBranch}'"))
            {
                // Normalize (download branches) before using the branch
                NormalizeGitDirectory(gitDirectory, options.Value.Settings.NoFetch, targetBranch, isDynamicRepository);
            }
        }

        private void CloneRepository(string repositoryUrl, string gitDirectory, AuthenticationInfo auth)
        {
            Credentials credentials = null;

            if (auth != null)
            {
                if (!string.IsNullOrWhiteSpace(auth.Username) && !string.IsNullOrWhiteSpace(auth.Password))
                {
                    log.Info($"Setting up credentials using name '{auth.Username}'");

                    credentials = new UsernamePasswordCredentials
                    {
                        Username = auth.Username,
                        Password = auth.Password
                    };
                }
            }

            try
            {
                using (log.IndentLog($"Cloning repository from url '{repositoryUrl}'"))
                {
                    var cloneOptions = new CloneOptions
                    {
                        Checkout = false,
                        CredentialsProvider = (url, usernameFromUrl, types) => credentials
                    };

                    var returnedPath = Repository.Clone(repositoryUrl, gitDirectory, cloneOptions);
                    log.Info($"Returned path after repository clone: {returnedPath}");
                }
            }
            catch (LibGit2SharpException ex)
            {
                var message = ex.Message;
                if (message.Contains("401"))
                {
                    throw new Exception("Unauthorized: Incorrect username/password");
                }
                if (message.Contains("403"))
                {
                    throw new Exception("Forbidden: Possibly Incorrect username/password");
                }
                if (message.Contains("404"))
                {
                    throw new Exception("Not found: The repository was not found");
                }

                throw new Exception("There was an unknown problem with the Git repository you provided", ex);
            }
        }

        /// <summary>
        /// Normalization of a git directory turns all remote branches into local branches, turns pull request refs into a real branch and a few other things. This is designed to be run *only on the build server* which checks out repositories in different ways.
        /// It is not recommended to run normalization against a local repository
        /// </summary>
        private void NormalizeGitDirectory(string gitDirectory, bool noFetch, string currentBranch, bool isDynamicRepository)
        {
            var authentication = options.Value.Authentication;
            using var repository = new Repository(gitDirectory);
            // Need to ensure the HEAD does not move, this is essentially a BugCheck
            var expectedSha = repository.Head.Tip.Sha;
            var expectedBranchName = repository.Head.CanonicalName;

            try
            {
                var remote = repository.EnsureOnlyOneRemoteIsDefined(log);

                repository.AddMissingRefSpecs(log, remote);

                //If noFetch is enabled, then GitVersion will assume that the git repository is normalized before execution, so that fetching from remotes is not required.
                if (noFetch)
                {
                    log.Info("Skipping fetching, if GitVersion does not calculate your version as expected you might need to allow fetching or use dynamic repositories");
                }
                else
                {
                    log.Info($"Fetching from remote '{remote.Name}' using the following refspecs: {string.Join(", ", remote.FetchRefSpecs.Select(r => r.Specification))}.");
                    Commands.Fetch(repository, remote.Name, new string[0], authentication.ToFetchOptions(), null);
                }

                repository.EnsureLocalBranchExistsForCurrentBranch(log, remote, currentBranch);
                repository.CreateOrUpdateLocalBranchesFromRemoteTrackingOnes(log, remote.Name);

                // Bug fix for https://github.com/GitTools/GitVersion/issues/1754, head maybe have been changed
                // if this is a dynamic repository. But only allow this in case the branches are different (branch switch)
                if (expectedSha != repository.Head.Tip.Sha &&
                    (isDynamicRepository || !expectedBranchName.IsBranch(currentBranch)))
                {
                    var newExpectedSha = repository.Head.Tip.Sha;
                    var newExpectedBranchName = repository.Head.CanonicalName;

                    log.Info($"Head has moved from '{expectedBranchName} | {expectedSha}' => '{newExpectedBranchName} | {newExpectedSha}', allowed since this is a dynamic repository");

                    expectedSha = newExpectedSha;
                }

                var headSha = repository.Refs.Head.TargetIdentifier;

                if (!repository.Info.IsHeadDetached)
                {
                    log.Info($"HEAD points at branch '{headSha}'.");
                    return;
                }

                log.Info($"HEAD is detached and points at commit '{headSha}'.");
                log.Info($"Local Refs:{System.Environment.NewLine}" + string.Join(System.Environment.NewLine, repository.Refs.FromGlob("*").Select(r => $"{r.CanonicalName} ({r.TargetIdentifier})")));

                // In order to decide whether a fake branch is required or not, first check to see if any local branches have the same commit SHA of the head SHA.
                // If they do, go ahead and checkout that branch
                // If no, go ahead and check out a new branch, using the known commit SHA as the pointer
                var localBranchesWhereCommitShaIsHead = repository.Branches.Where(b => !b.IsRemote && b.Tip.Sha == headSha).ToList();

                var matchingCurrentBranch = !string.IsNullOrEmpty(currentBranch)
                    ? localBranchesWhereCommitShaIsHead.SingleOrDefault(b => b.CanonicalName.Replace("/heads/", "/") == currentBranch.Replace("/heads/", "/"))
                    : null;
                if (matchingCurrentBranch != null)
                {
                    log.Info($"Checking out local branch '{currentBranch}'.");
                    Commands.Checkout(repository, matchingCurrentBranch);
                }
                else if (localBranchesWhereCommitShaIsHead.Count > 1)
                {
                    var branchNames = localBranchesWhereCommitShaIsHead.Select(r => r.CanonicalName);
                    var csvNames = string.Join(", ", branchNames);
                    const string moveBranchMsg = "Move one of the branches along a commit to remove warning";

                    log.Warning($"Found more than one local branch pointing at the commit '{headSha}' ({csvNames}).");
                    var master = localBranchesWhereCommitShaIsHead.SingleOrDefault(n => n.FriendlyName == "master");
                    if (master != null)
                    {
                        log.Warning("Because one of the branches is 'master', will build master." + moveBranchMsg);
                        Commands.Checkout(repository, master);
                    }
                    else
                    {
                        var branchesWithoutSeparators = localBranchesWhereCommitShaIsHead.Where(b => !b.FriendlyName.Contains('/') && !b.FriendlyName.Contains('-')).ToList();
                        if (branchesWithoutSeparators.Count == 1)
                        {
                            var branchWithoutSeparator = branchesWithoutSeparators[0];
                            log.Warning($"Choosing {branchWithoutSeparator.CanonicalName} as it is the only branch without / or - in it. " + moveBranchMsg);
                            Commands.Checkout(repository, branchWithoutSeparator);
                        }
                        else
                        {
                            throw new WarningException("Failed to try and guess branch to use. " + moveBranchMsg);
                        }
                    }
                }
                else if (localBranchesWhereCommitShaIsHead.Count == 0)
                {
                    log.Info($"No local branch pointing at the commit '{headSha}'. Fake branch needs to be created.");
                    repository.CreateFakeBranchPointingAtThePullRequestTip(log, authentication);
                }
                else
                {
                    log.Info($"Checking out local branch 'refs/heads/{localBranchesWhereCommitShaIsHead[0].FriendlyName}'.");
                    Commands.Checkout(repository, repository.Branches[localBranchesWhereCommitShaIsHead[0].FriendlyName]);
                }
            }
            finally
            {
                if (repository.Head.Tip.Sha != expectedSha)
                {
                    if (environment.GetEnvironmentVariable("IGNORE_NORMALISATION_GIT_HEAD_MOVE") != "1")
                    {
                        // Whoa, HEAD has moved, it shouldn't have. We need to blow up because there is a bug in normalisation
                        throw new BugException($@"GitVersion has a bug, your HEAD has moved after repo normalisation.

To disable this error set an environmental variable called IGNORE_NORMALISATION_GIT_HEAD_MOVE to 1

Please run `git {LibGitExtensions.CreateGitLogArgs(100)}` and submit it along with your build log (with personal info removed) in a new issue at https://github.com/GitTools/GitVersion");
                    }
                }
            }
        }
    }
}
