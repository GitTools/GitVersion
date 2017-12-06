using GitVersion.VersionCalculation.BaseVersionCalculators;

namespace GitVersion.VersionCalculation
{
    public interface IMetaDataCalculator
    {
        SemanticVersionBuildMetaData Create(int commitCount, GitVersionContext context);
    }
}