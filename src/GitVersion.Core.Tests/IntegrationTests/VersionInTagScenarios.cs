using GitTools.Testing;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
internal class VersionInTagScenarios
{
    [Test]
    public void TagPreReleaseWeightIsNotConfigured_HeadIsATaggedCommit_WeightedPreReleaseNumberShouldBeTheDefaultValue()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .Add(new Config
            {
                AssemblyFileVersioningFormat = "{Major}.{Minor}.{Patch}.{WeightedPreReleaseNumber}",
            })
            .Build();

        // Act
        using var fixture = new BaseGitFlowRepositoryFixture("1.0.0");
        fixture.MakeATaggedCommit("1.1.0");
        var version = fixture.GetVersion(config);

        // Assert
        version.AssemblySemFileVer.ShouldBe("1.1.0.60000");
    }

    [Test]
    public void TagPreReleaseWeightIsConfigured_HeadIsATaggedCommit_WeightedPreReleaseNumberShouldBeTheSameAsTheTagPreReleaseWeight()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .Add(new Config
            {
                AssemblyFileVersioningFormat = "{Major}.{Minor}.{Patch}.{WeightedPreReleaseNumber}",
                TagPreReleaseWeight = 65535
            })
            .Build();

        // Act
        using var fixture = new BaseGitFlowRepositoryFixture("1.0.0");
        fixture.MakeATaggedCommit("1.1.0");
        var version = fixture.GetVersion(config);

        // Assert
        version.AssemblySemFileVer.ShouldBe("1.1.0.65535");
    }

    [Test]
    public void TagPreReleaseWeightIsConfigured_GitFlowReleaseIsFinished_WeightedPreReleaseNumberShouldBeTheSameAsTheTagPreReleaseWeight()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .Add(new Config
            {
                AssemblyFileVersioningFormat = "{Major}.{Minor}.{Patch}.{WeightedPreReleaseNumber}",
                TagPreReleaseWeight = 65535,
                VersioningMode = VersioningMode.ContinuousDeployment
            })
            .Build();

        // Act
        using var fixture = new BaseGitFlowRepositoryFixture("1.0.0");
        fixture.Checkout(TestBase.MainBranch);
        fixture.MergeNoFF("develop");
        fixture.Checkout("develop");
        fixture.MakeACommit("Feature commit 1");
        fixture.BranchTo("release/1.1.0");
        fixture.MakeACommit("Release commit 1");
        fixture.AssertFullSemver("1.1.0-beta.1", config);
        fixture.ApplyTag("1.1.0");
        var version = fixture.GetVersion(config);

        // Assert
        version.AssemblySemFileVer.ShouldBe("1.1.0.65535");
    }

    [Test]
    public void TagPreReleaseWeightIsNotConfigured_GitFlowReleaseIsFinished_WeightedPreReleaseNumberShouldBeTheDefaultValue()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .Add(new Config
            {
                AssemblyFileVersioningFormat = "{Major}.{Minor}.{Patch}.{WeightedPreReleaseNumber}",
                VersioningMode = VersioningMode.ContinuousDeployment
            })
            .Build();

        // Act
        using var fixture = new BaseGitFlowRepositoryFixture("1.0.0");
        fixture.Checkout(TestBase.MainBranch);
        fixture.MergeNoFF("develop");
        fixture.Checkout("develop");
        fixture.MakeACommit("Feature commit 1");
        fixture.BranchTo("release/1.1.0");
        fixture.MakeACommit("Release commit 1");
        fixture.AssertFullSemver("1.1.0-beta.1", config);
        fixture.ApplyTag("1.1.0");
        var version = fixture.GetVersion(config);

        // Assert
        version.AssemblySemFileVer.ShouldBe("1.1.0.60000");
    }
}
