using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.BuildAgents;

public class BuildAgentResolver : IBuildAgentResolver
{
    private readonly IEnumerable<IBuildAgent> buildAgents;
    private readonly ILog log;
    public BuildAgentResolver(IEnumerable<IBuildAgent> buildAgents, ILog log)
    {
        this.log = log.NotNull();
        this.buildAgents = buildAgents;
    }

    public ICurrentBuildAgent Resolve() => new Lazy<ICurrentBuildAgent>(ResolveInternal).Value;

    private ICurrentBuildAgent ResolveInternal()
    {
        ICurrentBuildAgent instance = (ICurrentBuildAgent)this.buildAgents.Single(x => x is LocalBuild);

        foreach (var buildAgent in this.buildAgents.Where(x => x is not LocalBuild))
        {
            var agentName = buildAgent.GetType().Name;
            try
            {
                if (!buildAgent.CanApplyToCurrentContext()) continue;
                instance = (ICurrentBuildAgent)buildAgent;
            }
            catch (Exception ex)
            {
                this.log.Warning($"Failed to check build agent '{agentName}': {ex.Message}");
            }
        }

        this.log.Info($"Applicable build agent found: '{instance.GetType().Name}'.");
        return instance;
    }
}
