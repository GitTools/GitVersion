using GitVersion.Git;
using GitVersion.Tests;

namespace GitVersion.Configuration.Tests;

[TestFixture]
public class ConfigurationExtensionsTests : TestBase
{
    private const string BranchName = "pull-request";
    private const string CommitSha = "1234567890abcdef1234567890abcdef12345678";

    [TestCase("{Sha}", CommitSha)]
    [TestCase("ci.{ShortSha}", "ci.1234567")]
    [TestCase("{ShortSha}.{Sha}.{ShortSha}", "1234567." + CommitSha + ".1234567")]
    [TestCase("{StoryNo}.{BranchName}.{ShortSha}", "sc-42.add-login.1234567")]
    [TestCase("{env:BUILD_LABEL ?? ShortSha}.{Sha}", "1234567." + CommitSha)]
    [TestCase("{Missing ?? ShortSha}", "1234567")]
    [TestCase(null, null)]
    [TestCase("", "")]
    [TestCase("literal", "literal")]
    public void CommitLabelPlaceholdersComposeWithCapturesAndFallbacks(string? label, string? expected)
    {
        var configuration = LabelConfiguration(label, @"^feature/(?<StoryNo>sc-\d+)/(?<BranchName>.+)");

        var actual = configuration.GetBranchSpecificLabel("feature/sc-42/add-login", null, new TestEnvironment(), CreateCommit());

        actual.ShouldBe(expected);
    }

    [TestCase(null, "feature/topic", null)]
    [TestCase("", "feature/topic", null)]
    [TestCase("^release/", "feature/topic", null)]
    [TestCase("^feature/", null, null)]
    [TestCase("^feature/", "", null)]
    [TestCase("^feature/", "feature/topic", "release/override")]
    public void CommitLabelPlaceholdersDoNotRequireMatchingBranchRegex(string? regex, string? branch, string? branchOverride)
    {
        var actual = LabelConfiguration("ci.{ShortSha}", regex)
            .GetBranchSpecificLabel(branch, branchOverride, new TestEnvironment(), CreateCommit());

        actual.ShouldBe("ci.1234567");
    }

    [TestCase("Sha", "captured", "ci.captured")]
    [TestCase("ShortSha", "captured", "ci.captured")]
    [TestCase("Sha", "", "ci.")]
    [TestCase("ShortSha", "", "ci.")]
    public void NamedCapturesTakePrecedenceOverCommitPlaceholders(string name, string captured, string expected)
    {
        var configuration = LabelConfiguration($"ci.{{{name}}}", $"^feature/(?<{name}>.+)?$");

        var actual = configuration.GetBranchSpecificLabel($"feature/{captured}", null, new TestEnvironment(), CreateCommit());

        actual.ShouldBe(expected);
    }

    [Test]
    public void BranchOverrideChangesCaptureButKeepsCurrentCommitHash()
    {
        var configuration = LabelConfiguration("{BranchName}.{ShortSha}", "^feature/(?<BranchName>.+)");

        var actual = configuration.GetBranchSpecificLabel(ReferenceName.FromBranchName("feature/original"),
            "feature/override", new TestEnvironment(), CreateCommit());

        actual.ShouldBe("override.1234567");
    }

    [Test]
    public void EnvironmentValuesComposeWithCommitPlaceholdersAndAreSanitized()
    {
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable("BUILD_LABEL", "team/topic");

        var actual = LabelConfiguration("{env:BUILD_LABEL}.{ShortSha}", null)
            .GetBranchSpecificLabel("feature/topic", null, environment, CreateCommit());

        actual.ShouldBe("team-topic.1234567");
    }

    [TestCase("{Unknown}.{ShortSha}")]
    [TestCase("{env:MISSING_VAR}.{ShortSha}")]
    [TestCase("{sha}.{ShortSha}")]
    [TestCase("{shortsha}.{Sha}")]
    public void CommitPlaceholdersDoNotSuppressUnknownPlaceholderErrors(string label)
        => Should.Throw<ArgumentException>(() => LabelConfiguration(label, null)
            .GetBranchSpecificLabel("feature/topic", null, new TestEnvironment(), CreateCommit()));

    [Test]
    public void CommitPlaceholdersPreserveMalformedFallbackErrors()
        => Should.Throw<FormatException>(() => LabelConfiguration("{env:MISSING_VAR ??? \"fallback\"}.{ShortSha}", null)
            .GetBranchSpecificLabel("feature/topic", null, new TestEnvironment(), CreateCommit()));

    [TestCase(null)]
    [TestCase("ci.{ShortSha}")]
    public void LabelResolutionRequiresExplicitCurrentCommit(string? label)
        => Should.Throw<ArgumentNullException>(() => LabelConfiguration(label, null)
            .GetBranchSpecificLabel("feature/topic", null, new TestEnvironment(), null!));

    private static EffectiveConfiguration LabelConfiguration(string? label, string? regex) =>
        new(GitFlowConfigurationBuilder.New.WithLabel(null).Build(),
            new BranchConfiguration { Label = label, RegularExpression = regex });

    private static ICommit CreateCommit()
    {
        var id = Substitute.For<IObjectId>();
        id.Sha.Returns(CommitSha);
        id.ToString(7).Returns("1234567");
        var commit = Substitute.For<ICommit>();
        commit.Sha.Returns(CommitSha);
        commit.Id.Returns(id);
        return commit;
    }

