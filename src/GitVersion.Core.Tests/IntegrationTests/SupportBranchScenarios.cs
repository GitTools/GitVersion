using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using LibGit2Sharp;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class SupportBranchScenarios : TestBase
{
    [Test]
    public void SupportIsCalculatedCorrectly()
    {
        using var fixture = new EmptyRepositoryFixture();
        // Start at 1.0.0
        fixture.MakeACommit();
        fixture.ApplyTag("1.1.0");

        // Create 2.0.0 release
        fixture.BranchTo("release-2.0.0");
        fixture.Repository.MakeCommits(2);

        // Merge into develop and main
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("release-2.0.0");
        fixture.ApplyTag("2.0.0");
        fixture.AssertFullSemver("2.0.0");

        // Now lets support 1.x release
        fixture.Checkout("1.1.0");
        fixture.BranchTo("support/1.0.0");
        fixture.AssertFullSemver("1.1.0");

        // Create release branch from support branch
        fixture.BranchTo("release/1.2.0");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.0-beta.1+1");

        // Create 1.2.0 release
        Commands.Checkout(fixture.Repository, "support/1.0.0");
        fixture.Repository.MergeNoFF("release/1.2.0");
        fixture.AssertFullSemver("1.2.0-2");
        fixture.Repository.ApplyTag("1.2.0");

        // Create 1.2.1 hotfix
        fixture.BranchTo("hotfix/1.2.1");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.1-beta.1+1");
        fixture.Checkout("support/1.0.0");
        fixture.MergeNoFF("hotfix/1.2.1");
        fixture.AssertFullSemver("1.2.1-2");
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

        fixture.AssertFullSemver("1.3.1-2");
    }

    [Test]
    public void WhenSupportIsBranchedFromMainWithSpecificTag()
    {
        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();
        fixture.AssertFullSemver("0.0.1-1", configuration);

        fixture.ApplyTag("1.4.0-rc");
        fixture.MakeACommit();
        fixture.BranchTo("support/1");

        fixture.AssertFullSemver("1.4.0-2", configuration);
    }

    [Test]
    public void WhenSupportIsBranchedFromMainWithSpecificTagOnCommit()
    {
        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();
        fixture.AssertFullSemver("0.0.1-1", configuration);

        fixture.ApplyTag("1.4.0-rc");
        fixture.BranchTo("support/1");

        fixture.AssertFullSemver("1.4.0-1", configuration);
    }
}
