using GitVersion.BuildAgents;
using GitVersion.Common;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.Model.Configuration;
using Microsoft.Extensions.Options;

namespace GitVersion;

public class GitPreparer : IGitPreparer
{
    private readonly ILog log;
    private readonly IEnvironment environment;
    private readonly IMutatingGitRepository repository;
    private readonly IOptions<GitVersionOptions> options;
    private readonly IGitRepositoryInfo repositoryInfo;
    private readonly IRepositoryStore repositoryStore;
    private readonly ICurrentBuildAgent buildAgent;
    private readonly RetryAction<LockedFileException> retryAction;

    private const string DefaultRemoteName = "origin";

    public GitPreparer(ILog log, IEnvironment environment, ICurrentBuildAgent buildAgent, IOptions<GitVersionOptions> options,
        IMutatingGitRepository repository, IGitRepositoryInfo repositoryInfo, IRepositoryStore repositoryStore)
    {
        this.log = log ?? throw new ArgumentNullException(nameof(log));
        this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.repositoryInfo = repositoryInfo ?? throw new ArgumentNullException(nameof(repositoryInfo));
        this.repositoryStore = repositoryStore ?? throw new ArgumentNullException(nameof(repositoryStore));
        this.buildAgent = buildAgent;
        this.retryAction = new RetryAction<LockedFileException>();
    }

    public void Prepare()
    {
        var gitVersionOptions = this.options.Value;

        // Normalize if we are running on build server
        var normalizeGitDirectory = !gitVersionOptions.Settings.NoNormalize && this.buildAgent != null;
        var shouldCleanUpRemotes = this.buildAgent != null && this.buildAgent.ShouldCleanUpRemotes();
        var currentBranch = ResolveCurrentBranch();

        var dotGitDirectory = this.repositoryInfo.DotGitDirectory;
        var projectRoot = this.repositoryInfo.ProjectRootDirectory;

        this.log.Info($"Project root is: {projectRoot}");
        this.log.Info($"DotGit directory is: {dotGitDirectory}");
        if (dotGitDirectory.IsNullOrEmpty() || projectRoot.IsNullOrEmpty())
        {
            throw new Exception($"Failed to prepare or find the .git directory in path '{gitVersionOptions.WorkingDirectory}'.");
        }

        PrepareInternal(normalizeGitDirectory, currentBranch, shouldCleanUpRemotes);
    }

    private void PrepareInternal(bool normalizeGitDirectory, string? currentBranch, bool shouldCleanUpRemotes = false)
    {
        var gitVersionOptions = this.options.Value;
        if (!gitVersionOptions.RepositoryInfo.TargetUrl.IsNullOrWhiteSpace())
        {
            CreateDynamicRepository(currentBranch);
        }
        else
        {
            if (!normalizeGitDirectory) return;
            if (shouldCleanUpRemotes)
            {
                CleanupDuplicateOrigin();
            }

            NormalizeGitDirectory(currentBranch, false);
        }
    }

    private string? ResolveCurrentBranch()
    {
        var gitVersionOptions = this.options.Value;
        var targetBranch = gitVersionOptions.RepositoryInfo.TargetBranch;
        if (this.buildAgent == null)
        {
            return targetBranch;
        }

        var isDynamicRepository = !gitVersionOptions.RepositoryInfo.DynamicRepositoryClonePath.IsNullOrWhiteSpace();
        var currentBranch = this.buildAgent.GetCurrentBranch(isDynamicRepository) ?? targetBranch;
        this.log.Info("Branch from build environment: " + currentBranch);

        return currentBranch;
    }

    private void CleanupDuplicateOrigin()
    {
        var remoteToKeep = DefaultRemoteName;
        // check that we have a remote that matches defaultRemoteName if not take the first remote
        if (!this.repository.Remotes.Any(remote => remote.Name.Equals(DefaultRemoteName, StringComparison.InvariantCultureIgnoreCase)))
        {
            remoteToKeep = this.repository.Remotes.First().Name;
        }

        var duplicateRemotes = this.repository.Remotes
            .Where(remote => !remote.Name.Equals(remoteToKeep, StringComparison.InvariantCultureIgnoreCase))
            .Select(remote => remote.Name);

        // remove all remotes that are considered duplicates
        foreach (var remoteName in duplicateRemotes)
        {
            this.repository.Remotes.Remove(remoteName);
        }
    }

