using Common.Utilities;

namespace Release.Utilities;

public class Credentials
{
    public GitHubCredentials? GitHub { get; private init; }
    public static Credentials GetCredentials(ICakeContext context) => new()
    {
        GitHub = new(context.EnvironmentVariable("GITHUB_TOKEN")),
    };
}
