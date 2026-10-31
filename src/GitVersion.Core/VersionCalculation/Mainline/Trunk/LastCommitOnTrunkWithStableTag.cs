namespace GitVersion.VersionCalculation.Mainline.Trunk;

internal sealed class LastCommitOnTrunkWithStableTag : CommitOnTrunkWithStableTagBase
{
    // B  58 minutes ago  (HEAD -> main) (tag 0.2.0) <<--
    // A  59 minutes ago

    public override bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => base.MatchPrecondition(iteration, commit, context) && context.IsLastCommit(commit);

    public override IEnumerable<IBaseVersionIncrement> GetIncrements(
        MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        foreach (var item in base.GetIncrements(iteration, commit, context))
        {
            yield return item;
        }

        if (!context.GetEffectiveConfiguration(iteration).IsMainBranch)
        {
            yield break;
        }

        context.ForceIncrement = true;

        yield return new BaseVersionOperator
        {
            Source = GetType().Name,
            BaseVersionSource = context.BaseVersionSource,
            Increment = context.Increment,
            ForceIncrement = context.ForceIncrement,
            Label = context.Label,
            AlternativeSemanticVersion = context.AlternativeSemanticVersions.Max(),
            AlternativeSemVerSource = context.GetAlternativeSemVerSource()
        };
    }
}
