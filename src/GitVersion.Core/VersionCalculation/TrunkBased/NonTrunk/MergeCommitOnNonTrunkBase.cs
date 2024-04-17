using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.TrunkBased.NonTrunk;

internal abstract class MergeCommitOnNonTrunkBase : ITrunkBasedIncrementer
{
    public virtual bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => commit.HasChildIteration && !commit.GetEffectiveConfiguration(context.Configuration).IsMainBranch
            && context.SemanticVersion is null;

    public virtual IEnumerable<IBaseVersionIncrement> GetIncrements(
        TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        if (commit.ChildIteration is null) throw new InvalidOperationException("The commit child iteration is null.");

        var baseVersion = TrunkBasedVersionStrategy.DetermineBaseVersionRecursive(
           iteration: commit.ChildIteration,
           targetLabel: context.TargetLabel,
           incrementStrategyFinder: context.IncrementStrategyFinder,
           configuration: context.Configuration
       );

        context.Label ??= baseVersion.Operator?.Label;

        var effectiveConfiguration = commit.GetEffectiveConfiguration(context.Configuration);
        var increment = VersionField.None;
        if (!effectiveConfiguration.PreventIncrementOfMergedBranch)
        {
            increment = increment.Consolidate(context.Increment);
        }

        if (!effectiveConfiguration.PreventIncrementWhenBranchMerged)
        {
            increment = increment.Consolidate(baseVersion.Operator?.Increment);
        }

        if (effectiveConfiguration.CommitMessageIncrementing != CommitMessageIncrementMode.Disabled)
        {
            increment = increment.Consolidate(commit.Increment);
        }
        context.Increment = increment;

        if (baseVersion.BaseVersionSource is not null)
        {
            context.BaseVersionSource = baseVersion.BaseVersionSource;
            context.SemanticVersion = baseVersion.SemanticVersion;
        }
        else
        {
            if (baseVersion.SemanticVersion != SemanticVersion.Empty)
            {
                context.AlternativeSemanticVersions.Add(baseVersion.SemanticVersion);
            }

            if (baseVersion.Operator?.AlternativeSemanticVersion is not null)
            {
                context.AlternativeSemanticVersions.Add(baseVersion.Operator.AlternativeSemanticVersion);
            }
        }

        yield break;
    }
}
