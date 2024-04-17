namespace GitVersion.VersionCalculation.TrunkBased.Trunk;

internal sealed class LastCommitOnTrunkWithStableTag : CommitOnTrunkWithStableTagBase
{
    // B  58 minutes ago  (HEAD -> main) (tag 0.2.0) <<--
    // A  59 minutes ago

    public override bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => base.MatchPrecondition(iteration, commit, context) && commit.Successor is null;

    public override IEnumerable<IBaseVersionIncrement> GetIncrements(
        TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        foreach (var item in base.GetIncrements(iteration, commit, context))
        {
            yield return item;
        }

        if (iteration.Configuration.IsMainBranch == true)
        {
            context.ForceIncrement = true;

            yield return new BaseVersionOperator()
            {
                Source = GetType().Name,
                BaseVersionSource = context.BaseVersionSource,
                Increment = context.Increment,
                ForceIncrement = context.ForceIncrement,
                Label = context.Label,
                AlternativeSemanticVersion = context.AlternativeSemanticVersions.Max()
            };
        }
    }
}
