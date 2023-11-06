using GitVersion.Agents;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using Microsoft.Extensions.Options;

namespace GitVersion;

internal class GitPreparer : IGitPreparer
{
    private readonly ILog log;
    private readonly IEnvironment environment;
    private readonly IMutatingGitRepository repository;
    private readonly IOptions<GitVersionOptions> options;
    private readonly IGitRepositoryInfo repositoryInfo;
    private readonly ICurrentBuildAgent buildAgent;
    private readonly RetryAction<LockedFileException> retryAction;
    private readonly Lazy<GitVersionContext> versionContext;
    private const string DefaultRemoteName = "origin";

    public GitPreparer(ILog log, IEnvironment environment, ICurrentBuildAgent buildAgent, IOptions<GitVersionOptions> options,
        IMutatingGitRepository repository, IGitRepositoryInfo repositoryInfo, Lazy<GitVersionContext> versionContext)
    {
        this.log = log.NotNull();
        this.environment = environment.NotNull();
        this.repository = repository.NotNull();
        this.options = options.NotNull();
        this.repositoryInfo = repositoryInfo.NotNull();
        this.buildAgent = buildAgent.NotNull();
        this.retryAction = new RetryAction<LockedFileException>();
        this.versionContext = versionContext.NotNull();
    }

    public void Prepare()
    {
        var gitVersionOptions = this.options.Value;
        var dotGitDirectory = this.repositoryInfo.DotGitDirectory;
        var projectRoot = this.repositoryInfo.ProjectRootDirectory;

        this.log.Info($"Project root is: {projectRoot}");
        this.log.Info($"DotGit directory is: {dotGitDirectory}");
        if (dotGitDirectory.IsNullOrEmpty() || projectRoot.IsNullOrEmpty())
        {
            throw new Exception($"Failed to prepare or find the .git directory in path '{gitVersionOptions.WorkingDirectory}'.");
        }

        PrepareInternal(gitVersionOptions);
    }

