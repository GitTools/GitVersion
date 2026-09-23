using System.Text.Json;
using System.Text.Json.Nodes;

namespace GitVersion.Sonar.Tests;

[TestFixture]
public class SonarResultTests
{
    private static readonly DateTimeOffset SubmittedAt = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);
    private static readonly RunIdentity Identity = new("GitTools/GitVersion", 123, 1, "pull_request", new('a', 40), new('b', 40), new('c', 40), 42, "feature", "main");
    private JsonObject checks = null!;
    private JsonArray comments = null!;

    [SetUp]
    public void SetUp()
    {
        this.checks = new JsonObject
        {
            ["check_runs"] = new JsonArray(new JsonObject
            {
                ["app"] = new JsonObject { ["slug"] = "sonarqubecloud" },
                ["head_sha"] = Identity.HeadSha,
                ["status"] = "completed",
                ["completed_at"] = SubmittedAt.AddSeconds(1).ToString("O")
            })
        };
        this.comments = new JsonArray(new JsonObject
        {
            ["user"] = new JsonObject { ["login"] = "sonarqubecloud[bot]" },
            ["updated_at"] = SubmittedAt.AddSeconds(1).ToString("O")
        });
    }

    [Test]
    public void HasDecorationAcceptsFreshSonarCheckAndComment() => Assert.That(HasDecoration(), Is.True);

    [TestCase("status", "in_progress")]
    [TestCase("head_sha", "different-head")]
    [TestCase("completed_at", "2026-09-23T11:59:59Z")]
    [TestCase("completed_at", "not-a-date")]
    public void HasDecorationRejectsIncompleteWrongHeadOrStaleCheck(string field, string value)
    {
        this.checks["check_runs"]![0]![field] = value;
        Assert.That(HasDecoration(), Is.False);
    }

    [Test]
    public void HasDecorationRejectsWrongCheckApp()
    {
        this.checks["check_runs"]![0]!["app"]!["slug"] = "attacker";
        Assert.That(HasDecoration(), Is.False);
    }

    [TestCase("2026-09-23T11:59:59Z")]
    [TestCase("not-a-date")]
    public void HasDecorationRejectsStaleOrInvalidCommentTimestamp(string timestamp)
    {
        this.comments[0]!["updated_at"] = timestamp;
        Assert.That(HasDecoration(), Is.False);
    }

    [Test]
    public void HasDecorationRejectsWrongCommentAuthor()
    {
        this.comments[0]!["user"]!["login"] = "attacker";
        Assert.That(HasDecoration(), Is.False);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void HasDecorationRequiresBothCheckAndComment(bool removeCheck)
    {
        if (removeCheck)
        {
            this.checks["check_runs"] = new JsonArray();
        }
        else
        {
            this.comments.Clear();
        }

        Assert.That(HasDecoration(), Is.False);
    }

    [Test]
    public void HasDecorationAcceptsExactSubmissionBoundary()
    {
        this.checks["check_runs"]![0]!["completed_at"] = SubmittedAt.ToString("O");
        this.comments[0]!["updated_at"] = SubmittedAt.ToString("O");
        Assert.That(HasDecoration(), Is.True);
    }

    private bool HasDecoration() => SonarResult.HasDecoration(JsonSerializer.SerializeToElement(this.checks), JsonSerializer.SerializeToElement(this.comments), Identity, SubmittedAt);
}
