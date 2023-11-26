namespace GitVersion.VersionCalculation.TrunkBased.NonTrunk;

internal sealed class LastMergeCommitOnNonTrunk : MergeCommitOnNonTrunkBase
{
    // *  55 minutes ago  (HEAD -> develop) <<--
    // |\
    // | B  56 minutes ago  (feature/foo)

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
            label: context.TargetLabel ?? context.Label,
            forceIncrement: context.ForceIncrement,
            alternativeSemanticVersion: context.AlternativeSemanticVersions.Max()
        );
    }
}
