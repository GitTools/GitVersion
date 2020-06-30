namespace GitVersion.VersionCalculation
{
    public interface INextVersionCalculator
    {
        SemanticVersion FindVersion();
    }
}
