using GitVersion.Configuration;

namespace GitVersion.VersionCalculation.Mainline.Trunk;

internal sealed class CommitOnTrunk : IIncrementer
{
    // B  57 minutes ago (HEAD -> main) <<--
    // A  58 minutes ago

    // B  57 minutes ago (HEAD -> main)
    // A  58 minutes ago <<--

    public bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => context.SemanticVersion is null && (!context.HasChildHistory(commit) || context.IsMergedMainline(commit))
            && context.GetEffectiveConfiguration(commit).IsMainBranch;

    public IEnumerable<IBaseVersionIncrement> GetIncrements(
        MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        if (context.GetPredecessor(commit) is { } predecessor && predecessor.BranchName != commit.BranchName)
        {
            context.Label = null;
        }

        var effectiveConfiguration = context.GetEffectiveConfiguration(commit);
        context.Label ??= effectiveConfiguration.GetBranchSpecificLabel(commit.BranchName, null, context.Environment, context.CurrentCommit);
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

        context.BaseVersionSource = commit.Value;
    }
}
