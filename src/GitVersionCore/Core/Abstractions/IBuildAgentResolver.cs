namespace GitVersion
{
    public interface IBuildAgentResolver
    {
        ICurrentBuildAgent Resolve();
    }
}
