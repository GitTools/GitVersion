using GitVersion.Configuration;

namespace GitVersion.VersionCalculation.TrunkBased.Trunk;

internal sealed class LastCommitOnTrunkWithPreReleaseTag : CommitOnTrunkWithPreReleaseTagBase
{
    // B 58 minutes ago (HEAD -> main) (tag 0.2.0-1) <<--
    // A 59 minutes ago

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
            context.Increment = commit.GetIncrementForcedByBranch(context.Configuration);

            var effectiveConfiguration = commit.GetEffectiveConfiguration(context.Configuration);
            context.Label = effectiveConfiguration.GetBranchSpecificLabel(commit.BranchName, null);
            context.ForceIncrement = false;

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
