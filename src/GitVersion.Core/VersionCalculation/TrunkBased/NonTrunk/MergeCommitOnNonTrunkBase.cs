namespace GitVersion.VersionCalculation.TrunkBased.NonTrunk;

internal abstract class MergeCommitOnNonTrunkBase : ITrunkBasedIncrementer
{
    public virtual bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => commit.ChildIteration is not null && !commit.Configuration.IsMainline && context.SemanticVersion is null;

    public virtual IEnumerable<BaseVersionV2> GetIncrements(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        var baseVersion = TrunkBasedVersionStrategy.DetermineBaseVersionRecursive(
           iteration: commit.ChildIteration!,
           targetLabel: context.TargetLabel,
           taggedSemanticVersions: context.TaggedSemanticVersions
       );

        context.Label ??= baseVersion.Label;

        if (commit.Configuration.PreventIncrementOfMergedBranchVersion)
            context.Increment = baseVersion.Increment;
        else
        {
            context.Increment = context.Increment.Consolidate(baseVersion.Increment);
        }
        if (commit.Configuration.CommitMessageIncrementing != CommitMessageIncrementMode.Disabled)
            context.Increment = context.Increment.Consolidate(commit.Increment);

        if (baseVersion.BaseVersionSource is not null)
        {
            context.BaseVersionSource = baseVersion.BaseVersionSource;
            context.SemanticVersion = baseVersion.GetSemanticVersion();
        }
        else if (baseVersion.AlternativeSemanticVersion is not null)
        {
            context.AlternativeSemanticVersions.Add(baseVersion.AlternativeSemanticVersion);
        }

        yield break;
    }
}
