using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion;

// TODO 3074: test
internal sealed class IgnoredFilteringGitRepositoryDecorator : IMutatingGitRepository
{
    public IVersionFilter[] VersionFilters { get; }
    public IMutatingGitRepository Decoratee { get; }

    // NOTE: it would be better to use the interface IMutatingGitRepository for gitRepository but this would be quite cumbersome to instantiate during startup: https://greatrexpectations.com/2018/10/25/decorators-in-net-core-with-dependency-injection
    public IgnoredFilteringGitRepositoryDecorator(IIgnoredFilterProvider ignoredFilterProvider, GitRepository decoratee)
    {
        Decoratee = decoratee.NotNull();
        VersionFilters = ignoredFilterProvider.Provide();
    }

    public string Path => Decoratee.Path;

    public string WorkingDirectory => Decoratee.WorkingDirectory;

    public bool IsHeadDetached => Decoratee.IsHeadDetached;

    public IBranch Head => Decoratee.Head;

    public ITagCollection Tags => Decoratee.Tags;

    public IReferenceCollection Refs => Decoratee.Refs;

    public IBranchCollection Branches => Decoratee.Branches;

    // TODO 3074: test
    public IEnumerable<ICommit> Commits =>
        Decoratee.Commits
            .Where(IncludeVersion)
            .ToList();

    public IEnumerable<ICommit> QueryBy(CommitFilter commitFilter) =>
        Decoratee.QueryBy(commitFilter)
            .Where(IncludeVersion)
            .ToList();

    public IRemoteCollection Remotes => Decoratee.Remotes;

    public void Checkout(string commitOrBranchSpec) => Decoratee.Checkout(commitOrBranchSpec);

    public void Clone(string? sourceUrl, string? workdirPath, AuthenticationInfo auth) => Decoratee.Clone(sourceUrl, workdirPath, auth);

    public void CreateBranchForPullRequestBranch(AuthenticationInfo auth) => Decoratee.CreateBranchForPullRequestBranch(auth);

    public void Fetch(string remote, IEnumerable<string> refSpecs, AuthenticationInfo auth, string? logMessage) => Decoratee.Fetch(remote, refSpecs, auth, logMessage);

    // TODO 3074: test
    public ICommit? FindMergeBase(ICommit commit, ICommit otherCommit)
    {
        if (IncludeVersion(commit) == false)
            throw new ArgumentException($"Commit {commit.Id.Sha} is ignored by date or SHA.", nameof(commit));

        if (IncludeVersion(otherCommit) == false)
            throw new ArgumentException($"Commit {otherCommit.Id.Sha} is ignored by date or SHA.", nameof(otherCommit));

        var mergeBase = Decoratee.FindMergeBase(commit, otherCommit);

        if (mergeBase != null && IncludeVersion(mergeBase))
            return mergeBase;

        return null;
    }

    public int GetNumberOfUncommittedChanges() => Decoratee.GetNumberOfUncommittedChanges();

    private bool IncludeVersion(ICommit commit)
    {
        foreach (var filter in VersionFilters)
        {
            if (filter.Exclude(commit, out var reason))
            {
                if (reason != null)
                {
                    // TODO 3074: add log to this class
                    //this.log.Info(reason);
                }
                return false;
            }
        }
        return true;
    }
}
