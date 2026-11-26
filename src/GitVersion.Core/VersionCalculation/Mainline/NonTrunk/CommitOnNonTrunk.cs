using GitVersion.Configuration;

namespace GitVersion.VersionCalculation.Mainline.NonTrunk;

internal sealed class CommitOnNonTrunk(MainlineHistory history, IEnvironment environment) : IIncrementer
{
    // B  57 minutes ago  (HEAD -> feature/foo)
    // A  58 minutes ago <<--

    // B  57 minutes ago  (HEAD -> feature/foo) <<--
    // A  58 minutes ago

    public bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => context.SemanticVersion is null && !history.HasChildHistory(commit, context.TargetLabel, context.IncludeSharedHistory)
           && !context.EffectiveConfiguration.IsMainBranch;

    public IEnumerable<IBaseVersionIncrement> GetIncrements(
        MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        if (context.GetPredecessor(commit) is { } predecessor && predecessor.BranchName != commit.BranchName)
        {
            context.Label = null;
        }

        var effectiveConfiguration = context.EffectiveConfiguration;
        context.Label ??= effectiveConfiguration.GetBranchSpecificLabel(commit.BranchName, null, environment, context.CurrentCommit);

        if (!context.IsLastCommit(commit))
        {
            yield break;
        }

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
        context.ForceIncrement = false;
    }
}
