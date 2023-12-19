using GitVersion.Configuration;

namespace GitVersion.VersionCalculation.TrunkBased.NonTrunk;

internal sealed class CommitOnNonTrunk : ITrunkBasedIncrementer
{
    // B  57 minutes ago  (HEAD -> feature/foo)
    // A  58 minutes ago <<--

    // B  57 minutes ago  (HEAD -> feature/foo) <<--
    // A  58 minutes ago

    public bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => commit.ChildIteration is null && !commit.Configuration.IsMainline && context.SemanticVersion is null;

    public IEnumerable<BaseVersionV2> GetIncrements(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        if (commit.Predecessor is not null && commit.Predecessor.BranchName != commit.BranchName)
            context.Label = null;
        context.Label ??= commit.Configuration.GetBranchSpecificLabel(commit.BranchName, null);

        if (commit.Successor is null)
        {
            yield return BaseVersionV2.ShouldIncrementTrue(
                source: GetType().Name,
                baseVersionSource: context.BaseVersionSource,
                increment: context.Increment,
                label: context.Label,
                forceIncrement: context.ForceIncrement,
                alternativeSemanticVersion: context.AlternativeSemanticVersions.Max()
            );

            context.BaseVersionSource = commit.Value;
        }
    }
}
