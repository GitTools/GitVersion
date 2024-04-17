using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.TrunkBased.Trunk;

internal abstract class MergeCommitOnTrunkBase : ITrunkBasedIncrementer
{
    public virtual bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => commit.HasChildIteration && commit.GetEffectiveConfiguration(context.Configuration).IsMainBranch && context.SemanticVersion is null;

    public virtual IEnumerable<IBaseVersionIncrement> GetIncrements(
        TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        if (commit.ChildIteration is null) throw new InvalidOperationException("The commit child iteration is null.");

        var baseVersion = TrunkBasedVersionStrategy.DetermineBaseVersionRecursive(
           iteration: commit.ChildIteration!,
           targetLabel: context.TargetLabel,
           incrementStrategyFinder: context.IncrementStrategyFinder,
           configuration: context.Configuration
       );

        context.Label ??= baseVersion.Operator?.Label;

        var increment = VersionField.None;

        if (!commit.GetEffectiveConfiguration(context.Configuration).PreventIncrementOfMergedBranch)
        {
            increment = increment.Consolidate(context.Increment);
        }

        if (!commit.ChildIteration.GetEffectiveConfiguration(context.Configuration).PreventIncrementWhenBranchMerged)
        {
            increment = increment.Consolidate(baseVersion.Operator?.Increment);
        }

        if (commit.GetEffectiveConfiguration(context.Configuration).CommitMessageIncrementing != CommitMessageIncrementMode.Disabled)
        {
            increment = increment.Consolidate(commit.Increment);
        }
        context.Increment = increment;

        if (baseVersion.BaseVersionSource is not null)
        {
            context.BaseVersionSource = baseVersion.BaseVersionSource;
            context.SemanticVersion = baseVersion.SemanticVersion;
            context.ForceIncrement = baseVersion.Operator?.ForceIncrement ?? false;
        }
        else if (baseVersion.Operator?.AlternativeSemanticVersion is not null)
        {
            context.AlternativeSemanticVersions.Add(baseVersion.Operator.AlternativeSemanticVersion);
        }

        if (context.SemanticVersion is not null)
        {
            yield return new BaseVersionOperand()
            {
                Source = GetType().Name,
                BaseVersionSource = context.BaseVersionSource,
                SemanticVersion = context.SemanticVersion.NotNull()
            };
        }

        yield return new BaseVersionOperator()
        {
            Source = GetType().Name,
            BaseVersionSource = context.BaseVersionSource,
            Increment = context.Increment,
            ForceIncrement = context.ForceIncrement,
            Label = context.Label,
            AlternativeSemanticVersion = context.AlternativeSemanticVersions.Max()
        };

        context.BaseVersionSource = commit.Value;
    }
}
