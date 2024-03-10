using Common.Utilities;

namespace Docker.Utilities;

public class Credentials
{
    public DockerHubCredentials? DockerHub { get; private init; }

    public static Credentials GetCredentials(ICakeContext context) => new()
    {
        DockerHub = new(
            context.EnvironmentVariable("DOCKER_USERNAME"),
            context.EnvironmentVariable("DOCKER_PASSWORD")),
    };
}
