using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.TrunkBased.NonTrunk;

internal abstract class CommitOnNonTrunkWithPreReleaseTagBase : ITrunkBasedIncrementer
{
    public virtual bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => !commit.HasChildIteration && !commit.Configuration.IsMainBranch
            && context.SemanticVersion?.IsPreRelease == true;

    public virtual IEnumerable<IBaseVersionIncrement> GetIncrements(
        TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        context.BaseVersionSource = commit.Value;

        yield return new BaseVersionOperand()
        {
            Source = GetType().Name,
            BaseVersionSource = context.BaseVersionSource,
            SemanticVersion = context.SemanticVersion.NotNull()
        };

        context.Increment = commit.GetIncrementForcedByBranch();
        context.Label = commit.Configuration.GetBranchSpecificLabel(commit.BranchName, null);
    }
}
