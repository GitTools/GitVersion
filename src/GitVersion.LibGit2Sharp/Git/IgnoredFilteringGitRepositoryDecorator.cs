using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.VersionCalculation;

namespace GitVersion;

// TODO 3074: test
internal sealed class IgnoredFilteringGitRepositoryDecorator : IMutatingGitRepository
{
    private readonly ILog log;
    private readonly IVersionFilter[] versionFilters;
    private readonly IMutatingGitRepository decoratee;

    // NOTE: it would be better to use the interface IMutatingGitRepository for gitRepository but this would be quite cumbersome to instantiate during startup: https://greatrexpectations.com/2018/10/25/decorators-in-net-core-with-dependency-injection
    public IgnoredFilteringGitRepositoryDecorator(IIgnoredFilterProvider ignoredFilterProvider, ILog log, GitRepository decoratee)
    {
        this.decoratee = decoratee.NotNull();
        versionFilters = ignoredFilterProvider.Provide();
        this.log = log;
    }

    public string Path => decoratee.Path;

    public string WorkingDirectory => decoratee.WorkingDirectory;

    public bool IsHeadDetached => decoratee.IsHeadDetached;

    public IBranch Head => decoratee.Head;

    public ITagCollection Tags => decoratee.Tags;

    public IReferenceCollection Refs => decoratee.Refs;

    public IBranchCollection Branches => decoratee.Branches;

    // TODO 3074: test
    public IEnumerable<ICommit> Commits =>
        decoratee.Commits.Select(DecorateCommit);

    // TODO 3074: test
    public IEnumerable<ICommit> QueryBy(CommitFilter commitFilter) =>
        decoratee.QueryBy(commitFilter)
            .Select(DecorateCommit);

    public IRemoteCollection Remotes => decoratee.Remotes;

    public void Checkout(string commitOrBranchSpec) => decoratee.Checkout(commitOrBranchSpec);

    public void Clone(string? sourceUrl, string? workdirPath, AuthenticationInfo auth) => decoratee.Clone(sourceUrl, workdirPath, auth);

    public void CreateBranchForPullRequestBranch(AuthenticationInfo auth) => decoratee.CreateBranchForPullRequestBranch(auth);

    public void Fetch(string remote, IEnumerable<string> refSpecs, AuthenticationInfo auth, string? logMessage) => decoratee.Fetch(remote, refSpecs, auth, logMessage);

    // TODO 3074: test
    public ICommit? FindMergeBase(ICommit commit, ICommit otherCommit)
    {
        var mergeBase = decoratee.FindMergeBase(commit, otherCommit);

        if (mergeBase == null)
            return null;

        return DecorateCommit(mergeBase);
    }

    // NOTE: filter out those with an old date as a performance measure in order to be able to introduce a clip for big repositories.
    private ICommit DecorateCommit(ICommit commit) =>
        IncludeVersion(commit) ?
            commit :
            new IgnoredCommit(commit, IgnoredState.Ignored);

    public int GetNumberOfUncommittedChanges() => decoratee.GetNumberOfUncommittedChanges();

    private bool IncludeVersion(ICommit commit)
    {
        foreach (var filter in versionFilters)
        {
            if (filter.Exclude(commit, out var reason))
            {
                if (reason != null)
                    this.log.Info(reason);

                return false;
            }
        }
        return true;
    }
}
