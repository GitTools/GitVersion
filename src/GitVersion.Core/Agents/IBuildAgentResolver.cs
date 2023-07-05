namespace GitVersion.Agents;

internal interface IBuildAgentResolver
{
    ICurrentBuildAgent Resolve();
}
