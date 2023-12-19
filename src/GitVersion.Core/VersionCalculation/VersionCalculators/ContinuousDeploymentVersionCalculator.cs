using System.Diagnostics.Contracts;
using GitVersion.Common;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

internal sealed class ContinuousDeploymentVersionCalculator : NonTrunkBasedVersionCalculatorBase, IVersionModeCalculator
{
    public ContinuousDeploymentVersionCalculator(ILog log, IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
        : base(log, repositoryStore, versionContext)
    {
    }

    public SemanticVersion Calculate(NextVersion nextVersion)
    {
        using (this.log.IndentLog("Using continuous deployment workflow to calculate the incremented version."))
        {
            if (nextVersion.Configuration.Label is not null)
            {
                throw new WarningException("Continuous deployment requires no pre-release tag.");
            }
            if (!nextVersion.Configuration.IsMainline)
            {
                throw new WarningException("Continuous deployment is only supported for mainline branches.");
            }

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

            return new SemanticVersion(semanticVersion)
            {
                PreReleaseTag = SemanticVersionPreReleaseTag.Empty,
                BuildMetaData = new SemanticVersionBuildMetaData(semanticVersion.BuildMetaData)
                {
                    CommitsSinceVersionSource = semanticVersion.BuildMetaData.CommitsSinceTag.Value,
                    CommitsSinceTag = null
                }
            };
        }

        var baseVersionBuildMetaData = CreateVersionBuildMetaData(nextVersion.BaseVersion.BaseVersionSource);

        Contract.Assume(baseVersionBuildMetaData.CommitsSinceTag.HasValue);

        return new SemanticVersion(nextVersion.BaseVersion.GetSemanticVersion())
        {
            PreReleaseTag = SemanticVersionPreReleaseTag.Empty,
            BuildMetaData = new SemanticVersionBuildMetaData(baseVersionBuildMetaData)
            {
                CommitsSinceVersionSource = baseVersionBuildMetaData.CommitsSinceTag.Value,
                CommitsSinceTag = null
            }
        };
    }
}
