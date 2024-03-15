using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.TrunkBased.Trunk;

internal abstract class MergeCommitOnTrunkBase : ITrunkBasedIncrementer
{
    public virtual bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => commit.HasChildIteration && commit.Configuration.IsMainBranch && context.SemanticVersion is null;

    public virtual IEnumerable<BaseVersionV2> GetIncrements(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        if (commit.ChildIteration is null) throw new InvalidOperationException("The commit child iteration is null.");

        var baseVersion = TrunkBasedVersionStrategy.DetermineBaseVersionRecursive(
           iteration: commit.ChildIteration!,
           targetLabel: context.TargetLabel
       );

        context.Label ??= baseVersion.Label;

        var increment = VersionField.None;
        if (!commit.Configuration.PreventIncrementOfMergedBranch)
        {
            increment = increment.Consolidate(context.Increment);
        }
        if (!commit.ChildIteration.Configuration.PreventIncrementWhenBranchMerged)
        {
            increment = increment.Consolidate(baseVersion.Increment);
        }
        if (commit.Configuration.CommitMessageIncrementing != CommitMessageIncrementMode.Disabled)
        {
            increment = increment.Consolidate(commit.Increment);
        }
        context.Increment = increment;

        if (baseVersion.BaseVersionSource is not null)
        {
            context.BaseVersionSource = baseVersion.BaseVersionSource;
            context.SemanticVersion = baseVersion.GetSemanticVersion();
            context.ForceIncrement = baseVersion.ForceIncrement;
        }
        else if (baseVersion.AlternativeSemanticVersion is not null)
        {
            context.AlternativeSemanticVersions.Add(baseVersion.AlternativeSemanticVersion);
        }

        if (context.SemanticVersion is not null)
        {
            yield return BaseVersionV2.ShouldIncrementFalse(
                source: GetType().Name,
                baseVersionSource: context.BaseVersionSource,
                semanticVersion: context.SemanticVersion.NotNull()
            );
        }

        yield return BaseVersionV2.ShouldIncrementTrue(
            source: GetType().Name,
            baseVersionSource: context.BaseVersionSource,
            increment: context.Increment,
            label: context.Label,
            forceIncrement: context.ForceIncrement,
            alternativeSemanticVersion: context.AlternativeSemanticVersions.Max()
        );

        context.BaseVersionSource = commit.Value;
    }
}
