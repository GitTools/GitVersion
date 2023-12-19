using System.Diagnostics.Contracts;
using GitVersion.Common;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

internal sealed class ContinuousDeliveryVersionCalculator : NonTrunkBasedVersionCalculatorBase, IVersionModeCalculator
{
    public ContinuousDeliveryVersionCalculator(ILog log, IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
        : base(log, repositoryStore, versionContext)
    {
    }

    public SemanticVersion Calculate(NextVersion nextVersion)
    {
        using (this.log.IndentLog("Using continuous delivery workflow to calculate the incremented version."))
        {
            var preReleaseTag = nextVersion.IncrementedVersion.PreReleaseTag;
            if (!preReleaseTag.HasTag() || !preReleaseTag.Number.HasValue)
            {
                throw new WarningException("Continuous delivery requires a pre-release tag.");
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
                PreReleaseTag = new SemanticVersionPreReleaseTag(semanticVersion.PreReleaseTag)
                {
                    Number = semanticVersion.PreReleaseTag.Number.Value + semanticVersion.BuildMetaData.CommitsSinceTag - 1
                },
                BuildMetaData = new SemanticVersionBuildMetaData(semanticVersion.BuildMetaData)
                {
                    CommitsSinceVersionSource = semanticVersion.BuildMetaData.CommitsSinceTag.Value,
                    CommitsSinceTag = null
                }
            };
        }

        var baseVersionBuildMetaData = CreateVersionBuildMetaData(nextVersion.BaseVersion.BaseVersionSource);
        return new SemanticVersion(nextVersion.BaseVersion.GetSemanticVersion())
        {
            BuildMetaData = baseVersionBuildMetaData
        };
    }
}
