namespace GitVersion.VersionCalculation
{
    using System.Collections.Generic;
    using BaseVersionCalculators;

    public abstract class BaseVersionStrategy
    {
        public abstract IEnumerable<BaseVersion> GetVersions(GitVersionContext context);
    }
}