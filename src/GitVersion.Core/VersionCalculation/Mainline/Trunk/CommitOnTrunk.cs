using GitVersion.Configuration;

namespace GitVersion.VersionCalculation.Mainline.Trunk;

internal sealed class CommitOnTrunk : IIncrementer
{
    // B  57 minutes ago (HEAD -> main) <<--
    // A  58 minutes ago

    // B  57 minutes ago (HEAD -> main)
    // A  58 minutes ago <<--

    public bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => !commit.HasChildIteration
            && commit.GetEffectiveConfiguration(context.Configuration).IsMainBranch && context.SemanticVersion is null;

    public IEnumerable<IBaseVersionIncrement> GetIncrements(
        MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        if (commit.Predecessor is not null && commit.Predecessor.BranchName != commit.BranchName)
            context.Label = null;

        var effectiveConfiguration = commit.GetEffectiveConfiguration(context.Configuration);
        context.Label ??= effectiveConfiguration.GetBranchSpecificLabel(commit.BranchName, null);
        context.ForceIncrement = true;

        yield return new BaseVersionOperator
        {
            Source = GetType().Name,
            BaseVersionSource = context.BaseVersionSource,
            Increment = context.Increment,
            ForceIncrement = context.ForceIncrement,
            Label = context.Label,
            AlternativeSemanticVersion = context.AlternativeSemanticVersions.Max()
        };

        context.BaseVersionSource = commit.Value;
    }
}
