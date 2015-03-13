using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class SupportBranchScenarios
{
    [Test]
    public void SupportIsCalculatedCorrectly()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            // Start at 1.0.0
            fixture.Repository.MakeACommit();
            fixture.Repository.ApplyTag("1.1.0");

            // Create 2.0.0 release
            fixture.Repository.CreateBranch("release-2.0.0").Checkout();
            fixture.Repository.MakeCommits(2);

            // Merge into develop and master
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0");
            fixture.Repository.ApplyTag("2.0.0");
            fixture.AssertFullSemver("2.0.0");

            // Now lets support 1.x release
            fixture.Repository.Checkout("1.1.0");
            fixture.Repository.CreateBranch("support/1.0.0").Checkout();
            fixture.AssertFullSemver("1.1.0");

            // Create release branch from support branch
            fixture.Repository.CreateBranch("release/1.2.0").Checkout();
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.2.0-beta.1+1");

            // Create 1.2.0 release
            fixture.Repository.Checkout("support/1.0.0");
            fixture.Repository.MergeNoFF("release/1.2.0");
            fixture.AssertFullSemver("1.2.0+0");
            fixture.Repository.ApplyTag("1.2.0");

            // Create 1.2.1 hotfix
            fixture.Repository.CreateBranch("hotfix/1.2.1").Checkout();
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.2.1-beta.1+1");
            fixture.Repository.Checkout("support/1.0.0");
            fixture.Repository.MergeNoFF("hotfix/1.2.1");
            fixture.AssertFullSemver("1.2.1+0");
        }
    }

    [Test]
    public void WhenSupportIsBranchedAndTaggedFromAnotherSupportEnsureNewMinorIsUsed()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("Support-1.2.0");
            fixture.Repository.Checkout("Support-1.2.0");
            fixture.Repository.MakeACommit();
            fixture.Repository.ApplyTag("1.2.0");

            fixture.Repository.CreateBranch("Support-1.3.0");
            fixture.Repository.Checkout("Support-1.3.0");
            fixture.Repository.ApplyTag("1.3.0");

            //Move On
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();

            fixture.AssertFullSemver("1.3.0+2");
        }
    }
}