using GitTools.Testing;
using GitVersion.Core.Tests.Helpers;
using GitVersion.VersionCalculation;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests;

[TestFixture]
public class VersionSourceTests : TestBase
{
    [Test]
    public void VersionSourceSha()
    {
        using var fixture = new EmptyRepositoryFixture();
        var initialCommit = fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
        _ = fixture.Repository.MakeACommit();
        var featureBranch = fixture.Repository.CreateBranch("feature/foo");
        Commands.Checkout(fixture.Repository, featureBranch);
        _ = fixture.Repository.MakeACommit();

        var nextVersionCalculator = GetNextVersionCalculator(fixture);

        var version = nextVersionCalculator.FindVersion();

        version.BuildMetaData.VersionSourceSha.ShouldBe(initialCommit.Sha);
        version.BuildMetaData.CommitsSinceVersionSource.ShouldBe(2);
    }

    [Test]
    public void VersionSourceShaOneCommit()
    {
        using var fixture = new EmptyRepositoryFixture();
        var initialCommit = fixture.Repository.MakeACommit();

        var nextVersionCalculator = GetNextVersionCalculator(fixture);

        var version = nextVersionCalculator.FindVersion();

        version.BuildMetaData.VersionSourceSha.ShouldBe(initialCommit.Sha);
        version.BuildMetaData.CommitsSinceVersionSource.ShouldBe(0);
    }

    [Test]
    public void VersionSourceShaUsingTag()
    {
        using var fixture = new EmptyRepositoryFixture();
        _ = fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
        var secondCommit = fixture.Repository.MakeACommit();
        _ = fixture.Repository.Tags.Add("1.0", secondCommit);
        var featureBranch = fixture.Repository.CreateBranch("feature/foo");
        Commands.Checkout(fixture.Repository, featureBranch);
        _ = fixture.Repository.MakeACommit();

        var nextVersionCalculator = GetNextVersionCalculator(fixture);

        var version = nextVersionCalculator.FindVersion();

        version.BuildMetaData.VersionSourceSha.ShouldBe(secondCommit.Sha);
        version.BuildMetaData.CommitsSinceVersionSource.ShouldBe(1);
    }

    private static INextVersionCalculator GetNextVersionCalculator(RepositoryFixtureBase fixture)
    {
        var sp = BuildServiceProvider(fixture.RepositoryPath, fixture.Repository.ToGitRepository(), fixture.Repository.Head.CanonicalName);
        return sp.GetService<INextVersionCalculator>();
    }
}
