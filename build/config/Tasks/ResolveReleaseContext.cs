using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Common.Utilities;

namespace Config.Tasks;

[TaskDescription("Validate the selected release and resolve CI publication flags")]
public partial class ResolveReleaseContext : AsyncFrostingTask<BuildContext>
{
    public override async Task RunAsync(BuildContext context)
    {
        var repository = context.EnvironmentVariable("GITHUB_REPOSITORY");
        var eventName = context.EnvironmentVariable("GITHUB_EVENT_NAME");
        var main = context.EnvironmentVariable("GITHUB_REF_NAME") == Constants.DefaultBranch;
        var original = repository == $"{Constants.RepoOwner}/{Constants.Repository}";
        var dispatched = eventName == "repository_dispatch";
        var canPublish = original && main;
        var fullValidation = original && (main || eventName == "merge_group" || dispatched);
        var prerelease = false;

        if (dispatched)
        {
            if (!original)
            {
                throw new InvalidOperationException("Release dispatch must target the original repository.");
            }

            using var eventDocument = JsonDocument.Parse(await File.ReadAllTextAsync(context.EnvironmentVariable("GITHUB_EVENT_PATH")));
            var release = ReadSelection(eventDocument.RootElement);
            await ValidateReleaseAsync(context, release);
            canPublish = true;
            prerelease = release.Prerelease;
        }

        context.GitHubActions().Commands.SetOutputParameter("can_publish", canPublish.ToString().ToLowerInvariant());
        context.GitHubActions().Commands.SetOutputParameter("run_full_validation", fullValidation.ToString().ToLowerInvariant());
        context.GitHubActions().Commands.SetOutputParameter("prerelease", prerelease.ToString().ToLowerInvariant());
    }

    private static ReleasePublication ReadSelection(JsonElement eventData)
    {
        if (eventData.GetProperty("action").GetString() != "ci-release")
        {
            throw new InvalidOperationException("Unexpected release dispatch action.");
        }

        var payload = eventData.GetProperty("client_payload");
        var tag = payload.GetProperty("tag").GetString();
        var sha = payload.GetProperty("sha").GetString();
        var reference = payload.GetProperty("ref").GetString();
        if (string.IsNullOrEmpty(tag) || tag != tag.Trim() || !VersionTag().IsMatch(tag) ||
            string.IsNullOrEmpty(sha) || !CommitSha().IsMatch(sha) || reference != $"refs/tags/{tag}")
        {
            throw new InvalidOperationException("Release dispatch requires a version tag, its ref, and a full commit SHA.");
        }

        return ReleasePublication.Create(tag, sha);
    }

    private static async Task ValidateReleaseAsync(BuildContext context, ReleasePublication selection)
    {
        using var client = new HttpClient
        {
            BaseAddress = new Uri((context.EnvironmentVariable("GITHUB_API_URL") ?? "https://api.github.com").TrimEnd('/') + "/")
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", context.EnvironmentVariable("GITHUB_TOKEN"));
        client.DefaultRequestHeaders.UserAgent.ParseAdd("GitVersion-release-validation");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        var repository = $"repos/{Constants.RepoOwner}/{Constants.Repository}";
        using var releaseDocument = await GetJsonAsync(client, $"{repository}/releases/tags/{Uri.EscapeDataString(selection.Name)}");
        using var commitDocument = await GetJsonAsync(client, $"{repository}/commits/{Uri.EscapeDataString($"refs/tags/{selection.Name}")}");
        using var comparisonDocument = await GetJsonAsync(client, $"{repository}/compare/{selection.TargetCommitish}...{Constants.DefaultBranch}");
        var release = releaseDocument.RootElement;
        var commit = commitDocument.RootElement;
        var comparison = comparisonDocument.RootElement;

        if (release.GetProperty("draft").GetBoolean() || release.GetProperty("tag_name").GetString() != selection.Name ||
            release.GetProperty("prerelease").GetBoolean() != selection.Prerelease ||
            commit.GetProperty("sha").GetString() != selection.TargetCommitish ||
            comparison.GetProperty("merge_base_commit").GetProperty("sha").GetString() != selection.TargetCommitish)
        {
            throw new InvalidOperationException("Release metadata, tag SHA, or main ancestry does not match the dispatch.");
        }

        if (selection.Prerelease && string.IsNullOrWhiteSpace(release.GetProperty("body").GetString()))
        {
            throw new InvalidOperationException("Beta releases require reviewed release notes before publishing.");
        }

        if (selection.Prerelease)
        {
            using var content = await GetJsonAsync(client, $"{repository}/contents/build/release/protocol.json?ref={selection.TargetCommitish}");
            var encoded = content.RootElement.GetProperty("content").GetString()!;
            using var protocol = JsonDocument.Parse(Convert.FromBase64String(encoded));
            if (protocol.RootElement.GetProperty("betaPublishing").GetInt32() != 1)
            {
                throw new InvalidOperationException("The selected commit does not support this beta publication protocol.");
            }
        }
    }

    private static async Task<JsonDocument> GetJsonAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    [GeneratedRegex(@"^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$")]
    private static partial Regex VersionTag();

    [GeneratedRegex(@"\A[0-9a-f]{40}\z")]
    private static partial Regex CommitSha();
}
