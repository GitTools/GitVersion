using GitVersion.Common;
using GitVersion.Git;
using Microsoft.Extensions.Logging;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

internal sealed class ContinuousDeploymentVersionCalculator(
        ILogger<ContinuousDeploymentVersionCalculator> logger, IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
    : VersionCalculatorBase<ContinuousDeploymentVersionCalculator>(logger, repositoryStore, versionContext), IDeploymentModeCalculator
{
    public SemanticVersion Calculate(SemanticVersion semanticVersion, ICommit? baseVersionSource)
    {
        using (this.logger.IndentLog("Using continuous deployment workflow to calculate the incremented version."))
        {
            return CalculateInternal(semanticVersion, baseVersionSource);
        }
    }

    private SemanticVersion CalculateInternal(SemanticVersion semanticVersion, ICommit? baseVersionSource)
    {
        var buildMetaData = CreateVersionBuildMetaData(baseVersionSource);

        return new SemanticVersion(semanticVersion)
        {
            PreReleaseTag = SemanticVersionPreReleaseTag.Empty,
            BuildMetaData = new SemanticVersionBuildMetaData(buildMetaData)
            {
                CommitsSinceVersionSource = buildMetaData.CommitsSinceTag!.Value,
                CommitsSinceTag = null
            }
        };
    }
}
