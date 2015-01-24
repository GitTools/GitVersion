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
            fixture.AssertFullSemver("0.1.1-PullRequest.1+2");
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
            fixture.Repository.CreateBranch("pull/2/merge").Checkout();
            fixture.Repository.Checkout("develop");
            fixture.Repository.Reset(ResetMode.Hard, "HEAD~1");
            fixture.Repository.Checkout("pull/2/merge");

            fixture.DumpGraph();
            fixture.AssertFullSemver("0.2.0-PullRequest.1+3");
        }
    }
}