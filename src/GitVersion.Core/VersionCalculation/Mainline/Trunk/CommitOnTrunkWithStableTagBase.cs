using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.Mainline.Trunk;

internal abstract class CommitOnTrunkWithStableTagBase : IIncrementer
{
    public virtual bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => context.GetEffectiveConfiguration(commit).IsMainBranch
            && context.SemanticVersion?.IsPreRelease == false;

    public virtual IEnumerable<IBaseVersionIncrement> GetIncrements(
        MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        context.BaseVersionSource = commit.Value;

        yield return new BaseVersionOperand
        {
            Source = GetType().Name,
            SemanticVersion = context.SemanticVersion.NotNull(),
            SemVerSource = context.SemVerSource,
            BaseVersionSource = context.BaseVersionSource
        };

        var effectiveConfiguration = context.GetEffectiveConfiguration(commit);
        context.Label = effectiveConfiguration.GetBranchSpecificLabel(commit.BranchName, null, context.Environment, context.CurrentCommit);
    }
}
