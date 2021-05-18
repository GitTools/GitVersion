using Cake.Common;
using Cake.Core;

namespace Artifacts.Utilities
{
    public class BuildCredentials
    {
        public GitHubCredentials? GitHub { get; private set; }
        public GitterCredentials? Gitter { get; private set; }
        public DockerHubCredentials? Docker { get; private set; }
        public NugetCredentials? Nuget { get; private set; }
        public ChocolateyCredentials? Chocolatey { get; private set; }

        public static BuildCredentials GetCredentials(ICakeContext context)
        {
            return new()
            {
                GitHub = GitHubCredentials.GetGitHubCredentials(context),
                Gitter = GitterCredentials.GetGitterCredentials(context),
                Docker = DockerHubCredentials.GetDockerHubCredentials(context),
                Nuget = NugetCredentials.GetNugetCredentials(context),
                Chocolatey = ChocolateyCredentials.GetChocolateyCredentials(context),
            };
        }
    }

    public record GitHubCredentials(string UserName, string Token)
    {
        public static GitHubCredentials GetGitHubCredentials(ICakeContext context)
        {
            return new(
                context.EnvironmentVariable("GITHUB_USERNAME"),
                context.EnvironmentVariable("GITHUB_TOKEN"));
        }
    }

    public record GitterCredentials(string Token, string RoomId)
    {
        public static GitterCredentials GetGitterCredentials(ICakeContext context)
        {
            return new(
                context.EnvironmentVariable("GITTER_TOKEN"),
                context.EnvironmentVariable("GITTER_ROOM_ID")
            );
        }
    }

    public record DockerHubCredentials(string UserName, string Password)
    {
        public static DockerHubCredentials GetDockerHubCredentials(ICakeContext context)
        {
            return new(
                context.EnvironmentVariable("DOCKER_USERNAME"),
                context.EnvironmentVariable("DOCKER_PASSWORD"));
        }
    }

    public record NugetCredentials(string ApiKey, string ApiUrl)
    {
        public static NugetCredentials GetNugetCredentials(ICakeContext context)
        {
            return new(
                context.EnvironmentVariable("NUGET_API_KEY"),
                context.EnvironmentVariable("NUGET_API_URL"));
        }
    }

    public record ChocolateyCredentials(string ApiKey, string ApiUrl)
    {
        public static ChocolateyCredentials GetChocolateyCredentials(ICakeContext context)
        {
            return new(
                context.EnvironmentVariable("CHOCOLATEY_API_KEY"),
                context.EnvironmentVariable("CHOCOLATEY_API_URL"));
        }
    }
}
