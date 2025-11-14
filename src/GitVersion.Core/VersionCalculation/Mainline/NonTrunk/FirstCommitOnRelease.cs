using GitVersion.Configuration;

namespace GitVersion.VersionCalculation.Mainline.NonTrunk;

/// <summary>
/// This incrementer identifies the first commit on a branch marked with IsReleaseBranch true and appends the version number for
/// instance 1.0.0 (extracted from the branch name) as an alternative semantic version to the context. This information will be
/// used later to bump the version number to a higher value if necessary.
/// </summary>
internal sealed class FirstCommitOnRelease : IIncrementer
{
    // B  57 minutes ago  (HEAD -> release/1.0.0)
    // A  58 minutes ago <<--

    // A  58 minutes ago  (HEAD -> release/1.0.0) <<--

    // C  56 minutes ago  (HEAD -> release/1.0.0)
    // B  57 minutes ago <<--
    // A  58 minutes ago (main)

    public bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => !commit.HasChildIteration
           && !commit.GetEffectiveConfiguration(context.Configuration).IsMainBranch
           && commit.GetEffectiveConfiguration(context.Configuration).IsReleaseBranch
           && context.SemanticVersion is null
           && (commit.Predecessor is null
               || commit.BranchName != commit.Predecessor?.BranchName);

    public IEnumerable<IBaseVersionIncrement> GetIncrements(
        MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        var effectiveConfiguration = commit.GetEffectiveConfiguration(context.Configuration);
        if (!commit.BranchName.TryGetSemanticVersion(effectiveConfiguration, out var element)) yield break;
        context.AlternativeSemanticVersions.Add(element.Value);
    }
}
