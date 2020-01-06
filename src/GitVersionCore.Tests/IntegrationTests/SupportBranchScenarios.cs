using GitTools.Testing;
using LibGit2Sharp;
using NUnit.Framework;

namespace GitVersionCore.Tests.IntegrationTests
{
    [TestFixture]
    public class SupportBranchScenarios : TestBase
    {
        [Test]
        public void SupportIsCalculatedCorrectly()
        {
            using var fixture = new EmptyRepositoryFixture();
            // Start at 1.0.0
            fixture.Repository.MakeACommit();
            fixture.Repository.ApplyTag("1.1.0");

            // Create 2.0.0 release
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("release-2.0.0"));
            fixture.Repository.MakeCommits(2);

            // Merge into develop and master
            Commands.Checkout(fixture.Repository, "master");
            fixture.Repository.MergeNoFF("release-2.0.0");
            fixture.Repository.ApplyTag("2.0.0");
            fixture.AssertFullSemver("2.0.0");

            // Now lets support 1.x release
            Commands.Checkout(fixture.Repository, "1.1.0");
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("support/1.0.0"));
            fixture.AssertFullSemver("1.1.0");

            // Create release branch from support branch
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("release/1.2.0"));
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.2.0-beta.1+1");

            // Create 1.2.0 release
            Commands.Checkout(fixture.Repository, "support/1.0.0");
            fixture.Repository.MergeNoFF("release/1.2.0");
            fixture.AssertFullSemver("1.2.0+0");
            fixture.Repository.ApplyTag("1.2.0");

            // Create 1.2.1 hotfix
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("hotfix/1.2.1"));
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.2.1-beta.1+1");
            Commands.Checkout(fixture.Repository, "support/1.0.0");
            fixture.Repository.MergeNoFF("hotfix/1.2.1");
            fixture.AssertFullSemver("1.2.1+2");
        }

        [Test]
        public void WhenSupportIsBranchedAndTaggedFromAnotherSupportEnsureNewMinorIsUsed()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("Support-1.2.0");
            Commands.Checkout(fixture.Repository, "Support-1.2.0");
            fixture.Repository.MakeACommit();
            fixture.Repository.ApplyTag("1.2.0");

            fixture.Repository.CreateBranch("Support-1.3.0");
            Commands.Checkout(fixture.Repository, "Support-1.3.0");
            fixture.Repository.ApplyTag("1.3.0");

            //Move On
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();

            fixture.AssertFullSemver("1.3.1+2");
        }
    }
}