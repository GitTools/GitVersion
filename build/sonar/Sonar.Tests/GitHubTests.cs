using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace GitVersion.Sonar.Tests;

[TestFixture]
public class GitHubTests
{
    private static readonly string Head = new('a', 40);
    private static readonly string Merge = new('b', 40);
    private static readonly string Base = new('c', 40);
    private const string Prefix = "/repos/GitTools/GitVersion/";
    private Dictionary<string, JsonNode> responses = null!;

    [SetUp]
    public void SetUp()
    {
        var run = JsonNode.Parse("""
            {"workflow_id":10,"path":".github/workflows/ci.yml","repository":{"full_name":"GitTools/GitVersion"},"run_attempt":1,"conclusion":"success","head_branch":"feature","event":"pull_request","head_repository":{"full_name":"contributor/GitVersion"},"pull_requests":[]}
            """)!;
        run["head_sha"] = Head;
        var pr = JsonNode.Parse("""
            {"number":42,"state":"open","head":{"ref":"feature","repo":{"full_name":"contributor/GitVersion"}},"base":{"ref":"main"}}
            """)!;
        pr["head"]!["sha"] = Head;
        pr["base"]!["sha"] = Base;
        pr["merge_commit_sha"] = Merge;
        this.responses = new()
        {
            [Prefix + "actions/runs/123"] = run,
            [Prefix + "actions/workflows/ci.yml"] = new JsonObject { ["id"] = 10 },
            [Prefix + $"commits/{Head}/pulls?per_page=100"] = new JsonArray(pr.DeepClone()),
            [Prefix + "pulls/42"] = pr,
            [Prefix + $"commits/{Merge}"] = new JsonObject { ["parents"] = new JsonArray(new JsonObject { ["sha"] = Base }, new JsonObject { ["sha"] = Head }) }
        };
    }

    [Test]
    public async Task ResolveForkPullRequestWithEmptyRunAssociationUsesExactCommitAndRepository()
    {
        using var github = Client();
        Assert.That(await github.Resolve(123, 1), Is.EqualTo(new RunIdentity("GitTools/GitVersion", 123, 1, "pull_request", Head, Merge, Base, 42, "feature", "main")));
    }

    [Test]
    public async Task ResolveCurrentUpstreamPushReturnsBranchIdentity()
    {
        ConfigurePush();
        using var github = Client();
        Assert.That(await github.Resolve(123, 1), Is.EqualTo(new RunIdentity("GitTools/GitVersion", 123, 1, "push", Head, Head, Head, null, "feature", "feature")));
    }

    [TestCase("workflow_id", "99")]
    [TestCase("path", "\".github/workflows/untrusted.yml\"")]
    [TestCase("run_attempt", "2")]
    [TestCase("conclusion", "\"failure\"")]
    public void ResolveRejectsWrongWorkflowAttemptOrConclusion(string field, string value)
    {
        this.responses[Prefix + "actions/runs/123"][field] = JsonNode.Parse(value);
        using var github = Client();
        Assert.That(async () => await github.Resolve(123, 1), Throws.TypeOf<InvalidDataException>().With.Message.Contains("latest successful attempt"));
    }

    [Test]
    public void ResolveRejectsStalePush()
    {
        ConfigurePush();
        this.responses[Prefix + "commits/feature"]["sha"] = new string('d', 40);
        using var github = Client();
        Assert.That(async () => await github.Resolve(123, 1), Throws.TypeOf<InvalidDataException>().With.Message.Contains("Stale branch"));
    }

    [Test]
    public void ResolveRejectsHeadChangedDuringAdmission()
    {
        this.responses[Prefix + "pulls/42"]["head"]!["sha"] = new string('d', 40);
        using var github = Client();
        Assert.That(async () => await github.Resolve(123, 1), Throws.TypeOf<InvalidDataException>().With.Message.Contains("changed during admission"));
    }

    [Test]
    public void ResolveRejectsBaseThatDoesNotMatchTestedMerge()
    {
        this.responses[Prefix + "pulls/42"]["base"]!["sha"] = new string('d', 40);
        using var github = Client();
        Assert.That(async () => await github.Resolve(123, 1), Throws.TypeOf<InvalidDataException>().With.Message.Contains("current head/base"));
    }

    [Test]
    public void ResolveRejectsAmbiguousPullRequests()
    {
        var candidates = this.responses[Prefix + $"commits/{Head}/pulls?per_page=100"].AsArray();
        var duplicate = candidates[0]!.DeepClone();
        duplicate["number"] = 43;
        candidates.Add(duplicate);
        using var github = Client();
        Assert.That(async () => await github.Resolve(123, 1), Throws.TypeOf<InvalidDataException>().With.Message.Contains("unique current PR"));
    }

    [Test]
    public void ResolveRejectsSameCommitFromDifferentFork()
    {
        this.responses[Prefix + $"commits/{Head}/pulls?per_page=100"][0]!["head"]!["repo"]!["full_name"] = "other/GitVersion";
        using var github = Client();
        Assert.That(async () => await github.Resolve(123, 1), Throws.TypeOf<InvalidDataException>().With.Message.Contains("unique current PR"));
    }

