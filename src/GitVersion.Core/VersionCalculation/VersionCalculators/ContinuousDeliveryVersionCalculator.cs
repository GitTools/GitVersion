using GitVersion.Common;
using GitVersion.Git;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

internal sealed class ContinuousDeliveryVersionCalculator(
        ILogger<ContinuousDeliveryVersionCalculator> logger, IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
    : VersionCalculatorBase(logger, repositoryStore, versionContext), IDeploymentModeCalculator
{
    public SemanticVersion Calculate(SemanticVersion semanticVersion, ICommit? baseVersionSource)
    {
        using (this.logger.StartIndentedScope("Using continuous delivery workflow to calculate the incremented version."))
        {
            var preReleaseTag = semanticVersion.PreReleaseTag;
            if (!preReleaseTag.HasTag() || !preReleaseTag.Number.HasValue)
            {
                throw new WarningException("Continuous delivery requires a pre-release tag.");
            }

            return CalculateInternal(semanticVersion, baseVersionSource);
        }
    }

    private SemanticVersion CalculateInternal(SemanticVersion semanticVersion, ICommit? baseVersionSource)
    {
        var buildMetaData = CreateVersionBuildMetaData(baseVersionSource);

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
