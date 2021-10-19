using GitVersion.Logging;

namespace GitVersion.BuildAgents;

public class BuildAgentResolver : IBuildAgentResolver
{
    private readonly IEnumerable<IBuildAgent> buildAgents;
    private readonly ILog log;
    public BuildAgentResolver(IEnumerable<IBuildAgent> buildAgents, ILog log)
    {
        this.log = log;
        this.buildAgents = buildAgents ?? Array.Empty<IBuildAgent>();
    }

    public ICurrentBuildAgent? Resolve()
    {
        ICurrentBuildAgent? instance = null;
        foreach (var buildAgent in this.buildAgents)
        {
            var agentName = buildAgent.GetType().Name;
            try
            {
                if (!buildAgent.CanApplyToCurrentContext()) continue;

                this.log.Info($"Applicable build agent found: '{agentName}'.");
                instance = (ICurrentBuildAgent)buildAgent;
            }
            catch (Exception ex)
            {
                this.log.Warning($"Failed to check build agent '{agentName}': {ex.Message}");
            }
        }

        return instance;
    }
}
