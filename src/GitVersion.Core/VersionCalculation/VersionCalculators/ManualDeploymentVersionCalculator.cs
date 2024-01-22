using GitVersion.Common;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

internal sealed class ManualDeploymentVersionCalculator(ILog log, IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
    : NonTrunkBasedVersionCalculatorBase(log, repositoryStore, versionContext), IVersionModeCalculator
{
    public SemanticVersion Calculate(NextVersion nextVersion)
    {
        using (this.log.IndentLog("Using manual deployment workflow to calculate the incremented version."))
        {
            return CalculateInternal(nextVersion);
        }
    }

    private SemanticVersion CalculateInternal(NextVersion nextVersion)
    {
        if (ShouldTakeIncrementedVersion(nextVersion))
        {
            return CalculateIncrementedVersion(nextVersion);
        }

        return new(nextVersion.BaseVersion.GetSemanticVersion())
        {
            BuildMetaData = CreateVersionBuildMetaData(nextVersion.BaseVersion.BaseVersionSource)
        };
    }
}
