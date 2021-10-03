using GitVersion.Common;

namespace GitVersion.VersionCalculation;

/// <summary>
/// Version is extracted from all tags on the branch which are valid, and not newer than the current commit.
/// BaseVersionSource is the tag's commit.
/// Increments if the tag is not the current commit.
/// </summary>
public class TaggedCommitVersionStrategy : VersionStrategyBase
{
    private readonly IRepositoryStore repositoryStore;

    public TaggedCommitVersionStrategy(IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext) : base(versionContext) => this.repositoryStore = repositoryStore ?? throw new ArgumentNullException(nameof(repositoryStore));

    public override IEnumerable<BaseVersion> GetVersions() =>
        GetTaggedVersions(Context.CurrentBranch, Context.CurrentCommit?.When);

    internal IEnumerable<BaseVersion> GetTaggedVersions(IBranch? currentBranch, DateTimeOffset? olderThan)
    {
        if (currentBranch is null)
            return Enumerable.Empty<BaseVersion>();
        var versionTags = this.repositoryStore.GetValidVersionTags(Context.Configuration?.GitTagPrefix, olderThan);
        var versionTagsByCommit = versionTags.ToLookup(vt => vt.Item3.Id.Sha);
        var commitsOnBranch = currentBranch.Commits;
        var versionTagsOnBranch = commitsOnBranch.SelectMany(commit => versionTagsByCommit[commit.Id.Sha]);
        var versionTaggedCommits = versionTagsOnBranch.Select(t => new VersionTaggedCommit(t.Item3, t.Item2, t.Item1.Name.Friendly));
        var taggedVersions = versionTaggedCommits.Select(versionTaggedCommit => CreateBaseVersion(Context, versionTaggedCommit)).ToList();
        var taggedVersionsOnCurrentCommit = taggedVersions.Where(version => !version.ShouldIncrement).ToList();
        return taggedVersionsOnCurrentCommit.Any() ? taggedVersionsOnCurrentCommit : taggedVersions;
    }

    private BaseVersion CreateBaseVersion(GitVersionContext context, VersionTaggedCommit version)
    {
        var shouldUpdateVersion = version.Commit.Sha != context.CurrentCommit?.Sha;
        var baseVersion = new BaseVersion(FormatSource(version), shouldUpdateVersion, version.SemVer, version.Commit, null);
        return baseVersion;
    }

    protected virtual string FormatSource(VersionTaggedCommit version) => $"Git tag '{version.Tag}'";

    protected class VersionTaggedCommit
    {
        public string Tag;
        public ICommit Commit;
        public SemanticVersion SemVer;

        public VersionTaggedCommit(ICommit commit, SemanticVersion semVer, string tag)
        {
            this.Tag = tag;
            this.Commit = commit;
            this.SemVer = semVer;
        }

        public override string ToString() => $"{this.Tag} | {this.Commit} | {this.SemVer}";
    }
}
