using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.Mainline.Trunk;

internal abstract class CommitOnTrunkWithPreReleaseTagBase : IIncrementer
{
    public virtual bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => commit.GetEffectiveConfiguration(context.Configuration).IsMainBranch && !commit.HasChildIteration
            && context.SemanticVersion?.IsPreRelease == true;

    public virtual IEnumerable<IBaseVersionIncrement> GetIncrements(
        MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        context.BaseVersionSource = commit.Value;

        yield return new BaseVersionOperand
        {
            Source = GetType().Name,
            BaseVersionSource = context.BaseVersionSource,
            SemanticVersion = context.SemanticVersion.NotNull()
        };
    }
}
