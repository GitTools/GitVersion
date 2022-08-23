using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion;

// TODO 3074: test
internal sealed class IgnoredFilteringGitRepositoryDecorator : IMutatingGitRepository
{
    public IVersionFilter[] VersionFilters { get; }
    public IMutatingGitRepository Decoratee { get; }

    // TODO 3074: better to use interface for GitRepository but must instantiate this class explicitly in module
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

    // TODO 3074: Need to be tested
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

    // TODO 3074: on ignored commits as output or input return null
    public ICommit? FindMergeBase(ICommit commit, ICommit otherCommit) => Decoratee.FindMergeBase(commit, otherCommit);

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
