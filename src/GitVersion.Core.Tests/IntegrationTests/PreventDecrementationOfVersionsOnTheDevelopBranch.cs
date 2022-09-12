using GitTools.Testing;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests.IntegrationTests;

public class PreventDecrementationOfVersionsOnTheDevelopBranch : TestBase, IDisposable
{
    private readonly EmptyRepositoryFixture fixture = new("develop");

    public void Dispose() => fixture.Dispose();

    [Test]
    public void Discussion3177()
    {
        var configBuilder = ConfigBuilder.New.WithNextVersion("1.0.0")
            .WithVersioningMode(VersioningMode.ContinuousDelivery).WithoutAnyTrackMergeTargets();

        // create develop branche and make a commit
        MakeACommit();

        // ✅ succeeds as expected
        GetCurrentSemVer(ConfigBuilder.New.Build()).ShouldBe("0.1.0-alpha.1");
        fixture.AssertFullSemver("1.0.0-alpha.1", configBuilder.WithTrackMergeTarget("develop", true).Build());
        fixture.AssertFullSemver("1.0.0-alpha.1", configBuilder.WithoutAnyTrackMergeTargets().Build());

        MakeACommit();

        // ✅ succeeds as expected
        GetCurrentSemVer(ConfigBuilder.New.Build()).ShouldBe("0.1.0-alpha.2");
        fixture.AssertFullSemver("1.0.0-alpha.2", configBuilder.WithTrackMergeTarget("develop", true).Build());
        fixture.AssertFullSemver("1.0.0-alpha.2", configBuilder.WithoutAnyTrackMergeTargets().Build());

        // now we are ready to start with the preparation of the 1.0.0 release
        CreateBranch("release/1.0.0");

        // ✅ succeeds as expected
        GetCurrentSemVer(ConfigBuilder.New.Build()).ShouldBe("1.1.0-alpha.0");
        fixture.AssertFullSemver("1.1.0-alpha.0", configBuilder.WithTrackMergeTarget("develop", true).Build());
        fixture.AssertFullSemver("1.1.0-alpha.0", configBuilder.WithoutAnyTrackMergeTargets().Build());

        CheckoutBranch("release/1.0.0");

        // ✅ succeeds as expected
        GetCurrentSemVer(ConfigBuilder.New.Build()).ShouldBe("1.0.0-beta.1");
        fixture.AssertFullSemver("1.0.0-beta.1+0", configBuilder.Build());
        fixture.AssertFullSemver("1.0.0-beta.1+0", configBuilder.Build());

        // make another commit on release/1.0.0 to prepare the actual beta1 release
        MakeACommit();

        // ✅ succeeds as expected
        GetCurrentSemVer(ConfigBuilder.New.Build()).ShouldBe("1.0.0-beta.1");
        fixture.AssertFullSemver("1.0.0-beta.1+1", configBuilder.Build());
        fixture.AssertFullSemver("1.0.0-beta.1+1", configBuilder.Build());

        // now we makes changes on develop that may or may not end up in the 1.0.0 release
        CheckoutBranch("develop");

        // ✅ succeeds as expected
        GetCurrentSemVer(ConfigBuilder.New.Build()).ShouldBe("1.1.0-alpha.0");
        fixture.AssertFullSemver("1.1.0-alpha.0", configBuilder.WithTrackMergeTarget("develop", true).Build());
        fixture.AssertFullSemver("1.1.0-alpha.0", configBuilder.WithoutAnyTrackMergeTargets().Build());

        MakeACommit();

        // ✅ succeeds as expected
        GetCurrentSemVer(ConfigBuilder.New.Build()).ShouldBe("1.1.0-alpha.1");
        fixture.AssertFullSemver("1.1.0-alpha.1", configBuilder.WithTrackMergeTarget("develop", true).Build());
        fixture.AssertFullSemver("1.1.0-alpha.1", configBuilder.WithoutAnyTrackMergeTargets().Build());

        // now we do the actual release of beta 1
        CheckoutBranch("release/1.0.0");
        ApplyTag("1.0.0-beta.1");

        // ✅ succeeds as expected
        GetCurrentSemVer(ConfigBuilder.New.Build()).ShouldBe("1.0.0-beta.1");
        fixture.AssertFullSemver("1.0.0-beta.1", configBuilder.Build()); // 1.0.0-beta.1+0??
        fixture.AssertFullSemver("1.0.0-beta.1", configBuilder.Build());

        // continue with more work on develop that may or may not end up in the 1.0.0 release
        CheckoutBranch("develop");

        // ✅ succeeds as expected
        GetCurrentSemVer(ConfigBuilder.New.Build()).ShouldBe("1.1.0-alpha.1");
        fixture.AssertFullSemver("1.1.0-alpha.1", configBuilder.WithTrackMergeTarget("develop", true).Build());
        fixture.AssertFullSemver("1.1.0-alpha.1", configBuilder.WithoutAnyTrackMergeTargets().Build());

        MakeACommit();

        // ✅ succeeds as expected
        GetCurrentSemVer(ConfigBuilder.New.Build()).ShouldBe("1.1.0-alpha.2");
        fixture.AssertFullSemver("1.1.0-alpha.2", configBuilder.WithTrackMergeTarget("develop", true).Build());
        fixture.AssertFullSemver("1.1.0-alpha.2", configBuilder.WithoutAnyTrackMergeTargets().Build());

        // now we decide that the new on develop should be part of the beta 2 release
        // se we merge it into release/1.0.0 with --no-ff because it is a protected branch
        // but we don't do the release of beta 2 just yet
        CheckoutBranch("release/1.0.0");
        MergeWithNoFF("develop");

        // ✅ succeeds as expected
        GetCurrentSemVer(ConfigBuilder.New.Build()).ShouldBe("1.0.0-beta.2");
        fixture.AssertFullSemver("1.0.0-beta.2+2", configBuilder.Build());
        fixture.AssertFullSemver("1.0.0-beta.2+2", configBuilder.Build());

        CheckoutBranch("develop");

        // ✅ succeeds as expected
        GetCurrentSemVer(ConfigBuilder.New.Build()).ShouldBe("1.1.0-alpha.0");
        fixture.AssertFullSemver("1.1.0-alpha.0", configBuilder.WithTrackMergeTarget("develop", true).Build());
        fixture.AssertFullSemver("1.1.0-alpha.0", configBuilder.WithoutAnyTrackMergeTargets().Build());

        CheckoutBranch("release/1.0.0");
        ApplyTag("1.0.0-beta.2");

        // ✅ succeeds as expected
        GetCurrentSemVer(ConfigBuilder.New.Build()).ShouldBe("1.0.0-beta.2");
        fixture.AssertFullSemver("1.0.0-beta.2", configBuilder.Build());
        fixture.AssertFullSemver("1.0.0-beta.2", configBuilder.Build());

        CheckoutBranch("develop");

        // ✅ succeeds as expected
        GetCurrentSemVer(ConfigBuilder.New.Build()).ShouldBe("1.1.0-alpha.0");
        fixture.AssertFullSemver("1.1.0-alpha.0", configBuilder.WithTrackMergeTarget("develop", true).Build());
        fixture.AssertFullSemver("1.1.0-alpha.0", configBuilder.WithoutAnyTrackMergeTargets().Build());

        MergeWithNoFF("release/1.0.0");

        // ✅ succeeds as expected
        GetCurrentSemVer(ConfigBuilder.New.Build()).ShouldBe("1.1.0-alpha.3");
        fixture.AssertFullSemver("1.1.0-alpha.3", configBuilder.WithTrackMergeTarget("develop", true).Build());
        fixture.AssertFullSemver("1.1.0-alpha.3", configBuilder.WithoutAnyTrackMergeTargets().Build());

        DeleteBranch("release/1.0.0");

        // ✅ succeeds as expected
        GetCurrentSemVer(ConfigBuilder.New.Build()).ShouldBe("1.1.0-alpha.3");
        fixture.AssertFullSemver("1.1.0-alpha.3", configBuilder.WithTrackMergeTarget("develop", true).Build());
        fixture.AssertFullSemver("1.0.0-alpha.7", configBuilder.WithoutAnyTrackMergeTargets().Build());

        configBuilder.WithNextVersion("1.1.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.3", configBuilder.WithTrackMergeTarget("develop", true).Build());
        fixture.AssertFullSemver("1.1.0-alpha.7", configBuilder.WithoutAnyTrackMergeTargets().Build());
        configBuilder.WithNextVersion("1.0.0");

        RevertTag("1.0.0-beta.1");
        RevertTag("1.0.0-beta.2");

        // ✅ succeeds as expected
        GetCurrentSemVer(ConfigBuilder.New.Build()).ShouldBe("1.1.0-alpha.3");
        fixture.AssertFullSemver("1.1.0-alpha.3", configBuilder.WithTrackMergeTarget("develop", true).Build());
        fixture.AssertFullSemver("1.0.0-alpha.7", configBuilder.WithoutAnyTrackMergeTargets().Build());

    }

    private void MakeACommit() => fixture.Repository.MakeACommit();

    private void DeleteBranch(string branchName) => fixture.Repository.Branches.Remove(branchName);

    private void CheckoutBranch(string branchName)
        => Commands.Checkout(fixture.Repository, fixture.Repository.Branches[branchName]);

    private void CreateBranch(string branchName) => fixture.Repository.CreateBranch(branchName);

    private string GetCurrentSemVer(Config configuration) => fixture.GetVersion(configuration).SemVer;

    private void ApplyTag(string tag) => fixture.Repository.ApplyTag(tag);

    private void RevertTag(string tag) => fixture.Repository.Tags.Remove(tag);

    private void MergeWithNoFF(string sourceBranch) => fixture.Repository.MergeNoFF(sourceBranch);
}
