using GitTools.Testing;
using GitVersion.Core.Tests.Helpers;
using LibGit2Sharp;
using NUnit.Framework;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class PullRequestScenarios : TestBase
{
    [Test]
    public void CanCalculatePullRequestChanges()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("0.1.0");
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("feature/Foo"));
        fixture.Repository.MakeACommit();

        fixture.Repository.CreatePullRequestRef("feature/Foo", MainBranch, normalise: true);

        fixture.Repository.DumpGraph();
        fixture.AssertFullSemver("0.1.1-PullRequest2.2");
    }

    [Test]
    public void CanCalculatePullRequestChangesInheritingConfig()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("0.1.0");
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
        fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("feature/Foo"));
        fixture.Repository.MakeACommit();

        fixture.Repository.CreatePullRequestRef("feature/Foo", "develop", 44, true);

        fixture.Repository.DumpGraph();
        fixture.AssertFullSemver("0.2.0-PullRequest44.3");
    }

    [Test]
    public void CanCalculatePullRequestChangesFromRemoteRepo()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("0.1.0");
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("feature/Foo"));
        fixture.Repository.MakeACommit();


        fixture.Repository.CreatePullRequestRef("feature/Foo", MainBranch, normalise: true);

        fixture.Repository.DumpGraph();
        fixture.AssertFullSemver("0.1.1-PullRequest2.2");
    }

    [Test]
    public void CanCalculatePullRequestChangesInheritingConfigFromRemoteRepo()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("0.1.0");
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
        fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("feature/Foo"));
        fixture.Repository.MakeACommit();

        fixture.Repository.CreatePullRequestRef("feature/Foo", "develop", normalise: true);

        fixture.AssertFullSemver("0.2.0-PullRequest2.3");
    }

    [Test]
    public void CanCalculatePullRequestChangesWhenThereAreMultipleMergeCandidates()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("0.1.0");
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
        fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("copyOfDevelop"));
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("feature/Foo"));
        fixture.Repository.MakeACommit();

        fixture.Repository.CreatePullRequestRef("feature/Foo", "develop", normalise: true);

        fixture.AssertFullSemver("0.2.0-PullRequest2.3");
    }

    [Test]
    public void CalculatesCorrectVersionAfterReleaseBranchMergedToMain()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeACommit(); // one
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("release/2.0.0"));
        fixture.Repository.MakeACommit(); // two
        fixture.Repository.MakeACommit(); // three

        fixture.Repository.CreatePullRequestRef("release/2.0.0", MainBranch, normalise: true);

        fixture.AssertFullSemver("2.0.0-PullRequest2.3");
    }
}
