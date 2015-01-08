namespace GitVersion.VersionStrategies
{
    public abstract class VersionStrategyBase
    {
        public abstract SemanticVersion CalculateVersion(GitVersionContext context);
    }
}
