using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.Mainline.NonTrunk;

internal abstract class MergeCommitOnNonTrunkBase : IIncrementer
{
    public virtual bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => commit.HasChildIteration
           && !commit.GetEffectiveConfiguration(context.Configuration).IsMainBranch
           && context.SemanticVersion is null;

    public virtual IEnumerable<IBaseVersionIncrement> GetIncrements(
        MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        if (commit.ChildIteration is null) throw new InvalidOperationException("The commit child iteration is null.");

        var baseVersion = MainlineVersionStrategy.DetermineBaseVersionRecursive(
           iteration: commit.ChildIteration,
           targetLabel: context.TargetLabel,
           incrementStrategyFinder: context.IncrementStrategyFinder,
           configuration: context.Configuration,
           repository: context.Repository,
           gitverContext: context.GitverContext
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
