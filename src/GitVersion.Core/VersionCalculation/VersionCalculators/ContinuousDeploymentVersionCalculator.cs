using System.Diagnostics.Contracts;
using GitVersion.Common;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

internal sealed class ContinuousDeploymentVersionCalculator(ILog log, IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
    : VersionCalculatorBase(log, repositoryStore, versionContext), IVersionModeCalculator
{
    public SemanticVersion Calculate(NextVersion nextVersion)
    {
        using (this.log.IndentLog("Using continuous deployment workflow to calculate the incremented version."))
        {
            return CalculateInternal(nextVersion);
        }
    }

    private SemanticVersion CalculateInternal(NextVersion nextVersion)
    {
        if (ShouldTakeIncrementedVersion(nextVersion))
        {
            var semanticVersion = CalculateIncrementedVersion(nextVersion);

            Contract.Assume(semanticVersion.PreReleaseTag.Number.HasValue);
            Contract.Assume(semanticVersion.BuildMetaData.CommitsSinceTag.HasValue);

            return new(semanticVersion)
            {
                PreReleaseTag = SemanticVersionPreReleaseTag.Empty,
                BuildMetaData = new(semanticVersion.BuildMetaData)
                {
                    CommitsSinceVersionSource = semanticVersion.BuildMetaData.CommitsSinceTag.Value,
                    CommitsSinceTag = null
                }
            };
        }

        var baseVersionBuildMetaData = CreateVersionBuildMetaData(nextVersion.BaseVersion.BaseVersionSource);

        Contract.Assume(baseVersionBuildMetaData.CommitsSinceTag.HasValue);

        return new(nextVersion.BaseVersion.GetSemanticVersion())
        {
            PreReleaseTag = SemanticVersionPreReleaseTag.Empty,
            BuildMetaData = new(baseVersionBuildMetaData)
            {
                CommitsSinceVersionSource = baseVersionBuildMetaData.CommitsSinceTag.Value,
                CommitsSinceTag = null
            }
        };
    }
}
