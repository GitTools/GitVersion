using System.IO.Abstractions;
using GitVersion.Agents;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Helpers;
using GitVersion.Logging;

namespace GitVersion;

internal class GitPreparer(
    ILogger<GitPreparer> logger,
    IFileSystem fileSystem,
    IEnvironment environment,
    ICurrentBuildAgent buildAgent,
    IOptions<GitVersionOptions> options,
    IMutatingGitRepository repository,
    IGitRepositoryInfo repositoryInfo,
    Lazy<GitVersionContext> versionContext)
    : IGitPreparer
{
    private readonly ILogger<GitPreparer> logger = logger.NotNull();
    private readonly IFileSystem fileSystem = fileSystem.NotNull();
    private readonly IEnvironment environment = environment.NotNull();
    private readonly IMutatingGitRepository repository = repository.NotNull();
    private readonly IOptions<GitVersionOptions> options = options.NotNull();
    private readonly IGitRepositoryInfo repositoryInfo = repositoryInfo.NotNull();
    private readonly ICurrentBuildAgent buildAgent = buildAgent.NotNull();
    private readonly RetryAction<LockedFileException> retryAction = new();
    private readonly Lazy<GitVersionContext> versionContext = versionContext.NotNull();

    private const string DefaultRemoteName = "origin";

    public void Prepare()
    {
        var gitVersionOptions = this.options.Value;
        var dotGitDirectory = this.repositoryInfo.DotGitDirectory;
        var projectRoot = this.repositoryInfo.ProjectRootDirectory;

        this.logger.LogInformation("Project root is: {ProjectRoot}", projectRoot);
        this.logger.LogInformation("DotGit directory is: {DotGitDirectory}", dotGitDirectory);
        if (dotGitDirectory.IsNullOrEmpty() || projectRoot.IsNullOrEmpty())
        {
            throw new($"Failed to prepare or find the .git directory in path '{gitVersionOptions.WorkingDirectory}'.");
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
        this.logger.LogInformation("Branch from build environment: {CurrentBranch}", currentBranch);

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
            throw new("Dynamic Git repositories must have a target branch (/b)");
        }

        var gitDirectory = this.repositoryInfo.DynamicGitRepositoryPath;

        using (this.logger.StartIndentedScope($"Creating dynamic repository at '{gitDirectory}'"))
        {
            var gitVersionOptions = this.options.Value;
            var authentication = gitVersionOptions.AuthenticationInfo;
            if (string.IsNullOrWhiteSpace(gitDirectory))
            {
                throw new("Dynamic Git repositories should have a path specified");
            }
            if (!this.fileSystem.Directory.Exists(gitDirectory))
            {
                CloneRepository(gitVersionOptions.RepositoryInfo.TargetUrl, gitDirectory, authentication);
            }
            else
            {
                this.logger.LogInformation("Git repository already exists");
            }
        }
    }

    private void CloneRepository(string? repositoryUrl, string? gitDirectory, AuthenticationInfo auth)
    {
        using (this.logger.StartIndentedScope($"Cloning repository from url '{repositoryUrl}'"))
        {
            this.retryAction.Execute(() => this.repository.Clone(repositoryUrl, gitDirectory, auth));
        }
    }

    private void NormalizeGitDirectory(string? targetBranch, bool isDynamicRepository)
    {
        using (this.logger.StartIndentedScope($"Normalizing git directory for branch '{targetBranch}'"))
        {
            // Normalize (download branches) before using the branch
            NormalizeGitDirectory(this.options.Value.Settings.NoFetch || this.buildAgent.PreventFetch(), targetBranch, isDynamicRepository);
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

                this.logger.LogInformation("Head has moved from '{ExpectedBranchName} | {ExpectedSha}' => '{NewExpectedBranchName} | {NewExpectedSha}', allowed since this is a dynamic repository", expectedBranchName, expectedSha, newExpectedBranchName, newExpectedSha);

                expectedSha = newExpectedSha;
            }
        }

        EnsureHeadIsAttachedToBranch(currentBranchName, authentication);
        EnsureRepositoryHeadDuringNormalisation(nameof(EnsureHeadIsAttachedToBranch), expectedSha);

        if (!this.repository.IsShallow) return;
        if (this.options.Value.Settings.AllowShallow)
        {
            this.logger.LogInformation("Repository is a shallow clone. GitVersion will continue, but it is recommended to use a full clone for accurate versioning.");
        }
        else
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
        throw new BugException($"""
                                GitVersion has a bug, your HEAD has moved after repo normalisation after step '{occasion}'
                                To disable this error set an environmental variable called IGNORE_NORMALISATION_GIT_HEAD_MOVE to 1
                                Please run `git {GitExtensions.CreateGitLogArgs(100)}` and submit it along with your build log (with personal info removed) in a new issue at https://github.com/GitTools/GitVersion
                                """);
    }

    private void EnsureHeadIsAttachedToBranch(string? currentBranchName, AuthenticationInfo authentication)
    {
        var headSha = this.repository.References.Head?.TargetIdentifier;
        if (!this.repository.IsHeadDetached)
        {
            this.logger.LogInformation("HEAD points at branch '{HeadSha}'.", headSha);
            return;
        }

        this.logger.LogInformation("HEAD is detached and points at commit '{HeadSha}'.", headSha);
        var localRefs = this.repository.References.FromGlob("*").Select(r => $"{r.Name.Canonical} ({r.TargetIdentifier})");
        this.logger.LogInformation("Local Refs:{NewLine}{LocalRefs}", FileSystemHelper.Path.NewLine, string.Join(FileSystemHelper.Path.NewLine, localRefs));

        // In order to decide whether a fake branch is required or not, first check to see if any local branches have the same commit SHA of the head SHA.
        // If they do, go ahead and checkout that branch
        // If no, go ahead and check out a new branch, using the known commit SHA as the pointer
        var localBranchesWhereCommitShaIsHead = this.repository.Branches.Where(b => !b.IsRemote && b.Tip?.Sha == headSha).ToList();

        var matchingCurrentBranch = !currentBranchName.IsNullOrEmpty()
            ? localBranchesWhereCommitShaIsHead.SingleOrDefault(b => b.Name.Canonical.Replace("/heads/", "/") == currentBranchName.Replace("/heads/", "/"))
            : null;
        if (matchingCurrentBranch != null)
        {
            this.logger.LogInformation("Checking out local branch '{CurrentBranchName}'.", currentBranchName);
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
                    this.logger.LogInformation("No local branch pointing at the commit '{HeadSha}'. Fake branch needs to be created.", headSha);
                    this.retryAction.Execute(() => this.repository.CreateBranchForPullRequestBranch(authentication));
                    break;
                default:
                    this.logger.LogInformation("Checking out local branch 'refs/heads/{LocalBranch}'.", localBranchesWhereCommitShaIsHead[0]);
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

        this.logger.LogWarning("Found more than one local branch pointing at the commit '{HeadSha}' ({BranchNames}).", headSha, csvNames);
        var mainBranch = localBranches.SingleOrDefault(n => n.Name.EquivalentTo(ConfigurationConstants.MainBranchKey));
        if (mainBranch != null)
        {
            this.logger.LogWarning("Because one of the branches is 'main', will build main. {MoveBranchMsg}", moveBranchMsg);
            Checkout(ConfigurationConstants.MainBranchKey);
        }
        else
        {
            var branchesWithoutSeparators = localBranches.Where(b => !b.Name.Friendly.Contains('/') && !b.Name.Friendly.Contains('-')).ToList();
            if (branchesWithoutSeparators.Count == 1)
            {
                var branchWithoutSeparator = branchesWithoutSeparators[0];
                this.logger.LogWarning("Choosing {BranchName} as it is the only branch without / or - in it. {MoveBranchMsg}", branchWithoutSeparator.Name.Canonical, moveBranchMsg);
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
            this.logger.LogInformation("Skipping fetching, if GitVersion does not calculate your version as expected you might need to allow fetching or use dynamic repositories");
        }
        else
        {
            var refSpecs = string.Join(", ", remote.FetchRefSpecs.Select(r => r.Specification));
            this.logger.LogInformation("Fetching from remote '{RemoteName}' using the following refspecs: {RefSpecs}.", remote.Name, refSpecs);
            this.retryAction.Execute(() => this.repository.Fetch(remote.Name, [], authentication, null));
        }
    }

    private IRemote EnsureOnlyOneRemoteIsDefined()
    {
        var remotes = this.repository.Remotes;
        var howMany = remotes.Count();

        if (howMany == 1)
        {
            var remote = remotes.Single();
            this.logger.LogInformation("One remote found ({RemoteName} -> '{RemoteUrl}').", remote.Name, remote.Url);
            if (remote.FetchRefSpecs.Any(r => r.Source == "refs/heads/*"))
                return remote;

            var allBranchesFetchRefSpec = $"+refs/heads/*:refs/remotes/{remote.Name}/*";
            this.logger.LogInformation("Adding refspec: {RefSpec}", allBranchesFetchRefSpec);
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
        var remoteTrackingReferences = this.repository.References
            .FromGlob(prefix + "*")
            .Where(r => !r.Name.Equals(headReferenceName));

        foreach (var remoteTrackingReference in remoteTrackingReferences)
        {
            var remoteTrackingReferenceName = remoteTrackingReference.Name.Canonical;
            var branchName = remoteTrackingReferenceName[prefix.Length..];
            var localReferenceName = ReferenceName.FromBranchName(branchName);

            // We do not want to touch our current branch
            if (this.repository.Head.Name.EquivalentTo(branchName)) continue;

            var localRef = this.repository.References[localReferenceName];
            if (localRef != null)
            {
                if (localRef.TargetIdentifier == remoteTrackingReference.TargetIdentifier)
                {
                    this.logger.LogInformation("Skipping update of '{RemoteTrackingReferenceName}' as it already matches the remote ref.", remoteTrackingReference.Name.Canonical);
                    continue;
                }
                var remoteRefTipId = remoteTrackingReference.ReferenceTargetId;
                if (remoteRefTipId != null)
                {
                    this.logger.LogInformation("Updating local ref '{LocalRefName}' to point at {RemoteRefTipId}.", localRef.Name.Canonical, remoteRefTipId);
                    this.retryAction.Execute(() => this.repository.References.UpdateTarget(localRef, remoteRefTipId));
                }
                continue;
            }

            this.logger.LogInformation("Creating local branch from remote tracking '{RemoteTrackingReferenceName}'.", remoteTrackingReference.Name.Canonical);
            this.repository.References.Add(localReferenceName.Canonical, remoteTrackingReference.TargetIdentifier, true);

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

        const string referencePrefix = "refs/";
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
                if (isLocalBranch)
                    this.logger.LogInformation("Creating local branch {ReferenceName}", referenceName);
                else
                    this.logger.LogInformation("Creating local branch {ReferenceName} pointing at {RepoTipId}", referenceName, repoTipId);
                this.repository.References.Add(localCanonicalName, repoTipId.Sha);
            }
            else
            {
                if (isLocalBranch)
                    this.logger.LogInformation("Updating local branch {ReferenceName} to point at {RepoTipId}", referenceName, repoTipId);
                else
                    this.logger.LogInformation("Updating local branch {ReferenceName} to match ref {CurrentBranch}", referenceName, currentBranch);
                var localRef = this.repository.References[localCanonicalName];
                if (localRef != null)
                {
                    this.retryAction.Execute(() => this.repository.References.UpdateTarget(localRef, repoTipId));
                }
            }
        }
        Checkout(localCanonicalName);
    }

    private void Checkout(string commitOrBranchSpec) => this.retryAction.Execute(() => this.repository.Checkout(commitOrBranchSpec));
}