    private void CreateDynamicRepository(string? targetBranch)
    {
        var gitVersionOptions = this.options.Value;
        if (targetBranch.IsNullOrWhiteSpace())
        {
            throw new Exception("Dynamic Git repositories must have a target branch (/b)");
        }

        var gitDirectory = this.repositoryInfo.DynamicGitRepositoryPath;

        using (this.log.IndentLog($"Creating dynamic repository at '{gitDirectory}'"))
        {
            var authentication = gitVersionOptions.Authentication;
            if (!Directory.Exists(gitDirectory))
            {
                CloneRepository(gitVersionOptions.RepositoryInfo.TargetUrl, gitDirectory, authentication);
            }
            else
            {
                this.log.Info("Git repository already exists");
            }
            NormalizeGitDirectory(targetBranch, true);
        }
    }

    private void NormalizeGitDirectory(string? targetBranch, bool isDynamicRepository)
    {
        using (this.log.IndentLog($"Normalizing git directory for branch '{targetBranch}'"))
        {
            // Normalize (download branches) before using the branch
            NormalizeGitDirectory(this.options.Value.Settings.NoFetch, targetBranch, isDynamicRepository);
        }
    }

    private void CloneRepository(string? repositoryUrl, string? gitDirectory, AuthenticationInfo auth)
    {
        using (this.log.IndentLog($"Cloning repository from url '{repositoryUrl}'"))
        {
            this.retryAction.Execute(() => this.repository.Clone(repositoryUrl, gitDirectory, auth));
        }
    }

