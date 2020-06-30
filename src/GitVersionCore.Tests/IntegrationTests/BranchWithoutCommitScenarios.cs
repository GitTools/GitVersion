using GitTools.Testing;
using GitVersionCore.Tests.Helpers;
using LibGit2Sharp;
using NUnit.Framework;

namespace GitVersionCore.Tests.IntegrationTests
{
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

            fixture.AssertFullSemver("4.0.123-beta.1+0", null, fixture.Repository, commit.Sha, onlyTrackedBranches: false, targetBranch: "release-4.0.123");
        }

        [Test]
        public void BranchVersionHavePrecedenceOverTagVersionIfVersionGreaterThanTag()
        {
            using var fixture = new EmptyRepositoryFixture();

            fixture.Repository.MakeACommit();

            fixture.Repository.CreateBranch("develop");
            fixture.Checkout("develop");
            fixture.MakeATaggedCommit("0.1.0-alpha.1"); // simulate merge from feature branch

            fixture.Repository.CreateBranch("release/1.0");
            fixture.Checkout("release/1.0");

            fixture.AssertFullSemver("1.0.0-beta.1+0");
        }
    }
}
