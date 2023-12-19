using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.TrunkBased.Trunk;

internal abstract class CommitOnTrunkWithPreReleaseTagBase : ITrunkBasedIncrementer
{
    public virtual bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => commit.Configuration.IsMainline && commit.ChildIteration is null
            && context.SemanticVersion is not null && context.SemanticVersion.IsPreRelease;

    public virtual IEnumerable<BaseVersionV2> GetIncrements(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        context.BaseVersionSource = commit.Value;

        yield return BaseVersionV2.ShouldIncrementFalse(
            source: GetType().Name,
            baseVersionSource: context.BaseVersionSource,
            semanticVersion: context.SemanticVersion.NotNull()
        );

        context.Increment = VersionField.None;
    }
}