    /// <summary>
    /// Normalization of a git directory turns all remote branches into local branches,
    /// turns pull request refs into a real branch and a few other things.
    /// This is designed to be run *only on the build server* which checks out repositories in different ways.
    /// It is not recommended to run normalization against a local repository
    /// </summary>
    private void NormalizeGitDirectory(bool noFetch, string? currentBranchName, bool isDynamicRepository)
    {
        var authentication = this.options.Value.Authentication;
        // Need to ensure the HEAD does not move, this is essentially a BugCheck
        var expectedSha = this.repository.Head.Tip?.Sha;
        var expectedBranchName = this.repository.Head.Name.Canonical;

        try
        {
            var remote = EnsureOnlyOneRemoteIsDefined();

            //If noFetch is enabled, then GitVersion will assume that the git repository is normalized before execution, so that fetching from remotes is not required.
            if (noFetch)
            {
                this.log.Info("Skipping fetching, if GitVersion does not calculate your version as expected you might need to allow fetching or use dynamic repositories");
            }
            else
            {
                var refSpecs = string.Join(", ", remote.FetchRefSpecs.Select(r => r.Specification));
                this.log.Info($"Fetching from remote '{remote.Name}' using the following refspecs: {refSpecs}.");
                this.retryAction.Execute(() => this.repository.Fetch(remote.Name, Enumerable.Empty<string>(), authentication, null));
            }

            EnsureLocalBranchExistsForCurrentBranch(remote, currentBranchName);
            CreateOrUpdateLocalBranchesFromRemoteTrackingOnes(remote.Name);

            var currentBranch = this.repositoryStore.FindBranch(currentBranchName);
            // Bug fix for https://github.com/GitTools/GitVersion/issues/1754, head maybe have been changed
            // if this is a dynamic repository. But only allow this in case the branches are different (branch switch)
            if (expectedSha != this.repository.Head.Tip?.Sha &&
                (isDynamicRepository || currentBranch is null || !this.repository.Head.Equals(currentBranch)))
            {
                var newExpectedSha = this.repository.Head.Tip?.Sha;
                var newExpectedBranchName = this.repository.Head.Name.Canonical;

                this.log.Info($"Head has moved from '{expectedBranchName} | {expectedSha}' => '{newExpectedBranchName} | {newExpectedSha}', allowed since this is a dynamic repository");

                expectedSha = newExpectedSha;
            }

            var headSha = this.repository.Refs.Head?.TargetIdentifier;

            if (!this.repository.IsHeadDetached)
            {
                this.log.Info($"HEAD points at branch '{headSha}'.");
                return;
            }

            this.log.Info($"HEAD is detached and points at commit '{headSha}'.");
            this.log.Info($"Local Refs:{System.Environment.NewLine}" + string.Join(System.Environment.NewLine, this.repository.Refs.FromGlob("*").Select(r => $"{r.Name.Canonical} ({r.TargetIdentifier})")));

            // In order to decide whether a fake branch is required or not, first check to see if any local branches have the same commit SHA of the head SHA.
            // If they do, go ahead and checkout that branch
            // If no, go ahead and check out a new branch, using the known commit SHA as the pointer
            var localBranchesWhereCommitShaIsHead = this.repository.Branches.Where(b => !b.IsRemote && b.Tip?.Sha == headSha).ToList();

            var matchingCurrentBranch = !currentBranchName.IsNullOrEmpty()
                ? localBranchesWhereCommitShaIsHead.SingleOrDefault(b => b.Name.Canonical.Replace("/heads/", "/") == currentBranchName.Replace("/heads/", "/"))
                : null;
            if (matchingCurrentBranch != null)
            {
                this.log.Info($"Checking out local branch '{currentBranchName}'.");
                Checkout(matchingCurrentBranch.Name.Canonical);
            }
            else if (localBranchesWhereCommitShaIsHead.Count > 1)
            {
                var branchNames = localBranchesWhereCommitShaIsHead.Select(r => r.Name.Canonical);
                var csvNames = string.Join(", ", branchNames);
                const string moveBranchMsg = "Move one of the branches along a commit to remove warning";

                this.log.Warning($"Found more than one local branch pointing at the commit '{headSha}' ({csvNames}).");
                var main = localBranchesWhereCommitShaIsHead.SingleOrDefault(n => n.Name.EquivalentTo(Config.MainBranchKey));
                if (main != null)
                {
                    this.log.Warning("Because one of the branches is 'main', will build main." + moveBranchMsg);
                    Checkout(Config.MainBranchKey);
                }
                else
                {
                    var branchesWithoutSeparators = localBranchesWhereCommitShaIsHead.Where(b => !b.Name.Friendly.Contains('/') && !b.Name.Friendly.Contains('-')).ToList();
                    if (branchesWithoutSeparators.Count == 1)
                    {
                        var branchWithoutSeparator = branchesWithoutSeparators[0];
                        this.log.Warning($"Choosing {branchWithoutSeparator.Name.Canonical} as it is the only branch without / or - in it. " + moveBranchMsg);
                        Checkout(branchWithoutSeparator.Name.Canonical);
                    }
                    else
                    {
                        throw new WarningException("Failed to try and guess branch to use. " + moveBranchMsg);
                    }
                }
            }
            else if (localBranchesWhereCommitShaIsHead.Count == 0)
            {
                this.log.Info($"No local branch pointing at the commit '{headSha}'. Fake branch needs to be created.");
                this.retryAction.Execute(() => this.repository.CreateBranchForPullRequestBranch(authentication));
            }
            else
            {
                this.log.Info($"Checking out local branch 'refs/heads/{localBranchesWhereCommitShaIsHead[0]}'.");
                Checkout(localBranchesWhereCommitShaIsHead[0].Name.Friendly);
            }
        }
        finally
        {
            if (this.repository.Head.Tip?.Sha != expectedSha)
            {
                if (this.environment.GetEnvironmentVariable("IGNORE_NORMALISATION_GIT_HEAD_MOVE") != "1")
                {
                    // Whoa, HEAD has moved, it shouldn't have. We need to blow up because there is a bug in normalisation
                    throw new BugException($@"GitVersion has a bug, your HEAD has moved after repo normalisation.

To disable this error set an environmental variable called IGNORE_NORMALISATION_GIT_HEAD_MOVE to 1

Please run `git {GitExtensions.CreateGitLogArgs(100)}` and submit it along with your build log (with personal info removed) in a new issue at https://github.com/GitTools/GitVersion");
                }
            }
        }
    }

    private IRemote EnsureOnlyOneRemoteIsDefined()
    {
        var remotes = this.repository.Remotes;
        var howMany = remotes.Count();

        if (howMany == 1)
        {
            var remote = remotes.Single();
            this.log.Info($"One remote found ({remote.Name} -> '{remote.Url}').");
            if (remote.FetchRefSpecs.Any(r => r.Source == "refs/heads/*"))
                return remote;

            var allBranchesFetchRefSpec = $"+refs/heads/*:refs/remotes/{remote.Name}/*";
            this.log.Info($"Adding refspec: {allBranchesFetchRefSpec}");
            remotes.Update(remote.Name, allBranchesFetchRefSpec);
            return remote;
        }

        var message = $"{howMany} remote(s) have been detected. When being run on a build server, the Git repository is expected to bear one (and no more than one) remote.";
        throw new WarningException(message);
    }

    private void CreateOrUpdateLocalBranchesFromRemoteTrackingOnes(string remoteName)
    {
        var prefix = $"refs/remotes/{remoteName}/";
        var remoteHeadCanonicalName = $"{prefix}HEAD";
        var headReferenceName = ReferenceName.Parse(remoteHeadCanonicalName);
        var remoteTrackingReferences = this.repository.Refs
            .FromGlob(prefix + "*")
            .Where(r => !r.Name.Equals(headReferenceName));

        foreach (var remoteTrackingReference in remoteTrackingReferences)
        {
            var remoteTrackingReferenceName = remoteTrackingReference.Name.Canonical;
            var branchName = remoteTrackingReferenceName.Substring(prefix.Length);
            var localCanonicalName = "refs/heads/" + branchName;

            var referenceName = ReferenceName.Parse(localCanonicalName);
            // We do not want to touch our current branch
            if (this.repository.Head.Name.EquivalentTo(branchName)) continue;

            if (this.repository.Refs.Any(x => x.Name.Equals(referenceName)))
            {
                var localRef = this.repository.Refs[localCanonicalName]!;
                if (localRef.TargetIdentifier == remoteTrackingReference.TargetIdentifier)
                {
                    this.log.Info($"Skipping update of '{remoteTrackingReference.Name.Canonical}' as it already matches the remote ref.");
                    continue;
                }
                var remoteRefTipId = remoteTrackingReference.ReferenceTargetId!;
                this.log.Info($"Updating local ref '{localRef.Name.Canonical}' to point at {remoteRefTipId}.");
                this.retryAction.Execute(() => this.repository.Refs.UpdateTarget(localRef, remoteRefTipId));
                continue;
            }

            this.log.Info($"Creating local branch from remote tracking '{remoteTrackingReference.Name.Canonical}'.");
            this.repository.Refs.Add(localCanonicalName, remoteTrackingReference.TargetIdentifier, true);

            var branch = this.repository.Branches[branchName]!;
            this.repository.Branches.UpdateTrackedBranch(branch, remoteTrackingReferenceName);
        }
    }

    public void EnsureLocalBranchExistsForCurrentBranch(IRemote? remote, string? currentBranch)
    {
        if (remote is null)
        {
            throw new ArgumentNullException(nameof(remote));
        }

        if (currentBranch.IsNullOrEmpty()) return;

        var isRef = currentBranch.Contains("refs");
        var isBranch = currentBranch.Contains("refs/heads");
        var localCanonicalName = !isRef
            ? "refs/heads/" + currentBranch
            : isBranch
                ? currentBranch
                : currentBranch.Replace("refs/", "refs/heads/");

        var repoTip = this.repository.Head.Tip;

        // We currently have the rep.Head of the *default* branch, now we need to look up the right one
        var originCanonicalName = $"{remote.Name}/{currentBranch}";
        var originBranch = this.repository.Branches[originCanonicalName];
        if (originBranch != null)
        {
            repoTip = originBranch.Tip;
        }

        var repoTipId = repoTip!.Id;

        var referenceName = ReferenceName.Parse(localCanonicalName);
        if (this.repository.Branches.All(b => !b.Name.Equals(referenceName)))
        {
            this.log.Info(isBranch ? $"Creating local branch {referenceName}"
                : $"Creating local branch {referenceName} pointing at {repoTipId}");
            this.repository.Refs.Add(localCanonicalName, repoTipId.Sha);
        }
        else
        {
            this.log.Info(isBranch ? $"Updating local branch {referenceName} to point at {repoTip}"
                : $"Updating local branch {referenceName} to match ref {currentBranch}");
            var localRef = this.repository.Refs[localCanonicalName]!;
            this.retryAction.Execute(() => this.repository.Refs.UpdateTarget(localRef, repoTipId));
        }

        Checkout(localCanonicalName);
    }

    private void Checkout(string commitOrBranchSpec) => this.retryAction.Execute(() => this.repository.Checkout(commitOrBranchSpec));
}
