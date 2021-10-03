namespace GitVersion.BuildAgents;

public interface IBuildAgentResolver
{
    ICurrentBuildAgent? Resolve();
}
