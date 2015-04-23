using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class PullRequestScenarios
{
    [Test]
    public void CanCalculatePullRequestChanges()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("0.1.0");
            fixture.Repository.CreateBranch("feature/Foo").Checkout();
            fixture.Repository.MakeACommit();
            
            fixture.Repository.CreatePullRequest("feature/Foo", "master");

            fixture.DumpGraph();
            fixture.AssertFullSemver("0.1.1-PullRequest.2+2");
        }
    }

    [Test]
    public void CanCalculatePullRequestChangesInheritingConfig()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("0.1.0");
            fixture.Repository.CreateBranch("develop").Checkout();
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("feature/Foo").Checkout();
            fixture.Repository.MakeACommit();

            fixture.Repository.CreatePullRequest("feature/Foo", "develop", 44);

            fixture.DumpGraph();
            fixture.AssertFullSemver("0.2.0-PullRequest.44+3");
        }
    }

    [Test]
    public void CanCalculatePullRequestChangesFromRemoteRepo()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("0.1.0");
            fixture.Repository.CreateBranch("feature/Foo").Checkout();
            fixture.Repository.MakeACommit();


            fixture.Repository.CreatePullRequest("feature/Foo", "master");

            fixture.DumpGraph();
            fixture.AssertFullSemver("0.1.1-PullRequest.2+2");
        }
    }

    [Test]
    public void CanCalculatePullRequestChangesInheritingConfigFromRemoteRepo()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("0.1.0");
            fixture.Repository.CreateBranch("develop").Checkout();
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("feature/Foo").Checkout();
            fixture.Repository.MakeACommit();

            fixture.Repository.CreatePullRequest("feature/Foo", "develop");

            fixture.AssertFullSemver("0.2.0-PullRequest.2+3");
        }
    }

    [Test]
    public void CanCalculatePullRequestChangesWhenThereAreMultipleMergeCandidates()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("0.1.0");
            fixture.Repository.CreateBranch("develop").Checkout();
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("copyOfDevelop").Checkout();
            fixture.Repository.CreateBranch("feature/Foo").Checkout();
            fixture.Repository.MakeACommit();

            fixture.Repository.CreatePullRequest("feature/Foo", "develop");

            fixture.AssertFullSemver("0.2.0-PullRequest.2+3");
        }
    }

    [Test]
    public void CalculatesCorrectVersionAfterReleaseBranchMergedToMaster()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("release/2.0.0").Checkout();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();

            fixture.Repository.CreatePullRequest("release/2.0.0", "master");

            fixture.AssertFullSemver("2.0.0-PullRequest.2+0");
        }
    }
}