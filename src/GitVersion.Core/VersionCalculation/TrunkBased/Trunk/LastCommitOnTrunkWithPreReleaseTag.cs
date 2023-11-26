using GitVersion.Configuration;

namespace GitVersion.VersionCalculation.TrunkBased.Trunk;

internal sealed class LastCommitOnTrunkWithPreReleaseTag : CommitOnTrunkWithPreReleaseTagBase
{
    // B 58 minutes ago (HEAD -> main) (tag 0.2.0-1) <<--
    // A 59 minutes ago

    public override bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => base.MatchPrecondition(iteration, commit, context) && commit.Successor is null;

    public override IEnumerable<BaseVersionV2> GetIncrements(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        foreach (var item in base.GetIncrements(iteration, commit, context))
        {
            yield return item;
        }

        if (iteration.Configuration.IsMainline)
        {
            context.Increment = commit.GetIncrementForcedByBranch();
            context.Label = commit.Configuration.GetBranchSpecificLabel(commit.BranchName, null);
            context.ForceIncrement = false;

            yield return BaseVersionV2.ShouldIncrementTrue(
                source: GetType().Name,
                baseVersionSource: context.BaseVersionSource,
                increment: context.Increment,
                label: context.Label,
                forceIncrement: context.ForceIncrement
            );

            context.Increment = VersionField.None;
            context.Label = null;
        }
    }
}
