public class BuildCredentials
{
    public GitHubCredentials GitHub { get; private set; }
    public GitterCredentials Gitter { get; private set; }
    public DockerHubCredentials Docker { get; private set; }
    public NugetCredentials Nuget { get; private set; }
    public ChocolateyCredentials Chocolatey { get; private set; }
    public CodeCovCredentials CodeCov { get; private set; }

    public static BuildCredentials GetCredentials(ICakeContext context)
    {
        return new BuildCredentials
        {
            GitHub     = GitHubCredentials.GetGitHubCredentials(context),
            Gitter     = GitterCredentials.GetGitterCredentials(context),
            Docker     = DockerHubCredentials.GetDockerHubCredentials(context),
            Nuget      = NugetCredentials.GetNugetCredentials(context),
            Chocolatey = ChocolateyCredentials.GetChocolateyCredentials(context),
            CodeCov    = CodeCovCredentials.GetCodeCovCredentials(context),
        };
    }
}

public class GitHubCredentials
{
    public string UserName { get; private set; }
    public string Token { get; private set; }

    public GitHubCredentials(string userName, string token)
    {
        UserName = userName;
        Token = token;
    }

    public static GitHubCredentials GetGitHubCredentials(ICakeContext context)
    {
        return new GitHubCredentials(
            context.EnvironmentVariable("GITHUB_USERNAME"),
            context.EnvironmentVariable("GITHUB_TOKEN"));
    }
}

public class GitterCredentials
{
    public string Token { get; private set; }
    public string RoomId { get; private set; }

    public GitterCredentials(string token, string roomId)
    {
        Token = token;
        RoomId = roomId;
    }

    public static GitterCredentials GetGitterCredentials(ICakeContext context)
    {
        return new GitterCredentials(
            context.EnvironmentVariable("GITTER_TOKEN"),
            context.EnvironmentVariable("GITTER_ROOM_ID")
        );
    }
}

public class DockerHubCredentials
{
    public string UserName { get; private set; }
    public string Password { get; private set; }

    public DockerHubCredentials(string userName, string password)
    {
        UserName = userName;
        Password = password;
    }

    public static DockerHubCredentials GetDockerHubCredentials(ICakeContext context)
    {
        return new DockerHubCredentials(
            context.EnvironmentVariable("DOCKER_USERNAME"),
            context.EnvironmentVariable("DOCKER_PASSWORD"));
    }
}

public class NugetCredentials
{
    public string ApiKey { get; private set; }
    public string ApiUrl { get; private set; }

    public NugetCredentials(string apiKey, string apiUrl)
    {
        ApiKey = apiKey;
        ApiUrl = apiUrl;
    }

    public static NugetCredentials GetNugetCredentials(ICakeContext context)
    {
        return new NugetCredentials(
            context.EnvironmentVariable("NUGET_API_KEY"),
            context.EnvironmentVariable("NUGET_API_URL"));
    }
}

public class ChocolateyCredentials
{
    public string ApiKey { get; private set; }
    public string ApiUrl { get; private set; }

    public ChocolateyCredentials(string apiKey, string apiUrl)
    {
        ApiKey = apiKey;
        ApiUrl = apiUrl;
    }

    public static ChocolateyCredentials GetChocolateyCredentials(ICakeContext context)
    {
        return new ChocolateyCredentials(
            context.EnvironmentVariable("CHOCOLATEY_API_KEY"),
            context.EnvironmentVariable("CHOCOLATEY_API_URL"));
    }
}

public class CodeCovCredentials
{
    public string Token { get; private set; }

    public CodeCovCredentials(string token)
    {
        Token = token;
    }

    public static CodeCovCredentials GetCodeCovCredentials(ICakeContext context)
    {
        return new CodeCovCredentials(context.EnvironmentVariable("CODECOV_TOKEN"));
    }
}
