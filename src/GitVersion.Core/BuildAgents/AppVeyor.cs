using System.Text.Json;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.BuildAgents;

public class AppVeyor : BuildAgentBase
{
    public AppVeyor(IEnvironment environment, ILog log) : base(environment, log)
    {
    }

    public const string EnvironmentVariableName = "APPVEYOR";

    protected override string EnvironmentVariable { get; } = EnvironmentVariableName;

    public override string GenerateSetVersionMessage(VersionVariables variables)
    {
        var buildNumber = Environment.GetEnvironmentVariable("APPVEYOR_BUILD_NUMBER");

        using var httpClient = GetHttpClient();

        var body = new
        {
            version = $"{variables.FullSemVer}.build.{buildNumber}",
        };

        var stringContent = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        try
        {
            var response = httpClient.PutAsync("api/build", stringContent).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            return $"Failed to set AppVeyor build number to '{variables.FullSemVer}'. The error was: {ex.Message}";
        }

        return $"Set AppVeyor build number to '{variables.FullSemVer}'.";
    }

    public override string[] GenerateSetParameterMessage(string name, string value)
    {
        var httpClient = GetHttpClient();

        var body = new
        {
            name = $"GitVersion_{name}",
            value = $"{value}"
        };

        var stringContent = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = httpClient.PostAsync("api/build/variables", stringContent).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        return new[]
        {
            $"Adding Environment Variable. name='GitVersion_{name}' value='{value}']"
        };
    }

    private HttpClient GetHttpClient()
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(Environment.GetEnvironmentVariable("APPVEYOR_API_URL"))
        };

        return httpClient;
    }


    public override string? GetCurrentBranch(bool usingDynamicRepos)
    {
        var pullRequestBranchName = Environment.GetEnvironmentVariable("APPVEYOR_PULL_REQUEST_HEAD_REPO_BRANCH");
        if (!pullRequestBranchName.IsNullOrWhiteSpace())
        {
            return pullRequestBranchName;
        }
        return Environment.GetEnvironmentVariable("APPVEYOR_REPO_BRANCH");
    }

    public override bool PreventFetch() => false;
}
