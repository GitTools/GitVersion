namespace GitVersion.VersionCalculation.TrunkBased.NonTrunk;

internal sealed class LastMergeCommitOnNonTrunk : MergeCommitOnNonTrunkBase
{
    // *  55 minutes ago  (HEAD -> develop) <<--
    // |\
    // | B  56 minutes ago  (feature/foo)

    public override bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => base.MatchPrecondition(iteration, commit, context) && commit.Successor is null;

    public override IEnumerable<IBaseVersionIncrement> GetIncrements(
        TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        foreach (var item in base.GetIncrements(iteration, commit, context))
        {
            yield return item;
        }

        yield return new BaseVersionOperator()
        {
            Source = GetType().Name,
            BaseVersionSource = context.BaseVersionSource,
            Increment = context.Increment,
            ForceIncrement = context.ForceIncrement,
            Label = context.TargetLabel ?? context.Label,
            AlternativeSemanticVersion = context.AlternativeSemanticVersions.Max()
        };
    }
}
