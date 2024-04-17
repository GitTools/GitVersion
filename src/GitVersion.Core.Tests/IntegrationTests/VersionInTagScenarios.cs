using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
internal class VersionInTagScenarios
{
    [Test]
    public void TagPreReleaseWeightIsNotConfigured_HeadIsATaggedCommit_WeightedPreReleaseNumberShouldBeTheDefaultValue()
    {
        // Arrange
        var configuration = GitFlowConfigurationBuilder.New
            .WithAssemblyFileVersioningFormat("{Major}.{Minor}.{Patch}.{WeightedPreReleaseNumber}")
            .Build();

        // Act
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.1.0");
        var version = fixture.GetVersion(configuration);

        // Assert
        version.AssemblySemFileVer.ShouldBe("1.1.0.60000");
    }

    [Test]
    public void TagPreReleaseWeightIsConfigured_HeadIsATaggedCommit_WeightedPreReleaseNumberShouldBeTheSameAsTheTagPreReleaseWeight()
    {
        // Arrange
        var configuration = GitFlowConfigurationBuilder.New
            .WithAssemblyFileVersioningFormat("{Major}.{Minor}.{Patch}.{WeightedPreReleaseNumber}")
            .WithTagPreReleaseWeight(65535)
            .Build();

        // Act
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.1.0");
        var version = fixture.GetVersion(configuration);

        // Assert
        version.AssemblySemFileVer.ShouldBe("1.1.0.65535");
    }

    [Test]
    public void TagPreReleaseWeightIsConfigured_GitFlowReleaseIsFinished_WeightedPreReleaseNumberShouldBeTheSameAsTheTagPreReleaseWeight()
    {
        // Arrange
        var configuration = GitFlowConfigurationBuilder.New
            .WithAssemblyFileVersioningFormat("{Major}.{Minor}.{Patch}.{WeightedPreReleaseNumber}")
            .WithTagPreReleaseWeight(65535)
            .Build();

        // Act
        using var fixture = new BaseGitFlowRepositoryFixture("1.0.0");
        fixture.Checkout(TestBase.MainBranch);
        fixture.MergeNoFF("develop");
        fixture.Checkout("develop");
        fixture.MakeACommit("Feature commit 1");
        fixture.BranchTo("release/1.1.0");
        fixture.MakeACommit("Release commit 1");
        fixture.AssertFullSemver("1.1.0-beta.1+3", configuration);

        fixture.Checkout("main");
        fixture.MergeNoFF("release/1.1.0");
        fixture.ApplyTag("1.1.0");
        var version = fixture.GetVersion(configuration);

        // Assert
        version.AssemblySemFileVer.ShouldBe("1.1.0.65535");
    }

    [Test]
    public void TagPreReleaseWeightIsNotConfigured_GitFlowReleaseIsFinished_WeightedPreReleaseNumberShouldBeTheDefaultValue()
    {
        // Arrange
        var configuration = GitFlowConfigurationBuilder.New
            .WithAssemblyFileVersioningFormat("{Major}.{Minor}.{Patch}.{WeightedPreReleaseNumber}")
            .Build();

        // Act
        using var fixture = new BaseGitFlowRepositoryFixture("1.0.0");
        fixture.Checkout(TestBase.MainBranch);
        fixture.MergeNoFF("develop");
        fixture.Checkout("develop");
        fixture.MakeACommit("Feature commit 1");
        fixture.BranchTo("release/1.1.0");
        fixture.MakeACommit("Release commit 1");
        fixture.AssertFullSemver("1.1.0-beta.1+3", configuration);

        fixture.Checkout("main");
        fixture.MergeNoFF("release/1.1.0");
        fixture.ApplyTag("1.1.0");
        var version = fixture.GetVersion(configuration);

        // Assert
        version.AssemblySemFileVer.ShouldBe("1.1.0.60000");
    }
}
