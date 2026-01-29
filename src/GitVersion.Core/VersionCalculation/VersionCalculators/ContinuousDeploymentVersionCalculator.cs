using GitVersion.Common;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

internal sealed class ContinuousDeploymentVersionCalculator(
        ILogger<ContinuousDeploymentVersionCalculator> logger, IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
    : VersionCalculatorBase(logger, repositoryStore, versionContext), IDeploymentModeCalculator
{
    public SemanticVersion Calculate(SemanticVersion semanticVersion, IBaseVersion baseVersion)
    {
        using (this.logger.StartIndentedScope("Using continuous deployment workflow to calculate the incremented version."))
        {
            return CalculateInternal(semanticVersion, baseVersion);
        }
    }

    private SemanticVersion CalculateInternal(SemanticVersion semanticVersion, IBaseVersion baseVersion)
    {
        var buildMetaData = CreateVersionBuildMetaData(baseVersion);

        return new SemanticVersion(semanticVersion)
        {
            PreReleaseTag = SemanticVersionPreReleaseTag.Empty,
            BuildMetaData = new SemanticVersionBuildMetaData(buildMetaData)
            {
                VersionSourceDistance = buildMetaData.CommitsSinceTag!.Value,
                CommitsSinceTag = null
            }
        };
    }
}
