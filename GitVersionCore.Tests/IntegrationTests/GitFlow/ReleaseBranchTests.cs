using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class GitFlowReleaseBranchTests
{
    [Test]
    public void CanTakeVersionFromReleaseBranch()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.CreateBranch("develop");
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
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");

            fixture.AssertFullSemver("2.0.0-rc.1+5");
        }
    }

    [Test]
    public void CanHandleReleaseBranchWithStability()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("release-2.0.0-Final");
            fixture.Repository.Checkout("release-2.0.0-Final");

            fixture.AssertFullSemver("2.0.0-beta.1+5");
        }
    }

    [Test]
    public void WhenReleaseBranchIsMergedIntoMasterVersionIsTakenWithIt()
    {
        DoReleaseBranchIsMergedIntoMasterVersion("release-2.0.0");
    }
    [Test]
    public void WhenReleaseBranchWithUpperCaseIsMergedIntoMasterVersionIsTakenWithIt()
    {
        DoReleaseBranchIsMergedIntoMasterVersion("Release-2.0.0");
    }
    static void DoReleaseBranchIsMergedIntoMasterVersion(string releaseBranchName)
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.MakeCommits(1);
            fixture.Repository.CreateBranch(releaseBranchName);
            fixture.Repository.Checkout(releaseBranchName);
            fixture.Repository.MakeCommits(4);
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF(releaseBranchName, Constants.SignatureNow());

            // TODO For GitHubFlow this is 2.0.0+6, why is it different
            fixture.AssertFullSemver("2.0.0");
        }
    }

    // TODO This test fails for GitFlow, it needs to be fixed (although in reality a support branch should be used)
    [Test, Ignore]
    public void WhenReleaseBranchIsMergedIntoMasterHighestVersionIsTakenWithIt()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.MakeCommits(1);

            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0", Constants.SignatureNow());
            fixture.Repository.Checkout("develop");
            fixture.Repository.MergeNoFF("release-2.0.0", Constants.SignatureNow());

            fixture.Repository.CreateBranch("release-1.0.0");
            fixture.Repository.Checkout("release-1.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release-1.0.0", Constants.SignatureNow());
            fixture.Repository.Checkout("develop");
            fixture.Repository.MergeNoFF("release-1.0.0", Constants.SignatureNow());

            fixture.AssertFullSemver("2.0.0+11");
        }
    }
}