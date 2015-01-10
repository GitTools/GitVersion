namespace GitVersion.VersionCalculation
{
    using System;

    public interface IMetaDataCalculator
    {
        SemanticVersionBuildMetaData Create(DateTimeOffset? baseVersionWhenFrom, GitVersionContext context);
    }
}