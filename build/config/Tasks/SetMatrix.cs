using Cake.Json;

namespace Config.Tasks;

public class SetMatrix : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (context.BuildSystem().IsRunningOnGitHubActions)
        {
            context.GitHubActions().Commands.SetOutputParameter("docker_distros", context.SerializeJson(Constants.DockerDistros));
            context.GitHubActions().Commands.SetOutputParameter("dotnet_versions", context.SerializeJson(Constants.DotnetVersions));
        }
        else
        {
            context.Information("Docker Distros: {0}", context.SerializeJson(Constants.DockerDistros));
            context.Information("Dotnet Versions: {0}", context.SerializeJson(Constants.DotnetVersions));
        }
    }
}
