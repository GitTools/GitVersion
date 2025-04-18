using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.Agents;

internal class BuildAgentResolver(IEnumerable<IBuildAgent> buildAgents, ILog log) : IBuildAgentResolver
{
    private readonly ILog log = log.NotNull();

    public ICurrentBuildAgent Resolve() => new Lazy<ICurrentBuildAgent>(ResolveInternal).Value;

    private ICurrentBuildAgent ResolveInternal()
    {
        var instance = (ICurrentBuildAgent)buildAgents.Single(x => x.IsDefault);

        foreach (var buildAgent in buildAgents.Where(x => !x.IsDefault))
        {
            try
            {
                if (!buildAgent.CanApplyToCurrentContext()) continue;
                instance = (ICurrentBuildAgent)buildAgent;
            }
            catch (Exception ex)
            {
                var agentName = buildAgent.GetType().Name;
                this.log.Warning($"Failed to check build agent '{agentName}': {ex.Message}");
            }
        }

        this.log.Info($"Applicable build agent found: '{instance.GetType().Name}'.");
        return instance;
    }
}
