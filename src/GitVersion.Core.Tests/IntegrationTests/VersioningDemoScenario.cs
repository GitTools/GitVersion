using GitTools.Testing;
using GitVersion.Model.Configuration;
using GitVersion.OutputVariables;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests.IntegrationTests;

public class VersioningDemoScenario
{
    [Test]
    public void ReleaseAndDevelopProblemDemo()
    {
        var configuration = new Config
        {
            // Settings the below NextVersion results in an exception
            // I wanted to set it to globally start with version 1
            // Setting the version to 1.0 (which is not a valid semantic version)
            // works in GitVersion.yml but not here.

            // NextVersion = "1.0"
        };

        // create main and develop branch, develop is two commits ahead of main
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeACommit();

        var previousVersion = GetSemVer(fixture.GetVersion(configuration));

        // make a 3rd commit on develop
        fixture.Repository.MakeACommit();

        var currentVersion = GetSemVer(fixture.GetVersion(configuration));

        currentVersion.ShouldBeGreaterThan(previousVersion,
            "the semver should be incremented after a commit on develop");

        // we are ready to prepare the 1.0.0 release, create and checkout release/1.0.0
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("release/1.0.0"));

        fixture.GetVersion(configuration).SemVer.ShouldBe("1.0.0-beta.1",
            "the first semver on release/1.0.0 should be beta1");

        // make another commit on release/1.0.0 to prepare the actual beta1 release
        fixture.Repository.MakeACommit();

        fixture.GetVersion(configuration).SemVer.ShouldBe("1.0.0-beta.1",
            "the semver on release/1.0.0 should still be be beta1");

        Commands.Checkout(fixture.Repository, fixture.Repository.Branches["develop"]);

        previousVersion = currentVersion;
        currentVersion = GetSemVer(fixture.GetVersion(configuration));

        currentVersion.ShouldBe(previousVersion,
            "the semver on develop should not have changed " +
            "even when release/1.0.0 has new commits due to beta 1 preparations");

        // now some other team member makes changes on develop that may or may not end up in 1.0.0
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeACommit();

        previousVersion = currentVersion;
        currentVersion = GetSemVer(fixture.GetVersion(configuration));

        currentVersion.ShouldBeGreaterThan(previousVersion,
            "the semver should be incremented after a even more commit on develop");

        Commands.Checkout(fixture.Repository, fixture.Repository.Branches["release/1.0.0"]);

        // now we release the beta 1
        fixture.Repository.ApplyTag("1.0.0-beta1");

        fixture.GetVersion(configuration).SemVer.ShouldBe("1.0.0-beta.1",
            "the on release/1.0.0 should still be beta1 after the beta 1 tag");

        // continue with more work on develop that may or may not end up in 1.0.0
        Commands.Checkout(fixture.Repository, fixture.Repository.Branches["develop"]);
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeACommit();

        previousVersion = currentVersion;
        currentVersion = GetSemVer(fixture.GetVersion(configuration));

        currentVersion.ShouldBeGreaterThan(previousVersion,
            "the semver should be incremented after a even more commit on develop");

        // now we decide that the three commits on develop should be part of the beta 2 release
        // se we merge it into release/1.0.0 with --no-ff because it is a protected branch
        Commands.Checkout(fixture.Repository, fixture.Repository.Branches["release/1.0.0"]);
        fixture.Repository.Merge(
            fixture.Repository.Branches["develop"],
            Generate.SignatureNow(),
            new MergeOptions {FastForwardStrategy = FastForwardStrategy.NoFastForward});

        fixture.GetVersion(configuration).SemVer.ShouldBe("1.0.0-beta.2",
            "the next semver on release/1.0.0 should be beta2");

        Commands.Checkout(fixture.Repository, fixture.Repository.Branches["develop"]);
        previousVersion = currentVersion;
        currentVersion = GetSemVer(fixture.GetVersion(configuration));

        currentVersion.ShouldBeGreaterThanOrEqualTo(previousVersion,
            "the semver should be incremented (or unchanged) " +
            "after we merged develop into release/1.0.0");

        static SemanticVersion GetSemVer(VersionVariables ver)
            => SemanticVersion.Parse(ver.FullSemVer, null);
    }
}