    [Test]
    public void EnsureGetEffectiveConfigurationWithoutBranchUsesEmptyBranchConfiguration()
    {
        var configuration = EmptyConfigurationBuilder.New
            .WithCustomVersionFormat("{SemVer}-custom")
            .Build();

        var effectiveConfiguration = configuration.GetEffectiveConfiguration();

        effectiveConfiguration.CustomVersionFormat.ShouldBe("{SemVer}-custom");
        effectiveConfiguration.PreReleaseWeight.ShouldBe(0);
    }

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
        var actual = effectiveConfiguration.GetBranchSpecificLabel(ReferenceName.FromBranchName(branchName), null, new TestEnvironment(), CreateCommit());
        actual.ShouldBe(expectedLabel);
    }

    [Test]
    public void EnsureGetBranchSpecificLabelProcessesEnvironmentVariables()
    {
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable("GITHUB_HEAD_REF", "feature-branch");

        var configuration = GitFlowConfigurationBuilder.New
            .WithoutBranches()
            .WithBranch(BranchName, builder => builder
                .WithLabel("pr-{env:GITHUB_HEAD_REF}")
                .WithRegularExpression(@"^pull[/-]"))
            .Build();

        var effectiveConfiguration = configuration.GetEffectiveConfiguration(ReferenceName.FromBranchName(BranchName));
        var actual = effectiveConfiguration.GetBranchSpecificLabel(ReferenceName.FromBranchName(BranchName), null, environment, CreateCommit());
        actual.ShouldBe("pr-feature-branch");
    }

    [Test]
    public void EnsureGetBranchSpecificLabelProcessesEnvironmentVariablesWithFallback()
    {
        var environment = new TestEnvironment();
        // Don't set GITHUB_HEAD_REF to test fallback

        var configuration = GitFlowConfigurationBuilder.New
            .WithoutBranches()
            .WithBranch(BranchName, builder => builder
                .WithLabel("pr-{env:GITHUB_HEAD_REF ?? \"unknown\"}")
                .WithRegularExpression(@"^pull[/-]"))
            .Build();

        var effectiveConfiguration = configuration.GetEffectiveConfiguration(ReferenceName.FromBranchName(BranchName));
        var actual = effectiveConfiguration.GetBranchSpecificLabel(ReferenceName.FromBranchName(BranchName), null, environment, CreateCommit());
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
        var actual = effectiveConfiguration.GetBranchSpecificLabel(ReferenceName.FromBranchName("feature/test-branch"), null, environment, CreateCommit());
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
        var actual = effectiveConfiguration.GetBranchSpecificLabel(ReferenceName.FromBranchName("feature/test"), null, new TestEnvironment(), CreateCommit());
        actual.ShouldBe("test");
    }

    [Test]
    public void EnsureGetBranchSpecificLabelThrowsWhenEnvVarMissing()
    {
        var environment = new TestEnvironment();
        // Do not set MISSING_VAR

        var configuration = GitFlowConfigurationBuilder.New
            .WithoutBranches()
            .WithBranch(BranchName, builder => builder
                .WithLabel("pr-{env:MISSING_VAR}")
                .WithRegularExpression(@"^pull[/-]"))
            .Build();

        var effectiveConfiguration = configuration.GetEffectiveConfiguration(ReferenceName.FromBranchName(BranchName));
        Should.Throw<ArgumentException>(() =>
            effectiveConfiguration.GetBranchSpecificLabel(ReferenceName.FromBranchName(BranchName), null, environment, CreateCommit()));
    }

    [Test]
    public void EnsureGetBranchSpecificLabelThrowsWhenBranchNamePropertyMissing()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithoutBranches()
            .WithBranch("feature/test", builder => builder
                .WithLabel("{BranchName}")
                .WithRegularExpression(@"^features?[\/-]"))
            .Build();

        var effectiveConfiguration = configuration.GetEffectiveConfiguration(ReferenceName.FromBranchName(BranchName));
        Should.Throw<ArgumentException>(() =>
            effectiveConfiguration.GetBranchSpecificLabel(ReferenceName.FromBranchName(BranchName), null, new TestEnvironment(), CreateCommit()));
    }

    [TestCase("case-00/my-branch", "case-00-my-branch")]
    [TestCase("my-branch", "my-branch")]
    [TestCase("my_branch/valid", "my-branch-valid")]
    public void EnsureGetBranchSpecificLabelReturnsValidLabelForEnvironmentVariables(string variable, string expectedLabel)
    {
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable("GITHUB_HEAD_REF", variable);

        var configuration = GitFlowConfigurationBuilder.New
            .WithoutBranches()
            .WithBranch("feature/test-feature", builder => builder
                .WithLabel("{env:GITHUB_HEAD_REF}")
                .WithRegularExpression(@"^features?[\/-](?<BranchName>.+)"))
            .Build();

        var effectiveConfiguration = configuration.GetEffectiveConfiguration(ReferenceName.FromBranchName("feature/test-feature"));
        var actual = effectiveConfiguration.GetBranchSpecificLabel(ReferenceName.FromBranchName("feature/test-feature"), null, environment, CreateCommit());
        actual.ShouldBe(expectedLabel);
    }
}
