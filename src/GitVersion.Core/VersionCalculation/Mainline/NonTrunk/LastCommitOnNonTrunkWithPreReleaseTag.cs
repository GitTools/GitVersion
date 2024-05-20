namespace GitVersion.VersionCalculation.Mainline.NonTrunk;

internal sealed class LastCommitOnNonTrunkWithPreReleaseTag : CommitOnNonTrunkWithPreReleaseTagBase
{
    // B 57 minutes ago  (HEAD -> feature/foo) (tag 1.2.3-1) <<--
    // A 58 minutes ago

    public override bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => base.MatchPrecondition(iteration, commit, context) && commit.Successor is null;

    public override IEnumerable<IBaseVersionIncrement> GetIncrements(
        MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
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
            ForceIncrement = false,
            Label = context.Label,
            AlternativeSemanticVersion = context.AlternativeSemanticVersions.Max()
        };
    }
}
