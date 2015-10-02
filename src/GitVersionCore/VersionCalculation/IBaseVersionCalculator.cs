namespace GitVersion.VersionCalculation
{
    using GitVersion.VersionCalculation.BaseVersionCalculators;

    public interface IBaseVersionCalculator
    {
        BaseVersion GetBaseVersion(GitVersionContext context);
    }
}