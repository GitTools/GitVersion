using Cake.Common;
using Cake.Core;
using Common.Utilities;

namespace Docker.Utilities
{
    public class BuildCredentials
    {
        public GitHubCredentials? GitHub { get; private set; }
        public DockerHubCredentials? Docker { get; private set; }
        public static BuildCredentials GetCredentials(ICakeContext context) => new()
        {
            GitHub = new GitHubCredentials(
                context.EnvironmentVariable("GITHUB_USERNAME"),
                context.EnvironmentVariable("GITHUB_TOKEN")),

            Docker = new DockerHubCredentials(
                context.EnvironmentVariable("DOCKER_USERNAME"),
                context.EnvironmentVariable("DOCKER_PASSWORD")),
        };
    }
}
