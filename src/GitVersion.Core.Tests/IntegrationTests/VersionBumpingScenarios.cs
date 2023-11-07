using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.VersionCalculation;
using LibGit2Sharp;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class VersionBumpingScenarios : TestBase
{
    [Test]
    public void AppliedPreReleaseLabelCausesBump()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch(MainBranch, builder => builder.WithLabel("pre").WithSourceBranches())
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeATaggedCommit("1.0.0-pre.1");
        fixture.Repository.MakeACommit();

        fixture.AssertFullSemver("1.0.0-pre.2", configuration);
    }

    [Test]
    public void CanUseCommitMessagesToBumpVersion()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeACommit("+semver:minor");

        fixture.AssertFullSemver("1.1.0-1");

        fixture.Repository.MakeACommit("+semver:major");

        fixture.AssertFullSemver("2.0.0-2");
    }

    [Test]
    public void CanUseCommitMessagesToBumpVersion_TagTakesPriority()
    {
        using var fixture = new EmptyRepositoryFixture();
        var repo = fixture.Repository;

        repo.MakeATaggedCommit("1.0.0");
        repo.MakeACommit("+semver:major");
        fixture.AssertFullSemver("2.0.0-1");

        repo.ApplyTag("1.1.0");
        fixture.AssertFullSemver("1.1.0");

        repo.MakeACommit();
        fixture.AssertFullSemver("1.1.1-1");
    }

    [Theory]
    [TestCase("", "NotAVersion", "2.0.0-1", "1.9.0", "1.9.0", "1.9.1-1")]
    [TestCase("", "1.5.0", "1.5.0", "1.9.0", "1.9.0", "1.9.1-1")]
    [TestCase("prefix", "1.5.0", "2.0.0-1", "1.9.0", "2.0.0-1", "2.0.0-2")]
    [TestCase("prefix", "1.5.0", "2.0.0-1", "prefix1.9.0", "1.9.0", "1.9.1-1")]
    [TestCase("prefix", "prefix1.5.0", "1.5.0", "1.9.0", "1.5.0", "1.5.1-1")]
    public void CanUseCommitMessagesToBumpVersion_TagsTakePriorityOnlyIfVersions(
        string tagPrefix,
        string firstTag,
        string expectedAfterFirstTag,
        string secondTag,
        string expectedAfterSecondTag,
        string expectedVersionAfterNewCommit)
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithTagPrefix(tagPrefix)
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        var repo = fixture.Repository;

        repo.MakeATaggedCommit($"{tagPrefix}1.0.0");
        repo.MakeACommit("+semver:major");
        fixture.AssertFullSemver("2.0.0-1", configuration);

        repo.ApplyTag(firstTag);
        fixture.AssertFullSemver(expectedAfterFirstTag, configuration);

        repo.ApplyTag(secondTag);
        fixture.AssertFullSemver(expectedAfterSecondTag, configuration);

        repo.MakeACommit();
        fixture.AssertFullSemver(expectedVersionAfterNewCommit, configuration);
    }

    [Theory]
    [TestCase("build: Cleaned up various things", "1.0.1")]
    [TestCase("build: Cleaned up various things\n\nSome descriptive text", "1.0.1")]
    [TestCase("build: Cleaned up various things\n\nSome descriptive text\nWith a second line", "1.0.1")]
    [TestCase("build(ref): Cleaned up various things", "1.0.1")]
    [TestCase("build(ref)!: Major update", "2.0.0")]
    [TestCase("chore: Cleaned up various things", "1.0.1")]
    [TestCase("ci: Cleaned up various things", "1.0.1")]
    [TestCase("docs: Cleaned up various things", "1.0.1")]
    [TestCase("fix: Cleaned up various things", "1.0.1")]
    [TestCase("perf: Cleaned up various things", "1.0.1")]
    [TestCase("refactor: Cleaned up various things", "1.0.1")]
    [TestCase("revert: Cleaned up various things", "1.0.1")]
    [TestCase("style: Cleaned up various things", "1.0.1")]
    [TestCase("test: Cleaned up various things", "1.0.1")]
    [TestCase("feat(ref): Simple feature", "1.1.0")]
    [TestCase("feat(ref)!: Major update", "2.0.0")]
    [TestCase("feat: Major update\n\nSome descriptive text\n\nBREAKING CHANGE: A reason", "2.0.0")]
    [TestCase("feat: Major update\n\nSome descriptive text\n\nBREAKING CHANGE Missing colon", "1.1.0")]
    [TestCase("feat: Major update\n\nForgot to describe the change\n\nBREAKING CHANGE: ", "1.1.0")]
    [TestCase("feat: Major update\n\nBREAKING CHANGE: A reason", "2.0.0")]
    [TestCase("feat: Major update\n\nSome descriptive text\nWith a second line\n\nBREAKING CHANGE: A reason", "2.0.0")]
    public void CanUseConventionalCommitsToBumpVersion(string commitMessage, string expectedVersion)
    {
        var configuration = GitFlowConfigurationBuilder.New
            // For future debugging of this regex: https://regex101.com/r/CRoBol/2
            .WithMajorVersionBumpMessage("^(build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test)(\\([\\w\\s-]*\\))?(!:|:.*\\n\\n((.+\\n)+\\n)?BREAKING CHANGE:\\s.+)")
            // For future debugging of this regex: https://regex101.com/r/9ccNam/3
            .WithMinorVersionBumpMessage("^(feat)(\\([\\w\\s-]*\\))?:")
            // For future debugging of this regex: https://regex101.com/r/oFpqxA/2
            .WithPatchVersionBumpMessage("^(build|chore|ci|docs|fix|perf|refactor|revert|style|test)(\\([\\w\\s-]*\\))?:")
            .WithVersioningMode(VersioningMode.Mainline)
            .WithBranch("develop", builder => builder.WithVersioningMode(null))
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.MakeATaggedCommit("1.0.0");

        fixture.Repository.MakeACommit(commitMessage);
        fixture.AssertFullSemver(expectedVersion, configuration);
    }

    [Test]
    public void CanUseCommitMessagesToBumpVersionBaseVersionTagIsAppliedToSameCommit()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeACommit("+semver:minor");
        fixture.AssertFullSemver("1.1.0-1");

        fixture.ApplyTag("2.0.0");

        fixture.Repository.MakeACommit("Hello");

        // Default bump is patch

        fixture.AssertFullSemver("2.0.1-1");
    }
}
