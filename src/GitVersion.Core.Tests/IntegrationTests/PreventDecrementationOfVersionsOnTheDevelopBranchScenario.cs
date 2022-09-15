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

        var configBuilder = ConfigBuilder.New;

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.1", configBuilder.Build());

        configBuilder.WithNextVersion("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-alpha.1", configBuilder.Build());

        fixture.MakeACommit();
        configBuilder.WithNextVersion(null);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.2", configBuilder.Build());

        configBuilder.WithNextVersion("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-alpha.2", configBuilder.Build());

        // now we are ready to start with the preparation of the 1.0.0 release
        fixture.BranchTo("release/1.0.0");
        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.0", configBuilder.Build());

        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+0", configBuilder.Build());

        // make another commit on release/1.0.0 to prepare the actual beta1 release
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configBuilder.Build());

        // now we makes changes on develop that may or may not end up in the 1.0.0 release
        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.0", configBuilder.Build());

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.1", configBuilder.Build());

        // now we do the actual release of beta 1
        fixture.Checkout("release/1.0.0");
        fixture.ApplyTag("1.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1", configBuilder.Build());

        // continue with more work on develop that may or may not end up in the 1.0.0 release
        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.1", configBuilder.Build());

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.2", configBuilder.Build());

        // now we decide that the new on develop should be part of the beta 2 release
        // se we merge it into release/1.0.0 with --no-ff because it is a protected branch
        // but we don't do the release of beta 2 just yet
        fixture.Checkout("release/1.0.0");
        fixture.MergeNoFF("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.2+2", configBuilder.Build());

        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.0", configBuilder.Build());

        fixture.Checkout("release/1.0.0");
        fixture.ApplyTag("1.0.0-beta.2");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.2", configBuilder.Build());

        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.0", configBuilder.Build());

        fixture.MergeNoFF("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.3", configBuilder.Build());

        fixture.Repository.Branches.Remove("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.3", configBuilder.Build());

        configBuilder.WithNextVersion("1.1.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.3", configBuilder.Build());

        fixture.Repository.Tags.Remove("1.0.0-beta.1");
        fixture.Repository.Tags.Remove("1.0.0-beta.2");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.3", configBuilder.Build());

        configBuilder.WithNextVersion("1.0.0");

        // ❌ fails! expected: "1.0.0-alpha.3"
        // this behaviour needs to be changed for the git flow workflow.
        fixture.AssertFullSemver("1.1.0-alpha.3", configBuilder.Build());

        configBuilder.WithNextVersion(null);
        fixture.BranchTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0+3", configBuilder.Build());
    }
}
