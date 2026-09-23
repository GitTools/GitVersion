namespace GitVersion.Sonar.Tests;

[TestFixture]
public class RunIdentityTests
{
    private static RunIdentity PullRequest() => new("GitTools/GitVersion", 123, 1, "pull_request", new('a', 40), new('b', 40), new('c', 40), 42, "feature/coverage", "main");

    [TestCase("pull_request")]
    [TestCase("push")]
    public void ValidateAcceptsSupportedEvents(string eventName)
    {
        var identity = PullRequest() with { Event = eventName, PullRequest = eventName == "push" ? null : 42, AnalyzedSha = eventName == "push" ? new('a', 40) : new('b', 40) };
        Assert.That(() => identity.Validate(), Throws.Nothing);
    }

    private static IEnumerable<TestCaseData> InvalidIdentities()
    {
        var value = PullRequest();
        yield return new(value with { Repository = "attacker/GitVersion" });
        yield return new(value with { RunId = 0 });
        yield return new(value with { RunId = -1 });
        yield return new(value with { Attempt = 0 });
        yield return new(value with { Event = "workflow_dispatch" });
        yield return new(value with { Event = "merge_group" });
        yield return new(value with { HeadSha = new('A', 40) });
        yield return new(value with { HeadSha = new('a', 39) });
        yield return new(value with { AnalyzedSha = new('b', 41) });
        yield return new(value with { BaseSha = new('g', 40) });
        yield return new(value with { PullRequest = null });
        yield return new(value with { PullRequest = 0 });
        yield return new(value with { Branch = "" });
        yield return new(value with { BaseBranch = "main\n" });
        yield return new(value with { Event = "push", PullRequest = null });
        yield return new(value with { Event = "push", AnalyzedSha = value.HeadSha });
    }

    [TestCaseSource(nameof(InvalidIdentities))]
    public void ValidateRejectsInvalidRunMetadata(RunIdentity identity) =>
        Assert.That(() => identity.Validate(), Throws.TypeOf<InvalidDataException>());

    [Test]
    public void EnsureCurrentAcceptsExactIdentity() =>
        Assert.That(() => PullRequest().EnsureCurrent(PullRequest()), Throws.Nothing);

    private static IEnumerable<TestCaseData> ChangedIdentities()
    {
        var value = PullRequest();
        yield return new(value with { Repository = "fork/GitVersion" });
        yield return new(value with { RunId = 124 });
        yield return new(value with { Attempt = 2 });
        yield return new(value with { Event = "push" });
        yield return new(value with { HeadSha = new('d', 40) });
        yield return new(value with { AnalyzedSha = new('d', 40) });
        yield return new(value with { BaseSha = new('d', 40) });
        yield return new(value with { PullRequest = 43 });
        yield return new(value with { Branch = "other-feature" });
        yield return new(value with { BaseBranch = "develop" });
    }

    [TestCaseSource(nameof(ChangedIdentities))]
    public void EnsureCurrentRejectsEveryIdentityMismatch(RunIdentity expected) =>
        Assert.That(() => PullRequest().EnsureCurrent(expected), Throws.TypeOf<InvalidDataException>());
}
