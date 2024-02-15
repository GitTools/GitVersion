using System.Diagnostics.Contracts;
using GitVersion.Common;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

internal sealed class ContinuousDeliveryVersionCalculator(ILog log, IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
    : VersionCalculatorBase(log, repositoryStore, versionContext), IDeploymentModeCalculator
{
    public SemanticVersion Calculate(SemanticVersion semanticVersion, ICommit? baseVersionSource)
    {
        using (this.log.IndentLog("Using continuous delivery workflow to calculate the incremented version."))
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
        Contract.Assume(semanticVersion.PreReleaseTag.Number.HasValue);

        var buildMetaData = CreateVersionBuildMetaData(baseVersionSource);
        Contract.Assume(buildMetaData.CommitsSinceTag.HasValue);

        return new SemanticVersion(semanticVersion)
        {
            PreReleaseTag = new SemanticVersionPreReleaseTag(semanticVersion.PreReleaseTag)
            {
                Number = semanticVersion.PreReleaseTag.Number.Value + buildMetaData.CommitsSinceTag - 1
            },
            BuildMetaData = new SemanticVersionBuildMetaData(buildMetaData)
            {
                CommitsSinceVersionSource = buildMetaData.CommitsSinceTag.Value,
                CommitsSinceTag = null
            }
        };
    }
}
