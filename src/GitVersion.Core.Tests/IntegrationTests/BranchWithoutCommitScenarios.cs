using GitVersion.Core.Tests.Helpers;
using LibGit2Sharp;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class BranchWithoutCommitScenarios : TestBase
{
    [Test]
    public void CanTakeVersionFromReleaseBranch()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.3");
        var commit = fixture.Repository.MakeACommit();
        fixture.Repository.CreateBranch("release-4.0.123");
        fixture.Checkout(commit.Sha);

        fixture.AssertFullSemver("4.0.123-beta.1+1", null, fixture.Repository, commit.Sha, false, "release-4.0.123");
    }

    [TestCase("0.1.0-alpha.1", "1.0.0-beta.1+2")]
    [TestCase("1.0.0-alpha.1", "1.0.0-beta.1+2")]
    [TestCase("1.0.1-alpha.1", "1.0.1-beta.1+2")]
    public void BranchVersionHavePrecedenceOverTagVersionIfVersionGreaterThanTag(string tag, string expected)
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();

        fixture.BranchTo("develop");
        fixture.MakeATaggedCommit(tag); // simulate merge from feature branch

        fixture.BranchTo("release/1.0.0");

        fixture.AssertFullSemver(expected);
    }
}
