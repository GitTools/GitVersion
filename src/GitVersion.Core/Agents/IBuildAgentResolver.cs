namespace GitVersion.Agents;

public interface IBuildAgentResolver
{
    ICurrentBuildAgent Resolve();
}
