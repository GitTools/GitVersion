namespace GitVersion.VersionCalculation.TrunkBased.NonTrunk;

internal sealed class FirstCommitOnRelease : ITrunkBasedIncrementer
{
    // B  57 minutes ago  (HEAD -> release/1.0.0)
    // A  58 minutes ago <<--

    // A  58 minutes ago  (HEAD -> release/1.0.0) <<--

    // C  56 minutes ago  (HEAD -> release/1.0.0)
    // B  57 minutes ago <<--
    // A  58 minutes ago (main)

    public bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => !commit.HasChildIteration && !commit.GetEffectiveConfiguration(context.Configuration).IsMainBranch
            && commit.GetEffectiveConfiguration(context.Configuration).IsReleaseBranch
            && context.SemanticVersion is null && (commit.Predecessor is null
            || commit.Predecessor?.BranchName != commit.BranchName);

    public IEnumerable<IBaseVersionIncrement> GetIncrements(
        TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        var effectiveConfiguration = commit.GetEffectiveConfiguration(context.Configuration);
        if (commit.BranchName.TryGetSemanticVersion(out var element, effectiveConfiguration))
        {
            context.AlternativeSemanticVersions.Add(element.Value);
            yield break;
        }
    }
}
