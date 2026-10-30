using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.Mainline.Trunk;

internal abstract class MergeCommitOnTrunkBase : IIncrementer
{
    public virtual bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => context.SemanticVersion is null && context.HasChildHistory(commit) && !context.IsMergedMainline(commit)
            && context.GetEffectiveConfiguration(commit).IsMainBranch;

    public virtual IEnumerable<IBaseVersionIncrement> GetIncrements(
        MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        if (commit.ChildIteration is null)
        {
            throw new InvalidOperationException("The commit child iteration is null.");
        }

        return GetIncrementsInternal();

        IEnumerable<IBaseVersionIncrement> GetIncrementsInternal()
        {
            var baseVersion = MainlineVersionCalculator.GetOrCreate(iteration, context).CalculateBaseVersion(
                commit.ChildIteration, context.CreateChildContext());

            context.Label ??= baseVersion.Operator?.Label;

            var increment = VersionField.None;

            if (!context.GetEffectiveConfiguration(commit).PreventIncrementOfMergedBranch)
            {
                increment = increment.Consolidate(context.Increment);
            }

            if (!context.GetEffectiveConfiguration(commit.ChildIteration).PreventIncrementWhenBranchMerged)
            {
                increment = increment.Consolidate(baseVersion.Operator?.Increment);
            }

            if (context.GetEffectiveConfiguration(commit).CommitMessageIncrementing != CommitMessageIncrementMode.Disabled)
            {
                increment = increment.Consolidate(context.CommitIncrement);
            }
            context.Increment = increment;

            if (baseVersion.BaseVersionSource is not null)
            {
                context.BaseVersionSource = baseVersion.BaseVersionSource;
                context.SemanticVersion = baseVersion.SemanticVersion;
                context.SemVerSource = baseVersion.GetBaselineSemVerSource();
                context.ForceIncrement = baseVersion.Operator?.ForceIncrement ?? false;
            }
            else if (baseVersion.Operator?.AlternativeSemanticVersion is not null)
            {
                context.AddAlternativeSemanticVersion(baseVersion.Operator.AlternativeSemanticVersion,
                baseVersion.Operator.AlternativeSemVerSource ?? new SemanticVersionSource(
                    "Alternative semantic version", baseVersion.Operator.AlternativeSemanticVersion, null, VersionField.None));
            }

            if (context.SemanticVersion is not null)
            {
                yield return new BaseVersionOperand
                {
                    Source = GetType().Name,
                    BaseVersionSource = context.BaseVersionSource,
                    SemanticVersion = context.SemanticVersion.NotNull(),
                    SemVerSource = context.SemVerSource
                };
            }

            yield return new BaseVersionOperator
            {
                Source = GetType().Name,
                BaseVersionSource = context.BaseVersionSource,
                Increment = context.Increment,
                ForceIncrement = context.ForceIncrement,
                Label = context.Label,
                AlternativeSemanticVersion = context.AlternativeSemanticVersions.Max(),
                AlternativeSemVerSource = context.GetAlternativeSemVerSource()
            };

            context.BaseVersionSource = commit.Value;
        }
    }
}
