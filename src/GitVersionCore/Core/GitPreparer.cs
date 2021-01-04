using System;
using System.Collections.Generic;
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
            using var repo = new Repository(options.Value.GitRootPath);

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
                if (!string.IsNullOrWhiteSpace(auth.Username))
                {
                    log.Info($"Setting up credentials using name '{auth.Username}'");

                    credentials = new UsernamePasswordCredentials
                    {
                        Username = auth.Username,
                        Password = auth.Password ?? string.Empty
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
            using var repository = new GitRepository(() => gitDirectory);
            // Need to ensure the HEAD does not move, this is essentially a BugCheck
            var expectedSha = repository.Head.Tip.Sha;
            var expectedBranchName = repository.Head.CanonicalName;

            try
            {
                var remote = EnsureOnlyOneRemoteIsDefined(repository, log);

                AddMissingRefSpecs(repository, log, remote);

                //If noFetch is enabled, then GitVersion will assume that the git repository is normalized before execution, so that fetching from remotes is not required.
                if (noFetch)
                {
                    log.Info("Skipping fetching, if GitVersion does not calculate your version as expected you might need to allow fetching or use dynamic repositories");
                }
                else
                {
                    log.Info($"Fetching from remote '{remote.Name}' using the following refspecs: {string.Join(", ", remote.FetchRefSpecs.Select(r => r.Specification))}.");
                    repository.Commands.Fetch(remote.Name, new string[0], authentication.ToFetchOptions(), null);
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
                    repository.Commands.Checkout(matchingCurrentBranch);
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
                        repository.Commands.Checkout(master);
                    }
                    else
                    {
                        var branchesWithoutSeparators = localBranchesWhereCommitShaIsHead.Where(b => !b.FriendlyName.Contains('/') && !b.FriendlyName.Contains('-')).ToList();
                        if (branchesWithoutSeparators.Count == 1)
                        {
                            var branchWithoutSeparator = branchesWithoutSeparators[0];
                            log.Warning($"Choosing {branchWithoutSeparator.CanonicalName} as it is the only branch without / or - in it. " + moveBranchMsg);
                            repository.Commands.Checkout(branchWithoutSeparator);
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
                    CreateFakeBranchPointingAtThePullRequestTip(repository, log, authentication);
                }
                else
                {
                    log.Info($"Checking out local branch 'refs/heads/{localBranchesWhereCommitShaIsHead[0].FriendlyName}'.");
                    repository.Commands.Checkout(repository.Branches[localBranchesWhereCommitShaIsHead[0].FriendlyName]);
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
                    var remotedirectReference = remoteTrackingReference.ResolveToDirectReference();
                    if (localRef.ResolveToDirectReference().TargetIdentifier == remotedirectReference.TargetIdentifier)
                    {
                        log.Info($"Skipping update of '{remoteTrackingReference.CanonicalName}' as it already matches the remote ref.");
                        continue;
                    }
                    var remoteRefTipId = remotedirectReference.Target.Id;
                    log.Info($"Updating local ref '{localRef.CanonicalName}' to point at {remoteRefTipId}.");
                    repo.Refs.UpdateTarget(localRef, remoteRefTipId);
                    continue;
                }

                log.Info($"Creating local branch from remote tracking '{remoteTrackingReference.CanonicalName}'.");
                repo.Refs.Add(localCanonicalName, new ObjectId(remoteTrackingReference.ResolveToDirectReference().TargetIdentifier), true);

                var branch = repo.Branches[branchName];
                repo.Branches.Update(branch, b => b.TrackedBranch = remoteTrackingReferenceName);
            }
        }

        private static void EnsureLocalBranchExistsForCurrentBranch(IGitRepository repo, ILog log, Remote remote, string currentBranch)
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
                repo.Refs.Add(localCanonicalName, repoTipId);
            }
            else
            {
                log.Info(isBranch ? $"Updating local branch {localCanonicalName} to point at {repoTip.Sha}"
                    : $"Updating local branch {localCanonicalName} to match ref {currentBranch}");
                var localRef = repo.Refs[localCanonicalName];
                repo.Refs.UpdateTarget(localRef, repoTipId);
            }

            repo.Commands.Checkout(localCanonicalName);
        }

        private static Remote EnsureOnlyOneRemoteIsDefined(IGitRepository repo, ILog log)
        {
            var remotes = repo.Network.Remotes;
            var howMany = remotes.Count();

            if (howMany == 1)
            {
                var remote = remotes.Single();
                log.Info($"One remote found ({remote.Name} -> '{remote.Url}').");
                return remote;
            }

            var message = $"{howMany} remote(s) have been detected. When being run on a build server, the Git repository is expected to bear one (and no more than one) remote.";
            throw new WarningException(message);
        }

        private static void AddMissingRefSpecs(IGitRepository repo, ILog log, Remote remote)
        {
            if (remote.FetchRefSpecs.Any(r => r.Source == "refs/heads/*"))
                return;

            var allBranchesFetchRefSpec = $"+refs/heads/*:refs/remotes/{remote.Name}/*";

            log.Info($"Adding refspec: {allBranchesFetchRefSpec}");

            repo.Network.Remotes.Update(remote.Name,
                r => r.FetchRefSpecs.Add(allBranchesFetchRefSpec));
        }

        private static void CreateFakeBranchPointingAtThePullRequestTip(IGitRepository repo, ILog log, AuthenticationInfo authentication)
        {
            var remote = repo.Network.Remotes.Single();

            log.Info("Fetching remote refs to see if there is a pull request ref");
            var remoteTips = (string.IsNullOrEmpty(authentication.Username) ?
                    GetRemoteTipsForAnonymousUser(repo, remote) :
                    GetRemoteTipsUsingUsernamePasswordCredentials(repo, remote, authentication.Username, authentication.Password))
                .ToList();

            log.Info($"Remote Refs:{System.Environment.NewLine}" + string.Join(System.Environment.NewLine, remoteTips.Select(r => r.CanonicalName)));

            var headTipSha = repo.Head.Tip.Sha;

            var refs = remoteTips.Where(r => r.TargetIdentifier == headTipSha).ToList();

            if (refs.Count == 0)
            {
                var message = $"Couldn't find any remote tips from remote '{remote.Url}' pointing at the commit '{headTipSha}'.";
                throw new WarningException(message);
            }

            if (refs.Count > 1)
            {
                var names = string.Join(", ", refs.Select(r => r.CanonicalName));
                var message = $"Found more than one remote tip from remote '{remote.Url}' pointing at the commit '{headTipSha}'. Unable to determine which one to use ({names}).";
                throw new WarningException(message);
            }

            var reference = refs[0];
            var canonicalName = reference.CanonicalName;
            log.Info($"Found remote tip '{canonicalName}' pointing at the commit '{headTipSha}'.");

            if (canonicalName.StartsWith("refs/tags"))
            {
                log.Info($"Checking out tag '{canonicalName}'");
                repo.Commands.Checkout(reference.Target.Sha);
                return;
            }

            if (!canonicalName.StartsWith("refs/pull/") && !canonicalName.StartsWith("refs/pull-requests/"))
            {
                var message = $"Remote tip '{canonicalName}' from remote '{remote.Url}' doesn't look like a valid pull request.";
                throw new WarningException(message);
            }

            var fakeBranchName = canonicalName.Replace("refs/pull/", "refs/heads/pull/").Replace("refs/pull-requests/", "refs/heads/pull-requests/");

            log.Info($"Creating fake local branch '{fakeBranchName}'.");
            repo.Refs.Add(fakeBranchName, new ObjectId(headTipSha));

            log.Info($"Checking local branch '{fakeBranchName}' out.");
            repo.Commands.Checkout(fakeBranchName);
        }

        private static IEnumerable<DirectReference> GetRemoteTipsUsingUsernamePasswordCredentials(IGitRepository repository, Remote remote, string username, string password)
        {
            return repository.Network.ListReferences(remote, (url, fromUrl, types) => new UsernamePasswordCredentials
            {
                Username = username,
                Password = password ?? string.Empty
            }).Select(r => r.ResolveToDirectReference());
        }

        private static IEnumerable<DirectReference> GetRemoteTipsForAnonymousUser(IGitRepository repository, Remote remote)
        {
            return repository.Network.ListReferences(remote).Select(r => r.ResolveToDirectReference());
        }
    }
}
