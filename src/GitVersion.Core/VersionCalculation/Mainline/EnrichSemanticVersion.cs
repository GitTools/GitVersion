using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.Mainline;

internal sealed class EnrichSemanticVersion : IContextPreEnricher
{
    public void Enrich(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        var branchSpecificLabel = context.TargetLabel;
        branchSpecificLabel ??= iteration.GetEffectiveConfiguration(context.Configuration)
            .GetBranchSpecificLabel(commit.BranchName, null, context.Environment, context.CurrentCommit);
        branchSpecificLabel ??= commit.GetEffectiveConfiguration(context.Configuration)
            .GetBranchSpecificLabel(commit.BranchName, null, context.Environment, context.CurrentCommit);

        var semanticVersions = commit.SemanticVersions.Where(
            element => element.IsMatchForBranchSpecificLabel(branchSpecificLabel)
        ).ToList();
        foreach (var version in commit.SemanticVersions.Except(semanticVersions))
        {
            context.AddAlternativeSemanticVersion(version,
                new SemanticVersionSource("Git tag", version, commit.Value, VersionField.None));
        }
        context.SemanticVersion = semanticVersions.Max();
        context.SemVerSource = context.SemanticVersion is { } selected
            ? new SemanticVersionSource("Git tag", selected, commit.Value, VersionField.None)
            : null;
    }
}
