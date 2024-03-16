using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.TrunkBased.Trunk;

internal abstract class CommitOnTrunkWithStableTagBase : ITrunkBasedIncrementer
{
    public virtual bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => commit.Configuration.IsMainBranch && !commit.HasChildIteration
            && context.SemanticVersion?.IsPreRelease == false;

    public virtual IEnumerable<IBaseVersionIncrement> GetIncrements(
        TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        context.BaseVersionSource = commit.Value;

        yield return new BaseVersionOperand()
        {
            Source = GetType().Name,
            SemanticVersion = context.SemanticVersion.NotNull(),
            BaseVersionSource = context.BaseVersionSource
        };

        context.Label = commit.Configuration.GetBranchSpecificLabel(commit.BranchName, null);
    }
}
