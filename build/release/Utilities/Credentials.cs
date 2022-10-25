using Common.Utilities;

namespace Release.Utilities;

public class Credentials
{
    public GitHubCredentials? GitHub { get; private set; }
    public static Credentials GetCredentials(ICakeContext context) => new()
    {
        GitHub = new GitHubCredentials(context.EnvironmentVariable("GITHUB_TOKEN")),
    };
}
