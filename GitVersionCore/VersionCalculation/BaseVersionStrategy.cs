namespace GitVersion.VersionCalculation
{
    using GitVersion.VersionCalculation.BaseVersionCalculators;

    public abstract class BaseVersionStrategy
    {
        public abstract BaseVersion GetVersion(GitVersionContext context);
    }
}