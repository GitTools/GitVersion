using System;
using System.IO;
using System.Linq;
using GitVersion.Extensions;
using GitVersion.Logging;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    public class GitPreparer : IGitPreparer
    {
        private readonly ILog log;
        private readonly IEnvironment environment;
        private readonly IOptions<GitVersionOptions> options;
        private readonly IGitRepositoryInfo repositoryInfo;
        private readonly ICurrentBuildAgent buildAgent;

        private const string DefaultRemoteName = "origin";

        public GitPreparer(ILog log, IEnvironment environment, ICurrentBuildAgent buildAgent,
            IOptions<GitVersionOptions> options, IGitRepositoryInfo repositoryInfo)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.repositoryInfo = repositoryInfo ?? throw new ArgumentNullException(nameof(repositoryInfo));
            this.buildAgent = buildAgent;
        }

        public void Prepare()
        {
            var gitVersionOptions = options.Value;

            // Normalize if we are running on build server
            var normalizeGitDirectory = !gitVersionOptions.Settings.NoNormalize && buildAgent != null;
            var shouldCleanUpRemotes = buildAgent != null && buildAgent.ShouldCleanUpRemotes();
            var currentBranch = ResolveCurrentBranch();

            var dotGitDirectory = repositoryInfo.DotGitDirectory;
            var projectRoot = repositoryInfo.ProjectRootDirectory;

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

                    NormalizeGitDirectory(currentBranch, repositoryInfo.DotGitDirectory, false);
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
            using IGitRepository repo = new GitRepository(log, repositoryInfo.GitRootPath);
            repo.CleanupDuplicateOrigin(DefaultRemoteName);
        }

        private void CreateDynamicRepository(string targetBranch)
        {
            var gitVersionOptions = options.Value;
            if (string.IsNullOrWhiteSpace(targetBranch))
            {
                throw new Exception("Dynamic Git repositories must have a target branch (/b)");
            }

            var gitDirectory = repositoryInfo.DynamicGitRepositoryPath;

            using (log.IndentLog($"Creating dynamic repository at '{gitDirectory}'"))
            {
                var authentication = gitVersionOptions.Authentication;
                if (!Directory.Exists(gitDirectory))
                {
                    CloneRepository(gitVersionOptions.RepositoryInfo.TargetUrl, gitDirectory, authentication);
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
            using (log.IndentLog($"Cloning repository from url '{repositoryUrl}'"))
            {
                var returnedPath = GitRepository.Clone(repositoryUrl, gitDirectory, auth);
                log.Info($"Returned path after repository clone: {returnedPath}");
            }
        }

        /// <summary>
        /// Normalization of a git directory turns all remote branches into local branches, turns pull request refs into a real branch and a few other things. This is designed to be run *only on the build server* which checks out repositories in different ways.
        /// It is not recommended to run normalization against a local repository
        /// </summary>
        private void NormalizeGitDirectory(string gitDirectory, bool noFetch, string currentBranch, bool isDynamicRepository)
        {
            var authentication = options.Value.Authentication;
            using var repository = new GitRepository(log, gitDirectory);
            // Need to ensure the HEAD does not move, this is essentially a BugCheck
            var expectedSha = repository.Head.Tip.Sha;
            var expectedBranchName = repository.Head.CanonicalName;

            try
            {
                var remote = repository.EnsureOnlyOneRemoteIsDefined();

                //If noFetch is enabled, then GitVersion will assume that the git repository is normalized before execution, so that fetching from remotes is not required.
                if (noFetch)
                {
                    log.Info("Skipping fetching, if GitVersion does not calculate your version as expected you might need to allow fetching or use dynamic repositories");
                }
                else
                {
                    log.Info($"Fetching from remote '{remote.Name}' using the following refspecs: {remote.RefSpecs}.");
                    repository.Fetch(remote.Name, new string[0], authentication, null);
                }

                EnsureLocalBranchExistsForCurrentBranch(repository, log, remote, currentBranch);
                CreateOrUpdateLocalBranchesFromRemoteTrackingOnes(repository, log, remote.Name);

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

                if (!repository.IsHeadDetached)
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
                    repository.Checkout(matchingCurrentBranch);
                }
                else if (localBranchesWhereCommitShaIsHead.Count > 1)
                {
                    var branchNames = localBranchesWhereCommitShaIsHead.Select(r => r.CanonicalName);
                    var csvNames = string.Join(", ", branchNames);
                    const string moveBranchMsg = "Move one of the branches along a commit to remove warning";

                    log.Warning($"Found more than one local branch pointing at the commit '{headSha}' ({csvNames}).");
                    var master = localBranchesWhereCommitShaIsHead.SingleOrDefault(n => n.FriendlyName.IsEquivalentTo("master"));
                    if (master != null)
                    {
                        log.Warning("Because one of the branches is 'master', will build master." + moveBranchMsg);
                        repository.Checkout(master);
                    }
                    else
                    {
                        var branchesWithoutSeparators = localBranchesWhereCommitShaIsHead.Where(b => !b.FriendlyName.Contains('/') && !b.FriendlyName.Contains('-')).ToList();
                        if (branchesWithoutSeparators.Count == 1)
                        {
                            var branchWithoutSeparator = branchesWithoutSeparators[0];
                            log.Warning($"Choosing {branchWithoutSeparator.CanonicalName} as it is the only branch without / or - in it. " + moveBranchMsg);
                            repository.Checkout(branchWithoutSeparator);
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
                    repository.CreateBranchForPullRequestBranch(authentication);
                }
                else
                {
                    log.Info($"Checking out local branch 'refs/heads/{localBranchesWhereCommitShaIsHead[0].FriendlyName}'.");
                    repository.Checkout(repository.Branches[localBranchesWhereCommitShaIsHead[0].FriendlyName]);
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

Please run `git {GitExtensions.CreateGitLogArgs(100)}` and submit it along with your build log (with personal info removed) in a new issue at https://github.com/GitTools/GitVersion");
                    }
                }
            }
        }

        private static void CreateOrUpdateLocalBranchesFromRemoteTrackingOnes(IGitRepository repo, ILog log, string remoteName)
        {
            var prefix = $"refs/remotes/{remoteName}/";
            var remoteHeadCanonicalName = $"{prefix}HEAD";
            var remoteTrackingReferences = repo.Refs
                .FromGlob(prefix + "*")
                .Where(r => !r.CanonicalName.IsEquivalentTo(remoteHeadCanonicalName));

            foreach (var remoteTrackingReference in remoteTrackingReferences)
            {
                var remoteTrackingReferenceName = remoteTrackingReference.CanonicalName;
                var branchName = remoteTrackingReferenceName.Substring(prefix.Length);
                var localCanonicalName = "refs/heads/" + branchName;

                // We do not want to touch our current branch
                if (branchName.IsEquivalentTo(repo.Head.FriendlyName)) continue;

                if (repo.Refs.Any(x => x.CanonicalName.IsEquivalentTo(localCanonicalName)))
                {
                    var localRef = repo.Refs[localCanonicalName];
                    if (localRef.DirectReferenceTargetIdentifier == remoteTrackingReference.DirectReferenceTargetIdentifier)
                    {
                        log.Info($"Skipping update of '{remoteTrackingReference.CanonicalName}' as it already matches the remote ref.");
                        continue;
                    }
                    var remoteRefTipId = remoteTrackingReference.DirectReferenceTargetId;
                    log.Info($"Updating local ref '{localRef.CanonicalName}' to point at {remoteRefTipId}.");
                    repo.Refs.UpdateTarget(localRef, remoteRefTipId);
                    continue;
                }

                log.Info($"Creating local branch from remote tracking '{remoteTrackingReference.CanonicalName}'.");
                repo.Refs.Add(localCanonicalName, remoteTrackingReference.DirectReferenceTargetIdentifier, true);

                var branch = repo.Branches[branchName];
                repo.Branches.UpdateTrackedBranch(branch, remoteTrackingReferenceName);
            }
        }

        private static void EnsureLocalBranchExistsForCurrentBranch(IGitRepository repo, ILog log, IRemote remote, string currentBranch)
        {
            if (log is null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            if (remote is null)
            {
                throw new ArgumentNullException(nameof(remote));
            }

            if (string.IsNullOrEmpty(currentBranch)) return;

            var isRef = currentBranch.Contains("refs");
            var isBranch = currentBranch.Contains("refs/heads");
            var localCanonicalName = !isRef
                ? "refs/heads/" + currentBranch
                : isBranch
                    ? currentBranch
                    : currentBranch.Replace("refs/", "refs/heads/");

            var repoTip = repo.Head.Tip;

            // We currently have the rep.Head of the *default* branch, now we need to look up the right one
            var originCanonicalName = $"{remote.Name}/{currentBranch}";
            var originBranch = repo.Branches[originCanonicalName];
            if (originBranch != null)
            {
                repoTip = originBranch.Tip;
            }

            var repoTipId = repoTip.Id;

            if (repo.Branches.All(b => !b.CanonicalName.IsEquivalentTo(localCanonicalName)))
            {
                log.Info(isBranch ? $"Creating local branch {localCanonicalName}"
                    : $"Creating local branch {localCanonicalName} pointing at {repoTipId}");
                repo.Refs.Add(localCanonicalName, repoTipId.Sha);
            }
            else
            {
                log.Info(isBranch ? $"Updating local branch {localCanonicalName} to point at {repoTip.Sha}"
                    : $"Updating local branch {localCanonicalName} to match ref {currentBranch}");
                var localRef = repo.Refs[localCanonicalName];
                repo.Refs.UpdateTarget(localRef, repoTipId);
            }

            repo.Checkout(localCanonicalName);
        }
    }
}