    [Test]
    public void ResolveRejectsUnsupportedMergeGroup()
    {
        this.responses[Prefix + "actions/runs/123"]["event"] = "merge_group";
        using var github = Client();
        Assert.That(async () => await github.Resolve(123, 1), Throws.TypeOf<InvalidDataException>().With.Message.Contains("Unsupported source event"));
    }

    [Test]
    public async Task DownloadSelectsExactRunAttemptArtifactAndPreservesBytes()
    {
        ConfigureArtifact();
        var directory = Directory.CreateTempSubdirectory("sonar-download-tests-").FullName;
        try
        {
            using var github = Client();
            var path = Path.Combine(directory, "artifact.zip");
            await github.Download(DownloadIdentity, path);
            Assert.That(await File.ReadAllTextAsync(path), Is.EqualTo("{\"payload\":\"selected artifact\"}"));
        }
        finally { Directory.Delete(directory, true); }
    }

    [TestCase("expired", "true")]
    [TestCase("name", "\"sonar-build-123-2\"")]
    [TestCase("size_in_bytes", "1073741825")]
    public void DownloadRejectsExpiredWrongAttemptOrOversizedArtifact(string field, string value)
    {
        ConfigureArtifact();
        this.responses[Prefix + "actions/runs/123/artifacts?per_page=100"]["artifacts"]![0]![field] = JsonNode.Parse(value);
        using var github = Client();
        Assert.That(async () => await github.Download(DownloadIdentity, "must-not-be-created.zip"), Throws.TypeOf<InvalidDataException>().With.Message.Contains("analysis artifact"));
    }

    [Test]
    public void DownloadRejectsAmbiguousArtifactNames()
    {
        ConfigureArtifact();
        var artifacts = this.responses[Prefix + "actions/runs/123/artifacts?per_page=100"]["artifacts"]!.AsArray();
        artifacts.Add(artifacts[0]!.DeepClone());
        using var github = Client();
        Assert.That(async () => await github.Download(DownloadIdentity, "must-not-be-created.zip"), Throws.TypeOf<InvalidDataException>().With.Message.Contains("ambiguous"));
    }

    private static RunIdentity DownloadIdentity => new("GitTools/GitVersion", 123, 1, "pull_request", Head, Merge, Base, 42, "feature", "main");

    private void ConfigureArtifact()
    {
        this.responses[Prefix + "actions/runs/123/artifacts?per_page=100"] = JsonNode.Parse("""
            {"total_count":1,"artifacts":[{"id":99,"name":"sonar-build-123-1","expired":false,"size_in_bytes":100}]}
            """)!;
        this.responses[Prefix + "actions/artifacts/99/zip"] = new JsonObject { ["payload"] = "selected artifact" };
    }

    [Test]
    public void ReceiptNameContainsRunAttemptAndHead() =>
        Assert.That(GitHub.ReceiptName(DownloadIdentity), Is.EqualTo($"sonar-published-123-1-{Head}"));

    [TestCase(true, false, true)]
    [TestCase(true, true, false)]
    [TestCase(false, false, false)]
    public async Task HasReceiptAcceptsOnlyCurrentUnexpiredTrustedReceiverReceipt(bool matchingName, bool expired, bool expected)
    {
        this.responses[Prefix + "actions/workflows/sonar_publish.yml/runs?status=success&per_page=100"] =
            new JsonObject { ["workflow_runs"] = new JsonArray(new JsonObject { ["id"] = 777 }) };
        this.responses[Prefix + "actions/runs/777/artifacts?per_page=100"] = new JsonObject
        {
            ["artifacts"] = new JsonArray(new JsonObject { ["name"] = matchingName ? GitHub.ReceiptName(DownloadIdentity) : "sonar-published-other-run", ["expired"] = expired })
        };
        using var github = Client();
        Assert.That(await github.HasReceipt(DownloadIdentity), Is.EqualTo(expected));
    }

    [Test]
    public async Task HasReceiptReturnsFalseWithoutSuccessfulReceiverRuns()
    {
        this.responses[Prefix + "actions/workflows/sonar_publish.yml/runs?status=success&per_page=100"] = new JsonObject { ["workflow_runs"] = new JsonArray() };
        using var github = Client();
        Assert.That(await github.HasReceipt(DownloadIdentity), Is.False);
    }

    private void ConfigurePush()
    {
        var run = this.responses[Prefix + "actions/runs/123"];
        run["event"] = "push";
        run["head_repository"]!["full_name"] = "GitTools/GitVersion";
        this.responses[Prefix + "commits/feature"] = new JsonObject { ["sha"] = Head };
    }

    private GitHub Client() => new(new HttpClient(new StubHandler(this.responses)) { BaseAddress = new Uri("https://api.github.com/") });

    private sealed class StubHandler(Dictionary<string, JsonNode> responses) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.PathAndQuery;
            if (!responses.TryGetValue(path, out var response))
            {
                throw new InvalidOperationException("Unexpected request: " + path);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response.ToJsonString(), Encoding.UTF8, "application/json") });
        }
    }
}
