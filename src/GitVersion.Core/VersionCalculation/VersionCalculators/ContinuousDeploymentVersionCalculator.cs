using System.Diagnostics.Contracts;
using GitVersion.Common;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

internal sealed class ContinuousDeploymentVersionCalculator(ILog log, IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
    : VersionCalculatorBase(log, repositoryStore, versionContext), IDeploymentModeCalculator
{
    public SemanticVersion Calculate(SemanticVersion semanticVersion, ICommit? baseVersionSource)
    {
        using (this.log.IndentLog("Using continuous deployment workflow to calculate the incremented version."))
        {
            return CalculateInternal(semanticVersion, baseVersionSource);
        }
    }

    private SemanticVersion CalculateInternal(SemanticVersion semanticVersion, ICommit? baseVersionSource)
    {
        var buildMetaData = CreateVersionBuildMetaData(baseVersionSource);
        Contract.Assume(buildMetaData.CommitsSinceTag.HasValue);

        return new SemanticVersion(semanticVersion)
        {
            PreReleaseTag = SemanticVersionPreReleaseTag.Empty,
            BuildMetaData = new SemanticVersionBuildMetaData(buildMetaData)
            {
                CommitsSinceVersionSource = buildMetaData.CommitsSinceTag.Value,
                CommitsSinceTag = null
            }
        };
    }
}