    private void PrepareInternal(GitVersionOptions gitVersionOptions)
    {
        var currentBranch = ResolveCurrentBranch();

        if (!gitVersionOptions.RepositoryInfo.TargetUrl.IsNullOrWhiteSpace())
        {
            CreateDynamicRepository(currentBranch);
            NormalizeGitDirectory(currentBranch, true);
        }
        else
        {
            // Normalize if we are running on build server
            if (gitVersionOptions.Settings.NoNormalize || this.buildAgent is LocalBuild) return;
            if (this.buildAgent.ShouldCleanUpRemotes())
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

        var isDynamicRepository = !gitVersionOptions.RepositoryInfo.ClonePath.IsNullOrWhiteSpace();
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
        if (targetBranch.IsNullOrWhiteSpace())
        {
            throw new Exception("Dynamic Git repositories must have a target branch (/b)");
        }

        var gitDirectory = this.repositoryInfo.DynamicGitRepositoryPath;

        using (this.log.IndentLog($"Creating dynamic repository at '{gitDirectory}'"))
        {
            var gitVersionOptions = this.options.Value;
            var authentication = gitVersionOptions.AuthenticationInfo;
            if (!Directory.Exists(gitDirectory))
            {
                CloneRepository(gitVersionOptions.RepositoryInfo.TargetUrl, gitDirectory, authentication);
            }
            else
            {
                this.log.Info("Git repository already exists");
            }
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
        var authentication = this.options.Value.AuthenticationInfo;
        // Need to ensure the HEAD does not move, this is essentially a BugCheck
        var expectedSha = this.repository.Head.Tip?.Sha;
        var expectedBranchName = this.repository.Head.Name.Canonical;

        var remote = EnsureOnlyOneRemoteIsDefined();
        EnsureRepositoryHeadDuringNormalisation(nameof(EnsureOnlyOneRemoteIsDefined), expectedSha);
        FetchRemotesIfRequired(remote, noFetch, authentication);
        EnsureRepositoryHeadDuringNormalisation(nameof(FetchRemotesIfRequired), expectedSha);
        EnsureLocalBranchExistsForCurrentBranch(remote, currentBranchName);
        EnsureRepositoryHeadDuringNormalisation(nameof(EnsureLocalBranchExistsForCurrentBranch), expectedSha);
        CreateOrUpdateLocalBranchesFromRemoteTrackingOnes(remote.Name);
        EnsureRepositoryHeadDuringNormalisation(nameof(CreateOrUpdateLocalBranchesFromRemoteTrackingOnes), expectedSha);

        var currentBranch = this.repository.Branches.FirstOrDefault(x => x.Name.EquivalentTo(currentBranchName));
        // Bug fix for https://github.com/GitTools/GitVersion/issues/1754, head maybe have been changed
        // if this is a dynamic repository. But only allow this in case the branches are different (branch switch)
        if (expectedSha != this.repository.Head.Tip?.Sha)
        {
            if (isDynamicRepository || currentBranch is null || !this.repository.Head.Equals(currentBranch))
            {
                var newExpectedSha = this.repository.Head.Tip?.Sha;
                var newExpectedBranchName = this.repository.Head.Name.Canonical;

                this.log.Info($"Head has moved from '{expectedBranchName} | {expectedSha}' => '{newExpectedBranchName} | {newExpectedSha}', allowed since this is a dynamic repository");

                expectedSha = newExpectedSha;
            }
        }

        EnsureHeadIsAttachedToBranch(currentBranchName, authentication);
        EnsureRepositoryHeadDuringNormalisation(nameof(EnsureHeadIsAttachedToBranch), expectedSha);

        if (this.repository.IsShallow)
        {
            throw new WarningException("Repository is a shallow clone. Git repositories must contain the full history. See https://gitversion.net/docs/reference/requirements#unshallow for more info.");
        }
    }

    private void EnsureRepositoryHeadDuringNormalisation(string occasion, string? expectedSha)
    {
        expectedSha.NotNull();
        if (this.repository.Head.Tip?.Sha == expectedSha)
            return;

        if (this.environment.GetEnvironmentVariable("IGNORE_NORMALISATION_GIT_HEAD_MOVE") == "1")
            return;

        // Whoa, HEAD has moved, it shouldn't have. We need to blow up because there is a bug in normalisation
        throw new BugException($@"
GitVersion has a bug, your HEAD has moved after repo normalisation after step '{occasion}'
To disable this error set an environmental variable called IGNORE_NORMALISATION_GIT_HEAD_MOVE to 1
Please run `git {GitExtensions.CreateGitLogArgs(100)}` and submit it along with your build log (with personal info removed) in a new issue at https://github.com/GitTools/GitVersion");
    }

    private void EnsureHeadIsAttachedToBranch(string? currentBranchName, AuthenticationInfo authentication)
    {
        var headSha = this.repository.Refs.Head?.TargetIdentifier;
        if (!this.repository.IsHeadDetached)
        {
            this.log.Info($"HEAD points at branch '{headSha}'.");
            return;
        }

        this.log.Info($"HEAD is detached and points at commit '{headSha}'.");
        var localRefs = this.repository.Refs.FromGlob("*").Select(r => $"{r.Name.Canonical} ({r.TargetIdentifier})");
        this.log.Info($"Local Refs:{PathHelper.NewLine}" + string.Join(PathHelper.NewLine, localRefs));

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
        else
        {
            switch (localBranchesWhereCommitShaIsHead.Count)
            {
                case > 1:
                    ChooseLocalBranchToAttach(headSha, localBranchesWhereCommitShaIsHead);
                    break;
                case 0:
                    this.log.Info($"No local branch pointing at the commit '{headSha}'. Fake branch needs to be created.");
                    this.retryAction.Execute(() => this.repository.CreateBranchForPullRequestBranch(authentication));
                    break;
                default:
                    this.log.Info($"Checking out local branch 'refs/heads/{localBranchesWhereCommitShaIsHead[0]}'.");
                    Checkout(localBranchesWhereCommitShaIsHead[0].Name.Friendly);
                    break;
            }
        }
    }
    private void ChooseLocalBranchToAttach(string? headSha, IReadOnlyCollection<IBranch> localBranches)
    {
        var branchNames = localBranches.Select(r => r.Name.Canonical);
        var csvNames = string.Join(", ", branchNames);
        const string moveBranchMsg = "Move one of the branches along a commit to remove warning";

        this.log.Warning($"Found more than one local branch pointing at the commit '{headSha}' ({csvNames}).");
        var mainBranch = localBranches.SingleOrDefault(n => n.Name.EquivalentTo(ConfigurationConstants.MainBranchKey));
        if (mainBranch != null)
        {
            this.log.Warning("Because one of the branches is 'main', will build main." + moveBranchMsg);
            Checkout(ConfigurationConstants.MainBranchKey);
        }
        else
        {
            var branchesWithoutSeparators = localBranches.Where(b => !b.Name.Friendly.Contains('/') && !b.Name.Friendly.Contains('-')).ToList();
            if (branchesWithoutSeparators.Count == 1)
            {
                var branchWithoutSeparator = branchesWithoutSeparators[0];
                this.log.Warning($"Choosing {branchWithoutSeparator.Name.Canonical} as it is the only branch without / or - in it. " + moveBranchMsg);
                Checkout(branchWithoutSeparator.Name.Canonical);
            }
            else if (!this.versionContext.Value.IsCurrentCommitTagged)
            {
                throw new WarningException("Failed to try and guess branch to use. " + moveBranchMsg);
            }
        }
    }
    private void FetchRemotesIfRequired(IRemote remote, bool noFetch, AuthenticationInfo authentication)
    {
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
            var branchName = remoteTrackingReferenceName[prefix.Length..];
            var localReferenceName = ReferenceName.FromBranchName(branchName);

            // We do not want to touch our current branch
            if (this.repository.Head.Name.EquivalentTo(branchName)) continue;

            var localRef = this.repository.Refs[localReferenceName];
            if (localRef != null)
            {
                if (localRef.TargetIdentifier == remoteTrackingReference.TargetIdentifier)
                {
                    this.log.Info($"Skipping update of '{remoteTrackingReference.Name.Canonical}' as it already matches the remote ref.");
                    continue;
                }
                var remoteRefTipId = remoteTrackingReference.ReferenceTargetId;
                if (remoteRefTipId != null)
                {
                    this.log.Info($"Updating local ref '{localRef.Name.Canonical}' to point at {remoteRefTipId}.");
                    this.retryAction.Execute(() => this.repository.Refs.UpdateTarget(localRef, remoteRefTipId));
                }
                continue;
            }

            this.log.Info($"Creating local branch from remote tracking '{remoteTrackingReference.Name.Canonical}'.");
            this.repository.Refs.Add(localReferenceName.Canonical, remoteTrackingReference.TargetIdentifier, true);

            var branch = this.repository.Branches[branchName];
            if (branch != null)
            {
                this.repository.Branches.UpdateTrackedBranch(branch, remoteTrackingReferenceName);
            }
        }
    }

    public void EnsureLocalBranchExistsForCurrentBranch(IRemote remote, string? currentBranch)
    {
        remote.NotNull();

        if (currentBranch.IsNullOrEmpty()) return;

        var referencePrefix = "refs/";
        var isLocalBranch = currentBranch.StartsWith(ReferenceName.LocalBranchPrefix);
        var localCanonicalName = !currentBranch.StartsWith(referencePrefix)
            ? ReferenceName.LocalBranchPrefix + currentBranch
            : isLocalBranch
                ? currentBranch
                : ReferenceName.LocalBranchPrefix + currentBranch[referencePrefix.Length..];

        var repoTip = this.repository.Head.Tip;

        // We currently have the rep.Head of the *default* branch, now we need to look up the right one
        var originCanonicalName = $"{remote.Name}/{currentBranch}";
        var originBranch = this.repository.Branches[originCanonicalName];
        if (originBranch != null)
        {
            repoTip = originBranch.Tip;
        }

        var repoTipId = repoTip?.Id;

        if (repoTipId != null)
        {
            var referenceName = ReferenceName.Parse(localCanonicalName);
            if (this.repository.Branches.All(b => !b.Name.Equals(referenceName)))
            {
                this.log.Info(isLocalBranch
                    ? $"Creating local branch {referenceName}"
                    : $"Creating local branch {referenceName} pointing at {repoTipId}");
                this.repository.Refs.Add(localCanonicalName, repoTipId.Sha);
            }
            else
            {
                this.log.Info(isLocalBranch
                    ? $"Updating local branch {referenceName} to point at {repoTipId}"
                    : $"Updating local branch {referenceName} to match ref {currentBranch}");
                var localRef = this.repository.Refs[localCanonicalName];
                if (localRef != null)
                {
                    this.retryAction.Execute(() => this.repository.Refs.UpdateTarget(localRef, repoTipId));
                }
            }
        }
        Checkout(localCanonicalName);
    }

    private void Checkout(string commitOrBranchSpec) => this.retryAction.Execute(() => this.repository.Checkout(commitOrBranchSpec));
}
