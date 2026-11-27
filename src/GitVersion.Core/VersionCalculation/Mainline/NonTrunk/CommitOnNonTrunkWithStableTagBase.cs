using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.Mainline.NonTrunk;

internal abstract class CommitOnNonTrunkWithStableTagBase(IEnvironment environment) : IIncrementer
{
    public virtual bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => !context.EffectiveConfiguration.IsMainBranch
           && context.SemanticVersion?.IsPreRelease == false;

    public virtual IEnumerable<IBaseVersionIncrement> GetIncrements(
        MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        context.BaseVersionSource = commit.Value;

        yield return new BaseVersionOperand
        {
            Source = GetType().Name,
            BaseVersionSource = context.BaseVersionSource,
            SemanticVersion = context.SemanticVersion.NotNull(),
            SemVerSource = context.SemVerSource
        };

        context.Increment = context.EffectiveConfiguration.Increment.ToVersionField();
        var effectiveConfiguration = context.EffectiveConfiguration;
        context.Label = effectiveConfiguration.GetBranchSpecificLabel(commit.BranchName, null, environment, context.CurrentCommit);
    }
}
