using GitVersion.Common;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

internal sealed class ManualDeploymentVersionCalculator(
        ILogger<ManualDeploymentVersionCalculator> logger, IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
    : VersionCalculatorBase(logger, repositoryStore, versionContext), IDeploymentModeCalculator
{
    public SemanticVersion Calculate(SemanticVersion semanticVersion, IBaseVersion baseVersion)
    {
        using (this.logger.StartIndentedScope("Using manual deployment workflow to calculate the incremented version."))
        {
            return CalculateInternal(semanticVersion, baseVersion);
        }
    }

    private SemanticVersion CalculateInternal(SemanticVersion semanticVersion, IBaseVersion baseVersion)
    {
        var buildMetaData = CreateVersionBuildMetaData(baseVersion);

        return new SemanticVersion(semanticVersion)
        {
            BuildMetaData = buildMetaData
        };
    }
}
