using System.Diagnostics.Contracts;
using GitVersion.Common;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

internal sealed class ContinuousDeploymentVersionCalculator : NonTrunkBasedVersionCalculatorBase, IContinuousDeploymentVersionCalculator
{
    public ContinuousDeploymentVersionCalculator(ILog log, IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
        : base(log, repositoryStore, versionContext)
    {
    }

    public SemanticVersion Calculate(NextVersion nextVersion)
    {
        using (this.log.IndentLog("Using continues deployment typology to calculate the incremented version!!"))
        {
            if (!nextVersion.Configuration.IsMainline || nextVersion.Configuration.Label is not null)
                throw new WarningException("--PRE--CONDITION--FAILED--");

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
        else
        {
            var baseVersionBuildMetaData = CreateVersionBuildMetaData(nextVersion.BaseVersion.BaseVersionSource);

            Contract.Assume(baseVersionBuildMetaData.CommitsSinceTag.HasValue);

            return new SemanticVersion(nextVersion.BaseVersion.SemanticVersion)
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
}
