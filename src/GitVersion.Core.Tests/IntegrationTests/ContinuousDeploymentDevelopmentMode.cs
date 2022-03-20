using GitTools.Testing;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using NUnit.Framework;

namespace GitVersion.Core.Tests.IntegrationTests;

public class ContinuousDeploymentDevelopmentMode : TestBase
{
    private readonly Config config = new() { VersioningMode = VersioningMode.ContinuousDeployment };

    [Test]
    public void VerifyManuallyIncrementingVersion()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit("1");

        fixture.BranchTo("development", "development");
        fixture.MakeACommit("2 +semver: patch");

        fixture.AssertFullSemver("0.1.1-alpha.1", this.config);

        fixture.MakeACommit("3 +semver: patch");

        fixture.AssertFullSemver("0.1.2-alpha.2", this.config);
    }
}
