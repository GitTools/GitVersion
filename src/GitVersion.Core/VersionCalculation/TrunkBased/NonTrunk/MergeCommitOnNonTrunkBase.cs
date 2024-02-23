using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.TrunkBased.NonTrunk;

internal abstract class MergeCommitOnNonTrunkBase : ITrunkBasedIncrementer
{
    public virtual bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => commit.HasChildIteration && !commit.Configuration.IsMainBranch && context.SemanticVersion is null;

    public virtual IEnumerable<BaseVersionV2> GetIncrements(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        if (commit.ChildIteration is null) throw new InvalidOperationException("The commit child iteration is null.");

        var baseVersion = TrunkBasedVersionStrategy.DetermineBaseVersionRecursive(
           iteration: commit.ChildIteration,
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
        }
        else if (baseVersion.AlternativeSemanticVersion is not null)
        {
            context.AlternativeSemanticVersions.Add(baseVersion.AlternativeSemanticVersion);
        }

        yield break;
    }
}
