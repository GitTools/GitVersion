using System.Net.Http.Headers;
using System.Text.Json;
using Cake.Common.Tools.DotNet.NuGet.Push;
using Common.Utilities;

namespace Publish.Tasks;

[TaskName(nameof(PublishNuget))]
[TaskDescription("Publish nuget packages")]
[IsDependentOn(typeof(PublishNugetInternal))]
public class PublishNuget : FrostingTask<BuildContext>;

[TaskName(nameof(PublishNugetInternal))]
[TaskDescription("Publish nuget packages")]
public class PublishNugetInternal : AsyncFrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.IsGitHubActionsBuild, $"{nameof(PublishNuget)} works only on GitHub Actions.");
        shouldRun &= context.ShouldRun(context.IsStableRelease || context.IsTaggedPreRelease || context.IsInternalPreRelease, $"{nameof(PublishNuget)} works only for releases.");

        return shouldRun;
    }

    public override async Task RunAsync(BuildContext context)
    {
        // publish to github packages for commits on main and on original repo
        if (context.IsInternalPreRelease)
        {
            context.StartGroup("Publishing to GitHub Packages");
            var apiKey = context.Credentials?.GitHub?.Token;
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Could not resolve NuGet GitHub Packages API key.");
            }

            PublishToNugetRepo(context, apiKey, Constants.GithubPackagesUrl);
            context.EndGroup();
        }

        // publish to nuget.org for tagged releases
        if (context.IsStableRelease || context.IsTaggedPreRelease)
        {
            context.StartGroup("Publishing to Nuget.org");
            var apiKey = context.Credentials?.Nuget?.ApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Could not resolve NuGet org API key.");
            }

            PublishToNugetRepo(context, apiKey, Constants.NugetOrgUrl);
            context.EndGroup();
        }

        await Task.CompletedTask;
    }

    private static void PublishToNugetRepo(BuildContext context, string apiKey, string apiUrl)
    {
        ArgumentNullException.ThrowIfNull(context.Version);
        var nugetVersion = context.Version.NugetVersion;
        foreach (var (packageName, filePath, _) in context.Packages.Where(x => !x.IsChocoPackage))
        {
            context.Information($"Package {packageName}, version {nugetVersion} is being published.");
            context.DotNetNuGetPush(filePath.FullPath,
                new DotNetNuGetPushSettings
                {
                    ApiKey = apiKey,
                    Source = apiUrl,
                    SkipDuplicate = true
                });
        }
    }

    private static async Task<string?> GetNugetApiKey(BuildContext context)
    {
        try
        {
            var oidcToken = await GetGitHubOidcToken(context);
            var apiKey = await ExchangeOidcTokenForApiKey(oidcToken);

            context.Information($"Successfully exchanged OIDC token for NuGet API key.");
            return apiKey;
        }
        catch (HttpRequestException ex)
        {
            context.Error($"Network error while retrieving NuGet API key: {ex.Message}");
            return null;
        }
        catch (InvalidOperationException ex)
        {
            context.Error($"Invalid operation while retrieving NuGet API key: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            context.Error($"JSON parsing error while retrieving NuGet API key: {ex.Message}");
            return null;
        }
    }

    private static async Task<string> GetGitHubOidcToken(BuildContext context)
    {
        const string nugetAudience = "https://www.nuget.org";

        var oidcRequestToken = context.Environment.GetEnvironmentVariable("ACTIONS_ID_TOKEN_REQUEST_TOKEN");
        var oidcRequestUrl = context.Environment.GetEnvironmentVariable("ACTIONS_ID_TOKEN_REQUEST_URL");

        if (string.IsNullOrEmpty(oidcRequestToken) || string.IsNullOrEmpty(oidcRequestUrl))
            throw new InvalidOperationException("Missing GitHub OIDC request environment variables.");

        var tokenUrl = $"{oidcRequestUrl}&audience={Uri.EscapeDataString(nugetAudience)}";
        context.Information($"Requesting GitHub OIDC token from: {tokenUrl}");

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oidcRequestToken);

        var responseMessage = await http.GetAsync(tokenUrl);
        var tokenBody = await responseMessage.Content.ReadAsStringAsync();

        if (!responseMessage.IsSuccessStatusCode)
            throw new Exception("Failed to retrieve OIDC token from GitHub.");

        using var tokenDoc = JsonDocument.Parse(tokenBody);
        return ParseJsonProperty(tokenDoc, "value", "Failed to retrieve OIDC token from GitHub.");
    }

    private static async Task<string> ExchangeOidcTokenForApiKey(string oidcToken)
    {
        const string nugetUsername = "gittoolsbot";
        const string nugetTokenServiceUrl = "https://www.nuget.org/api/v2/token";

        var requestBody = JsonSerializer.Serialize(new { username = nugetUsername, tokenType = "ApiKey" });

        using var tokenServiceHttp = new HttpClient();
        tokenServiceHttp.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oidcToken);
        tokenServiceHttp.DefaultRequestHeaders.UserAgent.ParseAdd("nuget/login-action");
        using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var responseMessage = await tokenServiceHttp.PostAsync(nugetTokenServiceUrl, content);
        var exchangeBody = await responseMessage.Content.ReadAsStringAsync();

        if (!responseMessage.IsSuccessStatusCode)
        {
            var errorMessage = BuildErrorMessage((int)responseMessage.StatusCode, exchangeBody);
            throw new Exception(errorMessage);
        }

        using var respDoc = JsonDocument.Parse(exchangeBody);
        return ParseJsonProperty(respDoc, "apiKey", "Response did not contain \"apiKey\".");
    }

    private static string ParseJsonProperty(JsonDocument document, string propertyName, string errorMessage)
    {
        if (!document.RootElement.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
            throw new Exception(errorMessage);

        return property.GetString() ?? throw new Exception(errorMessage);
    }

    private static string BuildErrorMessage(int statusCode, string responseBody)
    {
        var errorMessage = $"Token exchange failed ({statusCode})";
        try
        {
            using var errDoc = JsonDocument.Parse(responseBody);
            errorMessage +=
                errDoc.RootElement.TryGetProperty("error", out var errProp) &&
                errProp.ValueKind == JsonValueKind.String
                    ? $": {errProp.GetString()}"
                    : $": {responseBody}";
        }
        catch (Exception)
        {
            errorMessage += $": {responseBody}";
        }

        return errorMessage;
    }
}
