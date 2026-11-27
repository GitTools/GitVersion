using GitVersion.Configuration;

namespace GitVersion.VersionCalculation.Mainline.Trunk;

internal abstract class CommitOnTrunkBase(MainlineHistory history, IEnvironment environment) : IIncrementer
{
    // B  57 minutes ago (HEAD -> main) <<--
    // A  58 minutes ago

    // B  57 minutes ago (HEAD -> main)
    // A  58 minutes ago <<--

    public virtual bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => context.SemanticVersion is null && (!history.HasChildHistory(commit, context.TargetLabel, context.IncludeSharedHistory) || context.IsMainlineMerge)
            && context.EffectiveConfiguration.IsMainBranch;

    public IEnumerable<IBaseVersionIncrement> GetIncrements(
        MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        if (context.GetPredecessor(commit) is { } predecessor && predecessor.BranchName != commit.BranchName)
        {
            context.Label = null;
        }

        var effectiveConfiguration = context.EffectiveConfiguration;
        context.Label ??= effectiveConfiguration.GetBranchSpecificLabel(
            commit.BranchName, null, environment, context.CurrentCommit);
        context.ForceIncrement = true;

        yield return CreateIncrement(new BaseVersionOperator
        {
            Source = nameof(CommitOnTrunk),
            BaseVersionSource = context.BaseVersionSource,
            Increment = context.Increment,
            ForceIncrement = context.ForceIncrement,
            Label = context.Label,
            AlternativeSemanticVersion = context.AlternativeSemanticVersions.Max(),
            AlternativeSemVerSource = context.GetAlternativeSemVerSource()
        }, commit, context);

        context.BaseVersionSource = commit.Value;
    }

    protected virtual IBaseVersionIncrement CreateIncrement(
        BaseVersionOperator operation, MainlineCommit commit, MainlineContext context) => operation;
}
