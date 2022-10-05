using GitTools.Testing;
using GitVersion.Core.Tests.Helpers;
using NUnit.Framework;

namespace GitVersion.Core.Tests.IntegrationTests;

/// <summary>
/// Prevent decrementation of versions on the develop branch #3177
/// </summary>
[TestFixture]
public class PreventDecrementationOfVersionsOnTheDevelopBranchScenario
{
    [Test]
    public void Discussion3177()
    {
        using EmptyRepositoryFixture fixture = new("develop");

        var configurationBuilder = TestConfigurationBuilder.New;

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.1", configurationBuilder.Build());

        configurationBuilder.WithNextVersion("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-alpha.1", configurationBuilder.Build());

        fixture.MakeACommit();
        configurationBuilder.WithNextVersion(null);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.2", configurationBuilder.Build());

        configurationBuilder.WithNextVersion("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-alpha.2", configurationBuilder.Build());

        // now we are ready to start with the preparation of the 1.0.0 release
        fixture.BranchTo("release/1.0.0");
        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.0", configurationBuilder.Build());

        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+0", configurationBuilder.Build());

        // make another commit on release/1.0.0 to prepare the actual beta1 release
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configurationBuilder.Build());

        // now we makes changes on develop that may or may not end up in the 1.0.0 release
        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.0", configurationBuilder.Build());

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.1", configurationBuilder.Build());

        // now we do the actual release of beta 1
        fixture.Checkout("release/1.0.0");
        fixture.ApplyTag("1.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1", configurationBuilder.Build());

        // continue with more work on develop that may or may not end up in the 1.0.0 release
        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.1", configurationBuilder.Build());

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.2", configurationBuilder.Build());

        // now we decide that the new on develop should be part of the beta 2 release
        // se we merge it into release/1.0.0 with --no-ff because it is a protected branch
        // but we don't do the release of beta 2 just yet
        fixture.Checkout("release/1.0.0");
        fixture.MergeNoFF("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.2+2", configurationBuilder.Build());

        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.0", configurationBuilder.Build());

        fixture.Checkout("release/1.0.0");
        fixture.ApplyTag("1.0.0-beta.2");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.2", configurationBuilder.Build());

        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.0", configurationBuilder.Build());

        fixture.MergeNoFF("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.3", configurationBuilder.Build());

        fixture.Repository.Branches.Remove("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.3", configurationBuilder.Build());

        configurationBuilder.WithNextVersion("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.3", configurationBuilder.Build());

        fixture.Repository.Tags.Remove("1.0.0-beta.1");
        fixture.Repository.Tags.Remove("1.0.0-beta.2");

        // ❌ expected: "1.0.0-alpha.3"
        // This behavior needs to be changed for the git flow workflow using the track-merge-message or track-merge-target options.
        // [Bug] track-merge-changes produces unexpected result when combining hotfix and support branches #3052
        fixture.AssertFullSemver("1.1.0-alpha.3", configurationBuilder.Build());

        configurationBuilder.WithNextVersion("1.1.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.3", configurationBuilder.Build());

        // Merge from develop to main
        fixture.BranchTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0+3", configurationBuilder.Build());

        configurationBuilder.WithNextVersion(null);

        // ❌ expected: "0.0.1+3"
        // This behavior needs to be changed for the git flow workflow using the track-merge-message or track-merge-target options.
        // [Bug] track-merge-changes produces unexpected result when combining hotfix and support branches #3052
        fixture.AssertFullSemver("1.0.0+3", configurationBuilder.Build());

        configurationBuilder.WithNextVersion("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0+3", configurationBuilder.Build());

        // Mark this version as RTM
        fixture.ApplyTag("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0", configurationBuilder.Build());
    }
}
