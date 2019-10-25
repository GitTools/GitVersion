using GitTools.Testing;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests.IntegrationTests
{
    [TestFixture]
    public class OtherBranchScenarios : TestBase
    {
        [Test]
        public void CanTakeVersionFromReleaseBranch()
        {
            using var fixture = new EmptyRepositoryFixture();
            const string taggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(taggedVersion);
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("release/beta-2.0.0");
            Commands.Checkout(fixture.Repository, "release/beta-2.0.0");

            fixture.AssertFullSemver("2.0.0-beta.1+0");
        }

        [Test]
        public void BranchesWithIllegalCharsShouldNotBeUsedInVersionNames()
        {
            using var fixture = new EmptyRepositoryFixture();
            const string taggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(taggedVersion);
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("issue/m/github-569");
            Commands.Checkout(fixture.Repository, "issue/m/github-569");

            fixture.AssertFullSemver("1.0.4-issue-m-github-569.1+5");
        }

        [Test]
        public void ShouldNotGetVersionFromFeatureBranchIfNotMerged()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.0-unstable.0"); // initial commit in master

            fixture.Repository.CreateBranch("feature");
            Commands.Checkout(fixture.Repository, "feature");
            fixture.Repository.MakeATaggedCommit("1.0.1-feature.1");

            Commands.Checkout(fixture.Repository, "master");
            fixture.Repository.CreateBranch("develop");
            Commands.Checkout(fixture.Repository, "develop");
            fixture.Repository.MakeACommit();

            var version = fixture.GetVersion();
            version.SemVer.ShouldBe("1.0.0-alpha.1");
        }
    }
}
