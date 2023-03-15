using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

/// <summary>
/// Version is extracted from all tags on the branch which are valid, and not newer than the current commit.
/// BaseVersionSource is the tag's commit.
/// Increments if the tag is not the current commit.
/// </summary>
public sealed class TaggedCommitVersionStrategy : VersionStrategyBase
{
    private readonly IRepositoryStore repositoryStore;

    public TaggedCommitVersionStrategy(IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
        : base(versionContext) => this.repositoryStore = repositoryStore.NotNull();

    public override IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
        => GetSemanticVersions(configuration).Select(element => CreateBaseVersion(configuration, element));

    private IEnumerable<SemanticVersionWithTag> GetSemanticVersions(EffectiveBranchConfiguration configuration)
    {
        var alreadyReturnedValues = new HashSet<SemanticVersionWithTag>();

        var olderThan = Context.CurrentCommit?.When;

        var label = configuration.Value.GetBranchSpecificLabel(Context.CurrentBranch.Name, null);

        var semanticVersions = this.repositoryStore.GetTaggedSemanticVersions(
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
                    if (commit.When <= olderThan && semanticVersion.Value.IsMatchForBranchSpecificLabel(label))
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
                    if (parentCommits.Any(element => commitsOnCurrentBranchByCommit.Contains(element.Id.Sha))
                        && semanticVersion.Value.IsMatchForBranchSpecificLabel(label))
                    {
                        if (alreadyReturnedValues.Add(semanticVersion)) yield return semanticVersion;
                    }
                }
            }
        }

        if (configuration.Value.TracksReleaseBranches)
        {
            foreach (var mainBranche in this.repositoryStore.FindMainlineBranches(Context.Configuration))
            {
                foreach (var commit in mainBranche.Commits?.ToArray() ?? Array.Empty<ICommit>())
                {
                    foreach (var semanticVersion in semanticVersionsByCommit[commit.Id.Sha])
                    {
                        if (semanticVersion.Value.IsMatchForBranchSpecificLabel(label))
                        {
                            if (alreadyReturnedValues.Add(semanticVersion)) yield return semanticVersion;
                        }
                    }
                }
            }
        }
    }

    private BaseVersion CreateBaseVersion(EffectiveBranchConfiguration configuration, SemanticVersionWithTag semanticVersion)
    {
        var tagCommit = semanticVersion.Tag.Commit;
        var shouldUpdateVersion = tagCommit.Sha != Context.CurrentCommit?.Sha;

        if (!shouldUpdateVersion && !configuration.Value.Label.IsNullOrEmpty() && !semanticVersion.Value.PreReleaseTag.HasTag())
        {
            return new BaseVersion(
                $"Git tag '{semanticVersion.Tag.Name.Friendly}'", true, semanticVersion.Value, tagCommit, null
            );
        }
        else
        {
            return new BaseVersion(
                $"Git tag '{semanticVersion.Tag.Name.Friendly}'", shouldUpdateVersion, semanticVersion.Value, tagCommit, null
            );
        }
    }
}
