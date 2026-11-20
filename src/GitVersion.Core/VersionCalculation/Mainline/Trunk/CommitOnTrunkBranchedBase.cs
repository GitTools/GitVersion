using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.Mainline.Trunk;

internal abstract class CommitOnTrunkBranchedBase : IIncrementer
{
    public virtual bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => context.EffectiveConfiguration.IsMainBranch
            && iteration.NumberOfCommits != 0
            && commit.BranchName != iteration.BranchName
            && context.IsLastCommit(commit);

    public virtual IEnumerable<IBaseVersionIncrement> GetIncrements(
        MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        context.BaseVersionSource = commit.Value;

        var effectiveConfiguration = context.IterationConfiguration;
        if (effectiveConfiguration.IsReleaseBranch
            && iteration.BranchName.TryGetSemanticVersion(effectiveConfiguration, out var element))
        {
            context.AddAlternativeSemanticVersion(element.Value,
                new SemanticVersionSource("Version in branch name", element.Value, null, VersionField.None));
        }

        var incrementForcedByBranch = iteration.Configuration.Increment == IncrementStrategy.Inherit
            ? context.EffectiveConfiguration.Increment.ToVersionField()
            : iteration.Configuration.Increment.ToVersionField();
        context.Increment = incrementForcedByBranch;

        var iterationEffectiveConfiguration = context.IterationConfiguration;
        context.Label = iterationEffectiveConfiguration.GetBranchSpecificLabel(
            iteration.BranchName, null, context.Environment, context.CurrentCommit) ?? context.Label;
        context.ForceIncrement = true;

        yield return CreateIncrement(new BaseVersionOperator
        {
            Source = iterationEffectiveConfiguration.IsMainBranch
                ? nameof(CommitOnTrunkBranchedToTrunk) : nameof(CommitOnTrunkBranchedToNonTrunk),
            BaseVersionSource = context.BaseVersionSource,
            Increment = context.Increment,
            ForceIncrement = context.ForceIncrement,
            Label = context.Label,
            AlternativeSemanticVersion = context.AlternativeSemanticVersions.Max(),
            AlternativeSemVerSource = context.GetAlternativeSemVerSource()
        }, commit, context);
    }

    protected virtual IBaseVersionIncrement CreateIncrement(
        BaseVersionOperator operation, MainlineCommit commit, MainlineContext context) => operation;
}
