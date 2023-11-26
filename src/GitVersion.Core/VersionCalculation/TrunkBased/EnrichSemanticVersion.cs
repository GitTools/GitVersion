using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.TrunkBased;

internal sealed class EnrichSemanticVersion : ITrunkBasedContextPreEnricher
{
    public void Enrich(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        var branchSpecificLabel = context.TargetLabel;
        branchSpecificLabel ??= iteration.Configuration.GetBranchSpecificLabel(commit.BranchName, null);
        branchSpecificLabel ??= commit.Configuration.GetBranchSpecificLabel(commit.BranchName, null);

        var semanticVersions = commit.SemanticVersions.Where(
            element => element.IsMatchForBranchSpecificLabel(branchSpecificLabel)
        ).ToList();
        context.AlternativeSemanticVersions.AddRange(commit.SemanticVersions.Except(semanticVersions));
        context.SemanticVersion = semanticVersions.Max();
    }
}
