using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal class AppVeyor(IEnvironment environment, ILogger<AppVeyor> logger, IFileSystem fileSystem) : BuildAgentBase(environment, logger, fileSystem)
{
    public const string EnvironmentVariableName = "APPVEYOR";

    protected override string EnvironmentVariable => EnvironmentVariableName;

    public override string SetBuildNumber(GitVersionVariables variables)
    {
        var buildNumber = Environment.GetEnvironmentVariable("APPVEYOR_BUILD_NUMBER");
        var apiUrl = Environment.GetEnvironmentVariable("APPVEYOR_API_URL") ?? throw new Exception("APPVEYOR_API_URL environment variable not set");

        using var httpClient = GetHttpClient(apiUrl);

        var body = new
        {
            version = $"{variables.FullSemVer}.build.{buildNumber}"
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

    public override string[] SetOutputVariables(string name, string? value)
    {
        var apiUrl = Environment.GetEnvironmentVariable("APPVEYOR_API_URL") ?? throw new Exception("APPVEYOR_API_URL environment variable not set");
        var httpClient = GetHttpClient(apiUrl);

        var body = new
        {
            name = $"GitVersion_{name}",
            value = $"{value}"
        };

        var stringContent = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = httpClient.PostAsync("api/build/variables", stringContent).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        return
        [
            $"Adding Environment Variable. name='GitVersion_{name}' value='{value}']"
        ];
    }

    private static HttpClient GetHttpClient(string apiUrl) => new()
    {
        BaseAddress = new Uri(apiUrl)
    };

    public override string? GetCurrentBranch(bool usingDynamicRepos)
    {
        var pullRequestBranchName = Environment.GetEnvironmentVariable("APPVEYOR_PULL_REQUEST_HEAD_REPO_BRANCH");
        return !pullRequestBranchName.IsNullOrWhiteSpace()
            ? pullRequestBranchName
            : this.Environment.GetEnvironmentVariable("APPVEYOR_REPO_BRANCH");
    }

    public override bool PreventFetch() => false;
}
