using GitTools;
using GitTools.Testing;
using GitVersionCore.Tests;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class PullRequestScenarios
{
    [Test]
    public void CanCalculatePullRequestChanges()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("0.1.0");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("feature/Foo"));
            fixture.Repository.MakeACommit();

            fixture.Repository.CreatePullRequestRef("feature/Foo", "master", normalise: true);

            fixture.Repository.DumpGraph();
            fixture.AssertFullSemver("0.1.1-PullRequest.2+2");
        }
    }

    [Test]
    public void CanCalculatePullRequestChangesInheritingConfig()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("0.1.0");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
            fixture.Repository.MakeACommit();
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("feature/Foo"));
            fixture.Repository.MakeACommit();

            fixture.Repository.CreatePullRequestRef("feature/Foo", "develop", 44, normalise: true);

            fixture.Repository.DumpGraph();
            fixture.AssertFullSemver("0.2.0-PullRequest.44+3");
        }
    }

    [Test]
    public void CanCalculatePullRequestChangesFromRemoteRepo()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("0.1.0");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("feature/Foo"));
            fixture.Repository.MakeACommit();


            fixture.Repository.CreatePullRequestRef("feature/Foo", "master", normalise: true);

            fixture.Repository.DumpGraph();
            fixture.AssertFullSemver("0.1.1-PullRequest.2+2");
        }
    }

    [Test]
    public void CanCalculatePullRequestChangesInheritingConfigFromRemoteRepo()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("0.1.0");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
            fixture.Repository.MakeACommit();
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("feature/Foo"));
            fixture.Repository.MakeACommit();

            fixture.Repository.CreatePullRequestRef("feature/Foo", "develop", normalise: true);

            fixture.AssertFullSemver("0.2.0-PullRequest.2+3");
        }
    }

    [Test]
    public void CanCalculatePullRequestChangesWhenThereAreMultipleMergeCandidates()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("0.1.0");
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
            fixture.Repository.MakeACommit();
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("copyOfDevelop"));
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("feature/Foo"));
            fixture.Repository.MakeACommit();

            fixture.Repository.CreatePullRequestRef("feature/Foo", "develop", normalise: true);

            fixture.AssertFullSemver("0.2.0-PullRequest.2+3");
        }
    }

    [Test]
    public void CalculatesCorrectVersionAfterReleaseBranchMergedToMaster()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.MakeACommit();
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("release/2.0.0"));
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();

            fixture.Repository.CreatePullRequestRef("release/2.0.0", "master", normalise: true);

            fixture.AssertFullSemver("2.0.0-PullRequest.2+0");
        }
    }
}