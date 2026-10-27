using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.Mainline.NonTrunk;

internal abstract class CommitOnNonTrunkBranchedBase : IIncrementer
{
    public virtual bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => !context.GetEffectiveConfiguration(commit).IsMainBranch
           && commit.BranchName != iteration.BranchName
           && context.IsLastCommit(commit);

    public virtual IEnumerable<IBaseVersionIncrement> GetIncrements(
        MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        context.BaseVersionSource = commit.Value;

        var incrementForcedByBranch = iteration.Configuration.Increment == IncrementStrategy.Inherit
            ? context.GetIncrementForcedByBranch(commit)
            : iteration.Configuration.Increment.ToVersionField();
        context.Increment = context.Increment.Consolidate(incrementForcedByBranch);

        var iterationEffectiveConfiguration = context.GetEffectiveConfiguration(iteration);
        context.Label = iterationEffectiveConfiguration.GetBranchSpecificLabel(iteration.BranchName, null, context.Environment, context.CurrentCommit) ?? context.Label;
        context.ForceIncrement = true;

        yield return new BaseVersionOperator
        {
            Source = GetType().Name,
            BaseVersionSource = null,
            Increment = VersionField.None,
            ForceIncrement = false,
            Label = context.Label,
            AlternativeSemanticVersion = context.AlternativeSemanticVersions.Max(),
            AlternativeSemVerSource = context.GetAlternativeSemVerSource()
        };
    }
}
