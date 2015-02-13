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

            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("feature/Foo");
            fixture.Repository.CreateBranch("pull/2/merge").Checkout();
            fixture.Repository.Checkout("master");
            fixture.Repository.Reset(ResetMode.Hard, "HEAD~1");
            fixture.Repository.Checkout("pull/2/merge");

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

            fixture.Repository.Checkout("develop");
            fixture.Repository.MergeNoFF("feature/Foo");
            fixture.Repository.CreateBranch("pull/44/merge").Checkout();
            fixture.Repository.Checkout("develop");
            fixture.Repository.Reset(ResetMode.Hard, "HEAD~1");
            fixture.Repository.Checkout("pull/44/merge");

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

            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("feature/Foo");
            fixture.Repository.CreateBranch("pull/2/merge").Checkout();
            fixture.Repository.Checkout("master");
            fixture.Repository.Reset(ResetMode.Hard, "HEAD~1");
            fixture.Repository.Checkout("pull/2/merge");
            // If we delete the branch, it is effectively the same as remote PR
            fixture.Repository.Branches.Remove("feature/Foo");

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

            fixture.Repository.Checkout("develop");
            fixture.Repository.MergeNoFF("feature/Foo");
            fixture.Repository.CreateBranch("pull/2/merge").Checkout();
            fixture.Repository.Checkout("develop");
            fixture.Repository.Reset(ResetMode.Hard, "HEAD~1");
            fixture.Repository.Checkout("pull/2/merge");
            // If we delete the branch, it is effectively the same as remote PR
            fixture.Repository.Branches.Remove("feature/Foo");
            
            fixture.AssertFullSemver("0.2.0-PullRequest.2+3");
        }
    }
}