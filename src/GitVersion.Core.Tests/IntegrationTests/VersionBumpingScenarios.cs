using GitTools.Testing;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using NUnit.Framework;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class VersionBumpingScenarios : TestBase
{
    [Test]
    public void AppliedPrereleaseTagCausesBump()
    {
        var configuration = new Config
        {
            Branches =
            {
                {
                    MainBranch, new BranchConfig
                    {
                        Tag = "pre",
                        SourceBranches = new HashSet<string>()
                    }
                }
            }
        };
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeATaggedCommit("1.0.0-pre.1");
        fixture.Repository.MakeACommit();

        fixture.AssertFullSemver("1.0.0-pre.2+1", configuration);
    }

    [Test]
    public void CanUseCommitMessagesToBumpVersion()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeACommit("+semver:minor");

        fixture.AssertFullSemver("1.1.0+1");

        fixture.Repository.MakeACommit("+semver:major");

        fixture.AssertFullSemver("2.0.0+2");
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
        var configuration = new Config
        {
            VersioningMode = VersioningMode.Mainline,

            // For future debugging of this regex: https://regex101.com/r/CRoBol/2
            MajorVersionBumpMessage = "^(build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test)(\\([\\w\\s-]*\\))?(!:|:.*\\n\\n((.+\\n)+\\n)?BREAKING CHANGE:\\s.+)",

            // For future debugging of this regex: https://regex101.com/r/9ccNam/3
            MinorVersionBumpMessage = "^(feat)(\\([\\w\\s-]*\\))?:",

            // For future debugging of this regex: https://regex101.com/r/oFpqxA/2
            PatchVersionBumpMessage = "^(build|chore|ci|docs|fix|perf|refactor|revert|style|test)(\\([\\w\\s-]*\\))?:"
        };
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
        fixture.AssertFullSemver("1.1.0+1");

        fixture.ApplyTag("2.0.0");

        fixture.Repository.MakeACommit("Hello");

        // Default bump is patch

        fixture.AssertFullSemver("2.0.1+1");
    }
}
