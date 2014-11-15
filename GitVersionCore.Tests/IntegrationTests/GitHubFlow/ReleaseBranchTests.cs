using GitVersion.Configuration;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class ReleaseBranchTests
{
    [Test]
    public void CanTakeVersionFromReleaseBranch()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");

            fixture.AssertFullSemver("2.0.0-beta.1+5");
        }
    }

    [Test]
    public void CanTakeVersionFromReleaseBranchWithTagOverriden()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config { ReleaseBranchTag = "rc" }))
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");

            fixture.AssertFullSemver("2.0.0-rc.1+5");
        }
    }

    [Test]
    public void WhenReleaseBranchIsMergedIntoMasterVersionIsTakenWithIt()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(1);
            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0", Constants.SignatureNow());

            fixture.AssertFullSemver("2.0.0+6");
        }
    }
    [Test]
    public void WhenReleaseBranchIsMergedIntoMasterHighestVersionIsTakenWithIt()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(1);

            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0", Constants.SignatureNow());

            fixture.Repository.CreateBranch("release-1.0.0");
            fixture.Repository.Checkout("release-1.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release-1.0.0", Constants.SignatureNow());

            fixture.AssertFullSemver("2.0.0+11");
        }
    }

    [Test]
    public void WhenMergingReleaseBackToDevShouldNotResetBetaVersion()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            const string TaggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.Checkout("develop");

            fixture.Repository.MakeCommits(1);

            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(1);

            fixture.AssertFullSemver("2.0.0-beta.1+1");

            //tag it to bump to beta 2
            fixture.Repository.ApplyTag("2.0.0-beta1");

            fixture.Repository.MakeCommits(1);

            fixture.AssertFullSemver("2.0.0-beta.2+0");

            //merge down to develop
            fixture.Repository.Checkout("develop");
            fixture.Repository.MergeNoFF("release-2.0.0", Constants.SignatureNow());

            //but keep working on the release
            fixture.Repository.Checkout("release-2.0.0");

            fixture.AssertFullSemver("2.0.0-beta.2+0");
        }
    }
}