using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.Mainline.Trunk;

internal abstract class CommitOnTrunkBranchedBase : IIncrementer
{
    public virtual bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => commit.GetEffectiveConfiguration(context.Configuration).IsMainBranch
            && commit.BranchName != iteration.BranchName
            && commit.Successor is null;

    public virtual IEnumerable<IBaseVersionIncrement> GetIncrements(
        MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        context.BaseVersionSource = commit.Value;

        var effectiveConfiguration = iteration.GetEffectiveConfiguration(context.Configuration);
        if (iteration.GetEffectiveConfiguration(context.Configuration).IsReleaseBranch
            && iteration.BranchName.TryGetSemanticVersion(out var element, effectiveConfiguration))
        {
            context.AlternativeSemanticVersions.Add(element.Value);
        }

        var incrementForcedByBranch = iteration.Configuration.Increment == IncrementStrategy.Inherit
            ? commit.GetIncrementForcedByBranch(context.Configuration)
            : iteration.Configuration.Increment.ToVersionField();
        context.Increment = incrementForcedByBranch;

        var iterationEffectiveConfiguration = iteration.GetEffectiveConfiguration(context.Configuration);
        context.Label = iterationEffectiveConfiguration.GetBranchSpecificLabel(iteration.BranchName, null) ?? context.Label;
        context.ForceIncrement = true;

        yield return new BaseVersionOperator()
        {
            Source = GetType().Name,
            BaseVersionSource = context.BaseVersionSource,
            Increment = context.Increment,
            ForceIncrement = context.ForceIncrement,
            Label = context.Label,
            AlternativeSemanticVersion = context.AlternativeSemanticVersions.Max()
        };
    }
}
