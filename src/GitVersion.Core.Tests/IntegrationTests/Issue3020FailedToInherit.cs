using GitTools.Testing;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using LibGit2Sharp;
using NUnit.Framework;

namespace GitVersion.Core.Tests.IntegrationTests;

public class Issue3020FailedToInherit : TestBase
{
    private readonly Config config = new()
    {
        VersioningMode = VersioningMode.ContinuousDeployment,
        Branches = new Dictionary<string, BranchConfig>
        {
            { MainBranch, new BranchConfig { Tag = "beta", Increment = IncrementStrategy.Minor } },
            { "pull-request", new BranchConfig { Tag = "alpha-pr", Increment = IncrementStrategy.Inherit } }, // Inherit is indeed the default
            { "support", new BranchConfig { Tag = "beta" } }
        }
    };

    [Test]
    public void MergingHotFixIntoSupportLocalOnly()
    {
        using var fixture = PrepareLocalRepoForHotFix();

        fixture.AssertFullSemver("1.0.2-alpha-pr0002.2", config);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void MergingHotFixIntoSupportWithRemote()
    {
        using var fixture = PrepareLocalRepoForHotFix();

        using var local = fixture.CloneRepository();
        local.Checkout("origin/hotfix/v1.0.2");
        local.BranchTo("hotfix/v1.0.2", "hotfix");
        local.Checkout("origin/support/v1.0.x");
        local.BranchTo("support/v1.0.x", "support");
        local.Checkout($"origin/{MainBranch}");
        local.BranchTo(MainBranch, MainBranch);
        local.Checkout("pull/2/merge");
        
        local.AssertFullSemver("1.0.2-alpha-pr0002.2", config);

        local.Repository.DumpGraph();
    }

    private static RepositoryFixtureBase PrepareLocalRepoForHotFix()
    {
        var fixture = new EmptyRepositoryFixture();

        fixture.Repository.MakeACommit("First commit");
        fixture.Repository.ApplyTag("v1.0.0", new Signature("Michael Denny", "micdenny@gmail.com", DateTimeOffset.Now), "Release v1.0.0");

        fixture.Repository.MakeACommit("Merged PR 1: new feature");

        fixture.Checkout("tags/v1.0.0");
        fixture.BranchTo("support/v1.0.x", "support");
        fixture.Repository.MakeACommit("hotfix 1");
        fixture.Repository.ApplyTag("v1.0.1", new Signature("Michael Denny", "micdenny@gmail.com", DateTimeOffset.Now), "hotfix 1");

        fixture.BranchTo("hotfix/v1.0.2", "hotfix");
        fixture.Repository.MakeACommit("hotfix 2");

        fixture.Checkout("support/v1.0.x");
        fixture.BranchTo("pull/2/merge", "pr-merge");
        fixture.MergeNoFF("hotfix/v1.0.2");

        return fixture;
    }

    [Test]
    public void MergingFeatureIntoMainLocalOnly()
    {
        using var fixture = PrepareLocalRepoForFeature();

        fixture.AssertFullSemver("1.1.0-alpha-pr0002.3", config);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void MergingFeatureIntoMainWithRemote()
    {
        using var fixture = PrepareLocalRepoForFeature();

        using var local = fixture.CloneRepository();
        local.Checkout("origin/feature/my-incredible-feature");
        local.BranchTo("feature/my-incredible-feature", "feature");
        local.Checkout($"origin/{MainBranch}");
        local.BranchTo(MainBranch, MainBranch);
        local.Checkout("pull/2/merge");

        local.AssertFullSemver("1.1.0-alpha-pr0002.3", config);

        local.Repository.DumpGraph();
    }

    private static RepositoryFixtureBase PrepareLocalRepoForFeature()
    {
        var fixture = new EmptyRepositoryFixture();

        fixture.Repository.MakeACommit("First commit");
        fixture.Repository.ApplyTag("v1.0.0", new Signature("Michael Denny", "micdenny@gmail.com", DateTimeOffset.Now), "Release v1.0.0");

        fixture.Repository.MakeACommit("Merged PR 1: new feature");
        
        fixture.BranchTo("feature/my-incredible-feature", "feature");
        fixture.Repository.MakeACommit("incredible feature");

        fixture.Checkout(MainBranch);
        fixture.BranchTo("pull/2/merge", "pr-merge");
        fixture.MergeNoFF("feature/my-incredible-feature");

        return fixture;
    }
}
