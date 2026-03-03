using GitVersion.Common;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

internal sealed class ContinuousDeliveryVersionCalculator(
        ILogger<ContinuousDeliveryVersionCalculator> logger, IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
    : VersionCalculatorBase(logger, repositoryStore, versionContext), IDeploymentModeCalculator
{
    public SemanticVersion Calculate(SemanticVersion semanticVersion, IBaseVersion baseVersion)
    {
        using (this.logger.StartIndentedScope("Using continuous delivery workflow to calculate the incremented version."))
        {
            var preReleaseTag = semanticVersion.PreReleaseTag;
            if (!preReleaseTag.HasTag() || !preReleaseTag.Number.HasValue)
            {
                throw new WarningException("Continuous delivery requires a pre-release tag.");
            }

            return CalculateInternal(semanticVersion, baseVersion);
        }
    }

    private SemanticVersion CalculateInternal(SemanticVersion semanticVersion, IBaseVersion baseVersion)
    {
        var buildMetaData = CreateVersionBuildMetaData(baseVersion);

        return new SemanticVersion(semanticVersion)
        {
            PreReleaseTag = new SemanticVersionPreReleaseTag(semanticVersion.PreReleaseTag)
            {
                Number = semanticVersion.PreReleaseTag.Number!.Value + buildMetaData.CommitsSinceTag - 1
            },
            BuildMetaData = new SemanticVersionBuildMetaData(buildMetaData)
            {
                VersionSourceDistance = buildMetaData.CommitsSinceTag!.Value,
                CommitsSinceTag = null
            }
        };
    }
}
