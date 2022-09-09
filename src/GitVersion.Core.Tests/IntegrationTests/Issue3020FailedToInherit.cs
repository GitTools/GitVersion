using GitTools.Testing;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests.IntegrationTests;

public class Issue3020FailedToInherit : TestBase
{
    private readonly Config config = new()
    {
        VersioningMode = VersioningMode.ContinuousDeployment,
        Branches = new Dictionary<string, BranchConfig>
        {
            { MainBranch, new BranchConfig { Tag = "beta", Increment = IncrementStrategy.Minor } },
            { "pull-request", new BranchConfig { Tag = "alpha-pr" } },
            { "support", new BranchConfig { Tag = "beta" } }
        }
    };

    [Test]
    public void LocalOnly()
    {
        var fixture = PrepareLocalRepo();

        fixture.AssertFullSemver("1.0.2-alpha-pr0002.2", config);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void WithRemote()
    {
        var fixture = PrepareLocalRepo();

        var local = fixture.CloneRepository();
        local.Checkout("origin/hotfix/v1.0.2");
        local.BranchTo("hotfix/v1.0.2", "hotfix");
        local.Checkout("origin/support/v1.0.x");
        local.BranchTo("support/v1.0.x", "support");
        local.Checkout("origin/main");
        local.BranchTo("main", "main");
        local.Checkout("pull/2/merge");

        local.AssertFullSemver("1.0.2-alpha-pr0002.2", config);

        local.Repository.DumpGraph();
    }

    private static RepositoryFixtureBase PrepareLocalRepo()
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
}
