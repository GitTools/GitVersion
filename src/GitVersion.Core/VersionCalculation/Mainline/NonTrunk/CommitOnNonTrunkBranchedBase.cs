using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.Mainline.NonTrunk;

internal abstract class CommitOnNonTrunkBranchedBase(IEnvironment environment) : IIncrementer
{
    public virtual bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => !context.EffectiveConfiguration.IsMainBranch
           && commit.BranchName != iteration.BranchName
           && context.IsLastCommit(commit);

    public virtual IEnumerable<IBaseVersionIncrement> GetIncrements(
        MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        context.BaseVersionSource = commit.Value;

        var incrementForcedByBranch = iteration.Configuration.Increment == IncrementStrategy.Inherit
            ? context.EffectiveConfiguration.Increment.ToVersionField()
            : iteration.Configuration.Increment.ToVersionField();
        context.Increment = context.Increment.Consolidate(incrementForcedByBranch);

        var iterationEffectiveConfiguration = context.IterationConfiguration;
        context.Label = iterationEffectiveConfiguration.GetBranchSpecificLabel(iteration.BranchName, null, environment, context.CurrentCommit) ?? context.Label;
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
