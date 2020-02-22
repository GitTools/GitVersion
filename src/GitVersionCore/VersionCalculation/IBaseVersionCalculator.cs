namespace GitVersion.VersionCalculation
{
    public interface IBaseVersionCalculator
    {
        BaseVersion GetBaseVersion(GitVersionContext context);
    }
}
