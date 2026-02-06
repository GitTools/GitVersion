using GitVersion.Core.Tests.Helpers;
using GitVersion.Git;

namespace GitVersion.Configuration.Tests;

[TestFixture]
public class ConfigurationExtensionsTests : TestBase
{
    private const string PullRequestBranch = "pull-request";
    [TestCase("release/2.0.0",
        "refs/heads/release/2.0.0", "release/2.0.0", "release/2.0.0",
        true, false, false, false, true)]
    [TestCase("upstream/release/2.0.0",
        "refs/heads/upstream/release/2.0.0", "upstream/release/2.0.0", "upstream/release/2.0.0",
        true, false, false, false, false)]
    [TestCase("origin/release/2.0.0",
        "refs/heads/origin/release/2.0.0", "origin/release/2.0.0", "origin/release/2.0.0",
        true, false, false, false, false)]
    [TestCase("refs/remotes/upstream/release/2.0.0",
        "refs/remotes/upstream/release/2.0.0", "upstream/release/2.0.0", "upstream/release/2.0.0",
        false, false, true, false, false)]
    [TestCase("refs/remotes/origin/release/2.0.0",
        "refs/remotes/origin/release/2.0.0", "origin/release/2.0.0", "release/2.0.0",
        false, false, true, false, true)]
    public void EnsureIsReleaseBranchWithReferenceNameWorksAsExpected(string branchName, string expectedCanonical, string expectedFriendly, string expectedWithoutOrigin,
        bool expectedIsLocalBranch, bool expectedIsPullRequest, bool expectedIsRemoteBranch, bool expectedIsTag, bool expectedIsReleaseBranch)
    {
        var configuration = GitFlowConfigurationBuilder.New.Build();

        var actual = ReferenceName.FromBranchName(branchName);
        var isReleaseBranch = configuration.IsReleaseBranch(actual);

        actual.Canonical.ShouldBe(expectedCanonical);
        actual.Friendly.ShouldBe(expectedFriendly);
        actual.WithoutOrigin.ShouldBe(expectedWithoutOrigin);
        actual.IsLocalBranch.ShouldBe(expectedIsLocalBranch);
        actual.IsPullRequest.ShouldBe(expectedIsPullRequest);
        actual.IsRemoteBranch.ShouldBe(expectedIsRemoteBranch);
        actual.IsTag.ShouldBe(expectedIsTag);
        isReleaseBranch.ShouldBe(expectedIsReleaseBranch);
    }

    [TestCase("feature/sc-1000/Description", @"^features?[\/-](?<BranchName>.+)", "{BranchName}", "sc-1000-Description")]
    [TestCase("feature/sc-1000/Description", @"^features?[\/-](?<StoryNo>sc-\d+)[-\/].+", "{StoryNo}", "sc-1000")]
    public void EnsureGetBranchSpecificLabelWorksAsExpected(string branchName, string regularExpression, string label, string expectedLabel)
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithoutBranches()
            .WithBranch(branchName, builder => builder
                .WithLabel(label)
                .WithRegularExpression(regularExpression))
            .Build();

        var effectiveConfiguration = configuration.GetEffectiveConfiguration(ReferenceName.FromBranchName(branchName));
        var actual = effectiveConfiguration.GetBranchSpecificLabel(ReferenceName.FromBranchName(branchName), null);
        actual.ShouldBe(expectedLabel);
    }

    [Test]
    public void EnsureGetBranchSpecificLabelProcessesEnvironmentVariables()
    {
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable("GITHUB_HEAD_REF", "feature-branch");

        var configuration = GitFlowConfigurationBuilder.New
            .WithoutBranches()
            .WithBranch(PullRequestBranch, builder => builder
                .WithLabel("pr-{env:GITHUB_HEAD_REF}")
                .WithRegularExpression(@"^pull[/-]"))
            .Build();

        var effectiveConfiguration = configuration.GetEffectiveConfiguration(ReferenceName.FromBranchName(PullRequestBranch));
        var actual = effectiveConfiguration.GetBranchSpecificLabel(ReferenceName.FromBranchName(PullRequestBranch), null, environment);
        actual.ShouldBe("pr-feature-branch");
    }

    [Test]
    public void EnsureGetBranchSpecificLabelProcessesEnvironmentVariablesWithFallback()
    {
        var environment = new TestEnvironment();
        // Don't set GITHUB_HEAD_REF to test fallback

        var configuration = GitFlowConfigurationBuilder.New
            .WithoutBranches()
            .WithBranch(PullRequestBranch, builder => builder
                .WithLabel("pr-{env:GITHUB_HEAD_REF ?? \"unknown\"}")
                .WithRegularExpression(@"^pull[/-]"))
            .Build();

        var effectiveConfiguration = configuration.GetEffectiveConfiguration(ReferenceName.FromBranchName(PullRequestBranch));
        var actual = effectiveConfiguration.GetBranchSpecificLabel(ReferenceName.FromBranchName(PullRequestBranch), null, environment);
        actual.ShouldBe("pr-unknown");
    }

    [Test]
    public void EnsureGetBranchSpecificLabelProcessesEnvironmentVariablesAndRegexPlaceholders()
    {
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable("GITHUB_HEAD_REF", "feature-branch");

        var configuration = GitFlowConfigurationBuilder.New
            .WithoutBranches()
            .WithBranch("feature/test-branch", builder => builder
                .WithLabel("{BranchName}-{env:GITHUB_HEAD_REF}")
                .WithRegularExpression(@"^features?[\/-](?<BranchName>.+)"))
            .Build();

        var effectiveConfiguration = configuration.GetEffectiveConfiguration(ReferenceName.FromBranchName("feature/test-branch"));
        var actual = effectiveConfiguration.GetBranchSpecificLabel(ReferenceName.FromBranchName("feature/test-branch"), null, environment);
        actual.ShouldBe("test-branch-feature-branch");
    }

    [Test]
    public void EnsureGetBranchSpecificLabelWorksWithoutEnvironmentWhenNoEnvPlaceholders()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithoutBranches()
            .WithBranch("feature/test", builder => builder
                .WithLabel("{BranchName}")
                .WithRegularExpression(@"^features?[\/-](?<BranchName>.+)"))
            .Build();

        var effectiveConfiguration = configuration.GetEffectiveConfiguration(ReferenceName.FromBranchName("feature/test"));
        var actual = effectiveConfiguration.GetBranchSpecificLabel(ReferenceName.FromBranchName("feature/test"), null, null);
        actual.ShouldBe("test");
    }

    [Test]
    public void EnsureGetBranchSpecificLabelProcessesEnvironmentVariablesEvenWhenRegexDoesNotMatch()
    {
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable("GITHUB_HEAD_REF", "feature-branch");

        var configuration = GitFlowConfigurationBuilder.New
            .WithoutBranches()
            .WithBranch(PullRequestBranch, builder => builder
                .WithLabel("pr-{env:GITHUB_HEAD_REF}")
                .WithRegularExpression(@"^pull[/-]"))
            .WithBranch("feature", builder => builder
                .WithLabel("{BranchName}")
                .WithRegularExpression(@"^features?[\/-](?<BranchName>.+)"))
            .Build();

        var effectiveConfiguration = configuration.GetEffectiveConfiguration(ReferenceName.FromBranchName("feature/other-branch"));
        var actual = effectiveConfiguration.GetBranchSpecificLabel(ReferenceName.FromBranchName("feature/other-branch"), null, environment);
        actual.ShouldBe("other-branch");
    }
}
