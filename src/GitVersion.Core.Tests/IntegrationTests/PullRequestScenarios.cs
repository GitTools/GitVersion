using GitVersion.Configuration;
using GitVersion.Testing.Extensions;
using GitVersion.VersionCalculation;
using LibGit2Sharp;

namespace GitVersion.Tests.IntegrationTests;

[TestFixture]
public class PullRequestScenarios : TestBase
{
    private const string AzureDevOpsMergeMessage = "Merge pull request 2 from hotfix/v1.0.2 into support/v1.0.x";

    /// <summary>
    /// GitHubFlow - Pull requests (increment major on main and minor on feature)
    /// </summary>
    [Test]
    public void EnsurePullRequestWithIncrementMajorOnMainAndMinorOnFeatureBranch()
    {
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithBranch("main", b => b
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("feature", b => b
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1", configuration);

        fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-PullRequest2.2", configuration);

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-2", configuration);
    }

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
        fixture.Repository.MakeACommit("one");
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("release/2.0.0"));
        fixture.Repository.MakeACommit("two");
        fixture.Repository.MakeACommit("three");

        fixture.Repository.CreatePullRequestRef("release/2.0.0", MainBranch, normalise: true);

        fixture.AssertFullSemver("2.0.0-PullRequest2.4");
    }

    [Test]
    public void PullRequestInheritsSupportConfigurationWhenLocalAndRemoteBranchesExist()
    {
        var configuration = GetHotfixToSupportConfiguration();

        using var remote = new EmptyRepositoryFixture("master");
        remote.Repository.MakeATaggedCommit("v1.0.0");
        remote.BranchTo("support/v1.0.x");
        remote.Repository.MakeATaggedCommit("v1.0.1");
        remote.Checkout("master");
        remote.Repository.MakeACommit("Merged PR 1: new feature");
        remote.Checkout("support/v1.0.x");
        remote.BranchTo("hotfix/v1.0.2");
        remote.Repository.MakeACommit();
        remote.Checkout("support/v1.0.x");
        remote.BranchTo("pull/2/merge");
        remote.Repository.MergeNoFF("hotfix/v1.0.2", AzureDevOpsMergeMessage);

        using var local = remote.CloneRepository();
        CopyRemoteBranchesToHeads(local.Repository);
        local.Checkout("pull/2/merge");

        local.AssertFullSemver("1.0.2-alpha-pr2.2", configuration);
    }

    [Test]
    public void PullRequestWithoutRemoteBranchesKeepsHotfixToSupportVersion()
    {
        var configuration = GetHotfixToSupportConfiguration();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("v1.0.0");
        fixture.BranchTo("support/v1.0.x");
        fixture.BranchTo("hotfix/v1.0.1");
        fixture.Repository.MakeACommit();
        fixture.Checkout("support/v1.0.x");
        fixture.BranchTo("pull/2/merge");
        fixture.Repository.MergeNoFF("hotfix/v1.0.1", "Merge pull request 2 from hotfix/v1.0.1 into support/v1.0.x");

        fixture.AssertFullSemver("1.0.1-alpha-pr2.2", configuration);
    }

    private static IGitVersionConfiguration GetHotfixToSupportConfiguration() => GitFlowConfigurationBuilder.New
        .WithDeploymentMode(DeploymentMode.ContinuousDeployment)
        .WithBranch("main", b => b
            .WithIncrement(IncrementStrategy.Minor)
            .WithLabel("beta")
        ).WithBranch("pull-request", b => b
            .WithIncrement(IncrementStrategy.Inherit)
            .WithLabel("alpha-pr{Number}")
        ).WithBranch("support", b => b
            .WithIncrement(IncrementStrategy.Patch)
            .WithLabel("beta")
        ).Build();

    private static void CopyRemoteBranchesToHeads(Repository repository)
    {
        foreach (var branch in repository.Branches.Where(branch => branch.IsRemote).ToArray())
        {
            var localName = branch.FriendlyName.Replace($"{branch.RemoteName}/", string.Empty);
            if (repository.Branches[localName] is null)
            {
                repository.CreateBranch(localName, branch.Tip);
            }
        }
    }
}
