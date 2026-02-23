using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.Mainline;

internal sealed class EnrichSemanticVersion : IContextPreEnricher
{
    public void Enrich(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        var branchSpecificLabel = context.TargetLabel;
        branchSpecificLabel ??= iteration.GetEffectiveConfiguration(context.Configuration)
            .GetBranchSpecificLabel(commit.BranchName, null, context.Environment);
        branchSpecificLabel ??= commit.GetEffectiveConfiguration(context.Configuration)
            .GetBranchSpecificLabel(commit.BranchName, null, context.Environment);

        var semanticVersions = commit.SemanticVersions.Where(
            element => element.IsMatchForBranchSpecificLabel(branchSpecificLabel)
        ).ToList();
        context.AlternativeSemanticVersions.AddRange(commit.SemanticVersions.Except(semanticVersions));
        context.SemanticVersion = semanticVersions.Max();
    }
}
