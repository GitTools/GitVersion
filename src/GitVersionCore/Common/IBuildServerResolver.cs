namespace GitVersion.Common
{
    public interface IBuildServerResolver
    {
        IBuildServer GetCurrentBuildServer();
    }
}
