namespace GitVersion.VersionCalculation.TrunkBased.NonTrunk;

internal sealed class LastCommitOnNonTrunkWithPreReleaseTag : CommitOnNonTrunkWithPreReleaseTagBase
{
    // B 57 minutes ago  (HEAD -> feature/foo) (tag 1.2.3-1) <<--
    // A 58 minutes ago

    public override bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => base.MatchPrecondition(iteration, commit, context) && commit.Successor is null;

    public override IEnumerable<BaseVersionV2> GetIncrements(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        foreach (var item in base.GetIncrements(iteration, commit, context))
        {
            yield return item;
        }

        yield return BaseVersionV2.ShouldIncrementTrue(
            source: GetType().Name,
            baseVersionSource: context.BaseVersionSource,
            increment: context.Increment,
            label: context.Label,
            forceIncrement: false
        );
    }
}
