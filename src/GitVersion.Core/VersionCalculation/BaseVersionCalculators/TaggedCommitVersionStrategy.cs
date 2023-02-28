using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

/// <summary>
/// Version is extracted from all tags on the branch which are valid, and not newer than the current commit.
/// BaseVersionSource is the tag's commit.
/// Increments if the tag is not the current commit.
/// </summary>
public class TaggedCommitVersionStrategy : VersionStrategyBase
{
    private readonly IRepositoryStore repositoryStore;

    public TaggedCommitVersionStrategy(IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
        : base(versionContext) => this.repositoryStore = repositoryStore.NotNull();

    public override IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
    {
        var taggedVersions = GetSemanticVersions(configuration)
            .Select(versionTaggedCommit => CreateBaseVersion(Context, versionTaggedCommit))
            .ToList();
        var taggedVersionsOnCurrentCommit = taggedVersions.Where(version => !version.ShouldIncrement).ToList();
        return taggedVersionsOnCurrentCommit.Any() ? taggedVersionsOnCurrentCommit : taggedVersions;
    }

    private IEnumerable<SemanticVersionWithTag> GetSemanticVersions(EffectiveBranchConfiguration configuration)
    {
        var alreadyReturnedValues = new HashSet<SemanticVersionWithTag>();

        var olderThan = Context.CurrentCommit?.When;

        var semanticVersions = this.repositoryStore.GetSemanticVersionFromTags(
            Context.Configuration.LabelPrefix, Context.Configuration.SemanticVersionFormat
        ).ToList();
        ILookup<string, SemanticVersionWithTag> semanticVersionsByCommit = semanticVersions.ToLookup(element => element.Tag.Commit.Id.Sha);

        var commitsOnCurrentBranch = Context.CurrentBranch.Commits?.ToArray() ?? Array.Empty<ICommit>();
        if (commitsOnCurrentBranch.Any())
        {
            foreach (var commit in commitsOnCurrentBranch)
            {
                foreach (var semanticVersion in semanticVersionsByCommit[commit.Id.Sha])
                {
                    if (commit.When <= olderThan)
                    {
                        if (alreadyReturnedValues.Add(semanticVersion)) yield return semanticVersion;
                    }
                }
            }

            if (configuration.Value.TrackMergeTarget)
            {
                var commitsOnCurrentBranchByCommit = commitsOnCurrentBranch.ToLookup(commit => commit.Id.Sha);
                foreach (var semanticVersion in semanticVersions)
                {
                    if (semanticVersion.Tag.Commit.When > olderThan) continue;

                    var parentCommits = semanticVersion.Tag.Commit.Parents ?? Array.Empty<ICommit>();
                    if (parentCommits.Any(element => commitsOnCurrentBranchByCommit.Contains(element.Id.Sha)))
                    {
                        if (alreadyReturnedValues.Add(semanticVersion)) yield return semanticVersion;
                    }
                }
            }
        }

        if (configuration.Value.TracksReleaseBranches)
        {
            var mainBranches = this.repositoryStore.FindMainlineBranches(Context.Configuration);
            foreach (var mainBranche in mainBranches)
            {
                var commitsOnMainBranch = mainBranche.Commits?.ToArray() ?? Array.Empty<ICommit>();
                foreach (var commit in commitsOnMainBranch)
                {
                    foreach (var semanticVersion in semanticVersionsByCommit[commit.Id.Sha])
                    {
                        if (alreadyReturnedValues.Add(semanticVersion)) yield return semanticVersion;
                    }
                }
            }
        }
    }

    private BaseVersion CreateBaseVersion(GitVersionContext context, SemanticVersionWithTag semanticVersion)
    {
        var tagCommit = semanticVersion.Tag.Commit;
        var shouldUpdateVersion = tagCommit.Sha != context.CurrentCommit?.Sha;
        var baseVersion = new BaseVersion(FormatSource(semanticVersion), shouldUpdateVersion, semanticVersion.Value, tagCommit, null);
        return baseVersion;
    }

    protected virtual string FormatSource(SemanticVersionWithTag semanticVersion) => $"Git tag '{semanticVersion.Tag.Name.Friendly}'";
}
