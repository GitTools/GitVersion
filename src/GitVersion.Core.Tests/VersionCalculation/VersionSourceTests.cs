using GitVersion.Core.Tests.Helpers;
using GitVersion.VersionCalculation;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Core.Tests;

[TestFixture]
public class VersionSourceTests : TestBase
{
    [Test]
    public void VersionSourceSha()
    {
        using var fixture = new EmptyRepositoryFixture();
        _ = fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
        _ = fixture.Repository.MakeACommit();
        var featureBranch = fixture.Repository.CreateBranch("feature/foo");
        Commands.Checkout(fixture.Repository, featureBranch);
        _ = fixture.Repository.MakeACommit();

        var nextVersionCalculator = GetNextVersionCalculator(fixture);

        var nextVersion = nextVersionCalculator.FindVersion();

        nextVersion.IncrementedVersion.BuildMetaData.VersionSourceSha.ShouldBeNull();
        nextVersion.IncrementedVersion.BuildMetaData.CommitsSinceVersionSource.ShouldBe(3);
    }

    [Test]
    public void VersionSourceShaOneCommit()
    {
        using var fixture = new EmptyRepositoryFixture();
        _ = fixture.Repository.MakeACommit();

        var nextVersionCalculator = GetNextVersionCalculator(fixture);

        var nextVersion = nextVersionCalculator.FindVersion();

        nextVersion.IncrementedVersion.BuildMetaData.VersionSourceSha.ShouldBeNull();
        nextVersion.IncrementedVersion.BuildMetaData.CommitsSinceVersionSource.ShouldBe(1);
    }

    [Test]
    public void VersionSourceShaUsingTag()
    {
        using var fixture = new EmptyRepositoryFixture();
        _ = fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
        var secondCommit = fixture.Repository.MakeACommit();
        _ = fixture.Repository.Tags.Add("1.0.0", secondCommit);
        var featureBranch = fixture.Repository.CreateBranch("feature/foo");
        Commands.Checkout(fixture.Repository, featureBranch);
        _ = fixture.Repository.MakeACommit();

        var nextVersionCalculator = GetNextVersionCalculator(fixture);

        var nextVersion = nextVersionCalculator.FindVersion();

        nextVersion.IncrementedVersion.BuildMetaData.VersionSourceSha.ShouldBe(secondCommit.Sha);
        nextVersion.IncrementedVersion.BuildMetaData.CommitsSinceVersionSource.ShouldBe(1);
    }

    private static INextVersionCalculator GetNextVersionCalculator(RepositoryFixtureBase fixture)
    {
        var sp = BuildServiceProvider(fixture.RepositoryPath, fixture.Repository.ToGitRepository(), fixture.Repository.Head.CanonicalName);
        return sp.GetRequiredService<INextVersionCalculator>();
    }
}
