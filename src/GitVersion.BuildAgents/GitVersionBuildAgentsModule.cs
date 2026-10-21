namespace GitVersion.Agents;

public class GitVersionBuildAgentsModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddSingleton<IBuildAgent, AppVeyor>();
        services.AddSingleton<IBuildAgent, AzurePipelines>();
        services.AddSingleton<IBuildAgent, BitBucketPipelines>();
        services.AddSingleton<IBuildAgent, BuildKite>();
        services.AddSingleton<IBuildAgent, CodeBuild>();
        services.AddSingleton<IBuildAgent, ContinuaCi>();
        services.AddSingleton<IBuildAgent, Drone>();
        services.AddSingleton<IBuildAgent, EnvRun>();
        services.AddSingleton<IBuildAgent, GitHubActions>();
        services.AddSingleton<IBuildAgent, GitLabCi>();
        services.AddSingleton<IBuildAgent, Jenkins>();
        services.AddSingleton<IBuildAgent, MyGet>();
        services.AddSingleton<IBuildAgent, SpaceAutomation>();
        services.AddSingleton<IBuildAgent, TeamCity>();
        services.AddSingleton<IBuildAgent, TravisCi>();
    }
}
