using System.Net.Http.Headers;
using System.Text.Json;

namespace GitVersion.Sonar;

public sealed class GitHub : IDisposable
{
    private const string FullName = "full_name";
    private const string Repository = "GitTools/GitVersion";
    private readonly HttpClient client;

    public GitHub(HttpClient client) => this.client = client;

    public GitHub(string token)
    {
        this.client = new HttpClient { BaseAddress = new Uri("https://api.github.com/"), Timeout = TimeSpan.FromMinutes(2) }; // NOSONAR Fixed credential destination; never configurable by PR data.
        this.client.DefaultRequestHeaders.UserAgent.ParseAdd("GitVersion-Sonar/2");
        this.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        this.client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    public async Task<JsonElement> Get(string path)
    {
        using var response = await this.client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    public async Task<RunIdentity> Resolve(long runId, int attempt)
    {
        SafeFiles.Require(runId > 0 && attempt > 0, "Invalid run/attempt");
        var run = await Get($"repos/{Repository}/actions/runs/{runId}");
        var workflow = await Get($"repos/{Repository}/actions/workflows/ci.yml");
        SafeFiles.Require(run.GetProperty("workflow_id").GetInt64() == workflow.GetProperty("id").GetInt64() &&
                          run.GetProperty("path").GetString() == ".github/workflows/ci.yml" &&
                          run.GetProperty("repository").GetProperty(FullName).GetString() == Repository &&
                          run.GetProperty("run_attempt").GetInt32() == attempt &&
                          run.GetProperty("conclusion").GetString() == "success", "Source must be the latest successful attempt of upstream CI");
        var head = run.GetProperty("head_sha").GetString()!;
        var branch = run.GetProperty("head_branch").GetString()!;
        var eventName = run.GetProperty("event").GetString()!;
        if (eventName == "push")
        {
            SafeFiles.Require(run.GetProperty("head_repository").GetProperty(FullName).GetString() == Repository, "Unexpected push repository");
            var current = await Get($"repos/{Repository}/commits/{Uri.EscapeDataString(branch)}");
            SafeFiles.Require(current.GetProperty("sha").GetString() == head, "Stale branch run");
            var identity = new RunIdentity(Repository, runId, attempt, eventName, head, head, head, null, branch, branch);
            identity.Validate();
            return identity;
        }
        SafeFiles.Require(eventName == "pull_request", "Unsupported source event; merge-group mapping has not been accepted");
        // Fork workflow_run payloads can have no pull_requests. Resolve by commit association,
        // then compare the full source repository and exact current head, never branch name alone.
        var associated = await Get($"repos/{Repository}/commits/{head}/pulls?per_page=100");
        var candidates = associated.EnumerateArray().Where(p => p.GetProperty("state").GetString() == "open" &&
            p.GetProperty("head").GetProperty("sha").GetString() == head &&
            p.GetProperty("head").GetProperty("repo").GetProperty(FullName).GetString() == run.GetProperty("head_repository").GetProperty(FullName).GetString()).ToArray();
        SafeFiles.Require(candidates.Length == 1, "No unique current PR for source run");
        var number = candidates[0].GetProperty("number").GetInt32();
        var pr = await Get($"repos/{Repository}/pulls/{number}");
        var merge = pr.GetProperty("merge_commit_sha").GetString() ?? throw new InvalidDataException("PR has no merge revision");
        var baseSha = pr.GetProperty("base").GetProperty("sha").GetString()!;
        SafeFiles.Require(pr.GetProperty("head").GetProperty("sha").GetString() == head && pr.GetProperty("state").GetString() == "open", "PR changed during admission");
        var commit = await Get($"repos/{Repository}/commits/{merge}");
        var parents = commit.GetProperty("parents").EnumerateArray().Select(p => p.GetProperty("sha").GetString()).ToArray();
        SafeFiles.Require(parents.Length == 2 && parents[0] == baseSha && parents[1] == head, "Tested merge does not match current head/base");
        var result = new RunIdentity(Repository, runId, attempt, eventName, head, merge, baseSha, number,
            pr.GetProperty("head").GetProperty("ref").GetString()!, pr.GetProperty("base").GetProperty("ref").GetString()!);
        result.Validate();
        return result;
    }

    public async Task Download(RunIdentity identity, string zip)
    {
        var artifacts = await Get($"repos/{Repository}/actions/runs/{identity.RunId}/artifacts?per_page=100");
        SafeFiles.Require(artifacts.GetProperty("total_count").GetInt32() <= 100, "Too many run artifacts");
        var name = $"sonar-build-{identity.RunId}-{identity.Attempt}";
        var matching = artifacts.GetProperty("artifacts").EnumerateArray().Where(a => a.GetProperty("name").GetString() == name && !a.GetProperty("expired").GetBoolean()).ToArray();
        SafeFiles.Require(matching.Length == 1 && matching[0].GetProperty("size_in_bytes").GetInt64() <= SafeFiles.MaxTotalBytes, "Missing, ambiguous or oversized analysis artifact");
        var id = matching[0].GetProperty("id").GetInt64();
        using var response = await this.client.GetAsync($"repos/{Repository}/actions/artifacts/{id}/zip", HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await using var input = await response.Content.ReadAsStreamAsync();
        await using var output = new FileStream(zip, FileMode.CreateNew);
        var buffer = new byte[81920];
        long length = 0;
        int read;
        while ((read = await input.ReadAsync(buffer)) > 0)
        {
            length += read;
            SafeFiles.Require(length <= SafeFiles.MaxTotalBytes, "Oversized download");
            await output.WriteAsync(buffer.AsMemory(0, read));
        }
    }

    public static string ReceiptName(RunIdentity identity) => $"sonar-published-{identity.RunId}-{identity.Attempt}-{identity.HeadSha}";

    public async Task<bool> HasReceipt(RunIdentity identity)
    {
        // Only completed successful executions of the trusted receiver can create a receipt.
        // Source CI artifacts, including fork-defined names, cannot satisfy this lookup.
        var source = await Get($"repos/{Repository}/actions/runs/{identity.RunId}");
        var created = source.GetProperty("created_at").GetDateTimeOffset().ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        var since = Uri.EscapeDataString(">=" + created);
        for (var page = 1; ; page++)
        {
            var pagination = page == 1 ? "" : $"&page={page}";
            var response = await Get($"repos/{Repository}/actions/workflows/sonar_publish.yml/runs?status=success&per_page=100&created={since}{pagination}");
            SafeFiles.Require(!response.TryGetProperty("total_count", out var count) || count.GetInt32() <= 1000,
                "Receipt search exceeds GitHub's result limit; rerun source CI to narrow the recovery window");
            var runs = response.GetProperty("workflow_runs").EnumerateArray().ToArray();
            foreach (var run in runs)
            {
                var id = run.GetProperty("id").GetInt64();
                var artifacts = await Get($"repos/{Repository}/actions/runs/{id}/artifacts?per_page=100");
                if (artifacts.GetProperty("artifacts").EnumerateArray().Any(a => a.GetProperty("name").GetString() == ReceiptName(identity) && !a.GetProperty("expired").GetBoolean()))
                {
                    return true;
                }
            }
            if (runs.Length < 100)
            {
                return false;
            }
        }
    }

    public void Dispose() => this.client.Dispose();
}
