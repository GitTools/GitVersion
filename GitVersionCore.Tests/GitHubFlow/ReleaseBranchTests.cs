using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class ReleaseBranchTests
{
    [Test]
    public void CanTakeVersionFromReleaseBranch()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout("release-2.0.0");

            fixture.AssertFullSemver("2.0.0-beta.1+5");
        }
    }

    [Test]
    public void WhenReleaseBranchIsMergedIntoMasterVersionIsTakenWithIt()
    {
        using (var fixture = new EmptyRepositoryFixture())
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
        using (var fixture = new EmptyRepositoryFixture())
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
}