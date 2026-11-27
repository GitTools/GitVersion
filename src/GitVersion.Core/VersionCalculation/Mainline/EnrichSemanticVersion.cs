using GitVersion.Configuration;

namespace GitVersion.VersionCalculation.Mainline;

internal sealed class EnrichSemanticVersion(IEnvironment environment) : IContextPreEnricher
{
    public void Enrich(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        var branchSpecificLabel = context.TargetLabel;
        branchSpecificLabel ??= context.IterationConfiguration
            .GetBranchSpecificLabel(commit.BranchName, null, environment, context.CurrentCommit);
        branchSpecificLabel ??= context.EffectiveConfiguration
            .GetBranchSpecificLabel(commit.BranchName, null, environment, context.CurrentCommit);

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
