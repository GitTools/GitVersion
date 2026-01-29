using GitVersion.Common;
using GitVersion.Git;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

internal sealed class ManualDeploymentVersionCalculator(
        ILogger<ManualDeploymentVersionCalculator> logger, IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
    : VersionCalculatorBase(logger, repositoryStore, versionContext), IDeploymentModeCalculator
{
    public SemanticVersion Calculate(SemanticVersion semanticVersion, ICommit? baseVersionSource)
    {
        using (this.logger.StartIndentedScope("Using manual deployment workflow to calculate the incremented version."))
        {
            return CalculateInternal(semanticVersion, baseVersionSource);
        }
    }

    private SemanticVersion CalculateInternal(SemanticVersion semanticVersion, ICommit? baseVersionSource)
    {
        var buildMetaData = CreateVersionBuildMetaData(baseVersionSource);

        return new SemanticVersion(semanticVersion)
        {
            BuildMetaData = buildMetaData
        };
    }
}
