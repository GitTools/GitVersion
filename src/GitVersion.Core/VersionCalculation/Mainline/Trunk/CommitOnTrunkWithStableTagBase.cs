using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.Mainline.Trunk;

internal abstract class CommitOnTrunkWithStableTagBase : IIncrementer
{
    public virtual bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => commit.GetEffectiveConfiguration(context.Configuration).IsMainBranch && !commit.HasChildIteration
            && context.SemanticVersion?.IsPreRelease == false;

    public virtual IEnumerable<IBaseVersionIncrement> GetIncrements(
        MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        context.BaseVersionSource = commit.Value;

        yield return new BaseVersionOperand
        {
            Source = GetType().Name,
            SemanticVersion = context.SemanticVersion.NotNull(),
            BaseVersionSource = context.BaseVersionSource
        };

        var effectiveConfiguration = commit.GetEffectiveConfiguration(context.Configuration);
        context.Label = effectiveConfiguration.GetBranchSpecificLabel(commit.BranchName, null);
    }
}
