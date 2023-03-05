using Common.Utilities;

namespace Publish.Utilities;

public class Credentials
{
    public GitHubCredentials? GitHub { get; private init; }
    public NugetCredentials? Nuget { get; private init; }
    public ChocolateyCredentials? Chocolatey { get; private init; }

    public static Credentials GetCredentials(ICakeContext context) => new()
    {
        GitHub = new GitHubCredentials(context.EnvironmentVariable("GITHUB_TOKEN")),
        Nuget = new NugetCredentials(context.EnvironmentVariable("NUGET_API_KEY")),
        Chocolatey = new ChocolateyCredentials(context.EnvironmentVariable("CHOCOLATEY_API_KEY")),
    };
}
