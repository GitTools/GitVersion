using GitVersion.Git;
using GitVersion.VersionCalculation;

namespace GitVersion.Tests;

[TestFixture]
public class VersionSourceTests : TestBase
{
    [Test]
    public void VersionSourceSha()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();

        var nextVersionCalculator = GetNextVersionCalculator(fixture.Repository.ToGitRepository());

        var semanticVersion = nextVersionCalculator.FindVersion();

        semanticVersion.BuildMetaData.VersionSourceSha.ShouldBeNull();
        semanticVersion.BuildMetaData.VersionSourceDistance.ShouldBe(3);
    }

    [Test]
    public void VersionSourceShaOneCommit()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();

        var nextVersionCalculator = GetNextVersionCalculator(fixture.Repository.ToGitRepository());

        var semanticVersion = nextVersionCalculator.FindVersion();

        semanticVersion.BuildMetaData.VersionSourceSha.ShouldBeNull();
        semanticVersion.BuildMetaData.VersionSourceDistance.ShouldBe(1);
    }

    [Test]
    public void VersionSourceShaUsingTag()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();
        fixture.BranchTo("develop");
        var secondCommitSha = fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();

        var nextVersionCalculator = GetNextVersionCalculator(fixture.Repository.ToGitRepository());

        var semanticVersion = nextVersionCalculator.FindVersion();

        semanticVersion.BuildMetaData.VersionSourceSha.ShouldBe(secondCommitSha);
        semanticVersion.BuildMetaData.VersionSourceDistance.ShouldBe(1);
    }

    private static INextVersionCalculator GetNextVersionCalculator(IGitRepository repository)
    {
        var serviceProvider = BuildServiceProvider(repository);
        return serviceProvider.GetRequiredService<INextVersionCalculator>();
    }
}
