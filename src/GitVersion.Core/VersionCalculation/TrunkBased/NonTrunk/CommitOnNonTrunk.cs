using GitVersion.Configuration;

namespace GitVersion.VersionCalculation.TrunkBased.NonTrunk;

internal sealed class CommitOnNonTrunk : ITrunkBasedIncrementer
{
    // B  57 minutes ago  (HEAD -> feature/foo)
    // A  58 minutes ago <<--

    // B  57 minutes ago  (HEAD -> feature/foo) <<--
    // A  58 minutes ago

    public bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => !commit.HasChildIteration && !commit.GetEffectiveConfiguration(context.Configuration).IsMainBranch
            && context.SemanticVersion is null;

    public IEnumerable<IBaseVersionIncrement> GetIncrements(
        TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        if (commit.Predecessor is not null && commit.Predecessor.BranchName != commit.BranchName)
            context.Label = null;

        var effectiveConfiguration = commit.GetEffectiveConfiguration(context.Configuration);
        context.Label ??= effectiveConfiguration.GetBranchSpecificLabel(commit.BranchName, null);

        if (commit.Successor is null)
        {
            yield return new BaseVersionOperator()
            {
                Source = GetType().Name,
                BaseVersionSource = context.BaseVersionSource,
                Increment = context.Increment,
                ForceIncrement = context.ForceIncrement,
                Label = context.Label,
                AlternativeSemanticVersion = context.AlternativeSemanticVersions.Max()
            };

            context.BaseVersionSource = commit.Value;
            context.ForceIncrement = false;
        }
    }
}
