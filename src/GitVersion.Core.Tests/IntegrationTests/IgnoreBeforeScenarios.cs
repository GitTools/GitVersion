using GitTools.Testing;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Model.Configuration;
using LibGit2Sharp;
using NUnit.Framework;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class IgnoreBeforeScenarios : TestBase
{
    [Test]
    public void ShouldFallbackToBaseVersionWhenAllCommitsAreIgnored()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.3");
        fixture.Repository.CreateBranch("develop");
        fixture.Repository.MakeCommits(1);
        var commit = fixture.Repository.MakeACommit("+semver:major");

        var config = new ConfigurationBuilder()
            .Add(new Config
            {
                Ignore = new IgnoreConfig
                {
                    ShAs = new[] { commit.Sha }
                }
            }).Build();

        fixture.AssertFullSemver("1.0.4+2", config);
    }

    [Test]
    public void ShouldFallbackToBaseVersionWhenAllMergeCommitsAreIgnored()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.3");
        fixture.Repository.CreateBranch("develop");
        fixture.Repository.MakeCommits(1);

        fixture.Repository.CreateBranch("feature/feature1");
        fixture.Checkout("feature/feature1");
        var commit = fixture.Repository.MakeACommit("+semver:major");
        fixture.Checkout("develop");
        fixture.Repository.MergeNoFF("feature/feature1", Generate.SignatureNow());

        var config = new ConfigurationBuilder()
            .Add(new Config
            {
                Ignore = new IgnoreConfig
                {
                    ShAs = new[] { commit.Sha }
                }
            }).Build();

        fixture.AssertFullSemver("1.1.0-alpha.3", config);
    }
}
