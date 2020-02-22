namespace GitVersion.VersionCalculation
{
    public interface IMainlineVersionCalculator
    {
        SemanticVersion FindMainlineModeVersion(BaseVersion baseVersion, GitVersionContext context);
    }
}
