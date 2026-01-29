using GitVersion.Extensions;

namespace GitVersion.Agents;

internal class BuildAgentResolver(IEnumerable<IBuildAgent> buildAgents, ILogger<BuildAgentResolver> logger) : IBuildAgentResolver
{
    private readonly IEnumerable<IBuildAgent> buildAgents = buildAgents.NotNull();
    private readonly ILogger<BuildAgentResolver> logger = logger.NotNull();

    public ICurrentBuildAgent Resolve() => new Lazy<ICurrentBuildAgent>(ResolveInternal).Value;

    private ICurrentBuildAgent ResolveInternal()
    {
        var instance = (ICurrentBuildAgent)this.buildAgents.Single(x => x.IsDefault);

        foreach (var buildAgent in this.buildAgents.Where(x => !x.IsDefault))
        {
            try
            {
                if (!buildAgent.CanApplyToCurrentContext()) continue;
                instance = (ICurrentBuildAgent)buildAgent;
            }
            catch (Exception ex)
            {
                var agentName = buildAgent.GetType().Name;
                this.logger.LogError(ex, "Failed to check build agent '{AgentName}': {Message}", agentName, ex.Message);
            }
        }

        this.logger.LogInformation("Applicable build agent found: '{AgentName}'.", instance.GetType().Name);
        return instance;
    }
}
