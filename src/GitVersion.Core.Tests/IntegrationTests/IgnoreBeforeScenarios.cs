using GitTools.Testing;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Model.Configuration;
using LibGit2Sharp;
using NSubstitute;
using NUnit.Framework;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class IgnoreBeforeScenarios : TestBase
{
    [Test]
    public void ShouldFallbackToBaseVersionWhenAllCommitsAreIgnored()
    {
        using var fixture = new EmptyRepositoryFixture();
        var objectId = fixture.Repository.MakeACommit();
        var commit = Substitute.For<ICommit>();
        commit.Sha.Returns(objectId.Sha);
        commit.When.Returns(DateTimeOffset.Now);

        var config = new ConfigurationBuilder()
            .Add(new Config
            {
                Ignore = new IgnoreConfig
                {
                    Before = commit.When.AddMinutes(1)
                }
            }).Build();

        fixture.AssertFullSemver("0.1.0+0", config);
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

        fixture.AssertFullSemver("1.1.0-alpha.2", config);
    }
}
