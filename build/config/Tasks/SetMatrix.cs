using Cake.Json;

namespace Config.Tasks;

public class SetMatrix : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (context.BuildSystem().IsRunningOnGitHubActions)
        {
            context.GitHubActions().Commands.SetOutputParameter("dockerDistros", context.SerializeJson(Constants.DockerDistros));
            context.GitHubActions().Commands.SetOutputParameter("dotnetVersions", context.SerializeJson(Constants.DotnetVersions));
        }
        else
        {
            context.Information("Docker Distros: {0}", context.SerializeJson(Constants.DockerDistros));
            context.Information("Dotnet Versions: {0}", context.SerializeJson(Constants.DotnetVersions));
        }
    }
}
