using GitTools.Testing;
using GitVersion.Core.Tests.Helpers;
using GitVersion.VersionCalculation;
using NUnit.Framework;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class ContinuousDeploymentTestScenarios
{
    [Test]
    public void __Just_A_Test__()
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

        configBuilder.WithNextVersion("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.3", configBuilder.Build());

        fixture.Repository.Tags.Remove("1.0.0-beta.1");
        fixture.Repository.Tags.Remove("1.0.0-beta.2");

        // ❌ expected: "1.0.0-alpha.3"
        // This behavior needs to be changed for the git flow workflow using the track-merge-message or track-merge-target options.
        // [Bug] track-merge-changes produces unexpected result when combining hotfix and support branches #3052
        fixture.AssertFullSemver("1.1.0-alpha.3", configBuilder.Build());

        configBuilder.WithNextVersion("1.1.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.3", configBuilder.Build());

        // Merge from develop to main
        fixture.BranchTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0+3", configBuilder.Build());

        configBuilder.WithNextVersion(null);

        // ❌ expected: "0.0.1+3"
        // This behavior needs to be changed for the git flow workflow using the track-merge-message or track-merge-target options.
        // [Bug] track-merge-changes produces unexpected result when combining hotfix and support branches #3052
        fixture.AssertFullSemver("1.0.0+3", configBuilder.Build());

        configBuilder.WithNextVersion("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0+3", configBuilder.Build());

        // Mark this version as RTM
        fixture.ApplyTag("2.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0", configBuilder.Build());
    }

    [Test]
    public void ShouldUseTheFallbackVersionOnMainWhenNoVersionsAreAvailable()
    {
        // * 2373a87 58 minutes ago  (HEAD -> main)

        var configuration = ConfigBuilder.New.WithVersioningMode(VersioningMode.ContinuousDeployment).Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-ci.1", configuration);

        fixture.Repository.DumpGraph(Console.WriteLine);
    }

    [Test]
    public void ShouldUseTheFallbackVersionOnDevelopWhenNoVersionsAreAvailable()
    {
        // * a831d61 58 minutes ago  (HEAD -> develop)

        var configuration = ConfigBuilder.New.WithVersioningMode(VersioningMode.ContinuousDeployment).Build();

        using var fixture = new EmptyRepositoryFixture("develop");

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.1", configuration);

        fixture.Repository.DumpGraph(Console.WriteLine);
    }

    [Test]
    public void ShouldUseConfiguredNextVersionOnMainWhenNoHigherVersionsAvailable()
    {
        // * 8c64db3 58 minutes ago  (HEAD -> main)

        var configuration = ConfigBuilder.New
            .WithVersioningMode(VersioningMode.ContinuousDeployment)
            .WithNextVersion("1.0.0").Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-ci.1", configuration);

        fixture.Repository.DumpGraph(Console.WriteLine);
    }

    [Test]
    public void ShouldNotMatterWhenConfiguredNextVersionIsEqualsToTheTaggeVersion()
    {
        // * 858f71b 58 minutes ago  (HEAD -> main, tag: 1.0.0)

        var configuration = ConfigBuilder.New
            .WithVersioningMode(VersioningMode.ContinuousDeployment)
            .WithNextVersion("1.0.0").Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeATaggedCommit("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0", configuration);

        fixture.Repository.DumpGraph(Console.WriteLine);
    }

    [Test]
    public void ShouldNotMatterWhenConfiguredNextVersionIsGreaterThanTheTaggedVersion()
    {
        // * ba74727 58 minutes ago  (HEAD -> main, tag: 1.1.0)

        var configuration = ConfigBuilder.New
            .WithVersioningMode(VersioningMode.ContinuousDeployment)
            .WithNextVersion("1.0.0").Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeATaggedCommit("1.1.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0", configuration);

        fixture.Repository.DumpGraph(Console.WriteLine);
    }

    [Test]
    public void ShouldCalculateTheCorrectVersionWhenMergingFromMainToFeatureBranch()
    {
        // *94f03f8 55 minutes ago(HEAD -> main)
        // |\  
        // | *b1f41a4 56 minutes ago
        // |/
        // *ec77f9c 58 minutes ago

        var configuration = ConfigBuilder.New.WithVersioningMode(VersioningMode.ContinuousDeployment).Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-ci.1", configuration);

        fixture.BranchTo("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-just-a-test.1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-just-a-test.2", configuration);

        fixture.Checkout("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-ci.1", configuration);

        fixture.MergeNoFF("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-ci.3", configuration);

        fixture.Repository.Branches.Remove("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-ci.3", configuration);

        fixture.Repository.DumpGraph(Console.WriteLine);
    }

    [Test]
    public void ShouldCalculateTheCorrectVersionWhenMergingFromDevelopToFeatureBranch()
    {
        // *2c475bf 55 minutes ago(HEAD -> develop)
        // |\  
        // | *e05365d 56 minutes ago
        // |/
        // *67acc03 58 minutes ago(main)

        var configuration = ConfigBuilder.New.WithVersioningMode(VersioningMode.ContinuousDeployment).Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();
        fixture.BranchTo("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.1", configuration);

        fixture.BranchTo("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-just-a-test.1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-just-a-test.2", configuration);

        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.1", configuration);

        fixture.MergeNoFF("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.3", configuration);

        fixture.Repository.Branches.Remove("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.3", configuration);

        fixture.Repository.DumpGraph(Console.WriteLine);
    }

    [Test]
    public void ShouldCalculateTheCorrectVersionWhenMergingFromReleaseToFeatureBranch()
    {
        // *b1e5593 53 minutes ago(HEAD -> release/ 1.0.0)
        // *8752695 55 minutes ago
        // |\  
        // | *0965b88 56 minutes ago
        // |/
        // *f63a536 58 minutes ago(main)

        var configuration = ConfigBuilder.New.WithVersioningMode(VersioningMode.ContinuousDeployment).Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();
        fixture.BranchTo("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.0", configuration);

        fixture.BranchTo("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-just-a-test.0", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-just-a-test.1", configuration);

        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.0", configuration);

        fixture.MergeNoFF("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.2", configuration);

        fixture.Repository.Branches.Remove("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.2", configuration);

        fixture.Repository.DumpGraph(Console.WriteLine);
    }

    [Test]
    public void ShouldFallbackToTheVersionOnDevelopLikeTheReleaseWasNeverCreatedWhenReleaseHasBeenCanceled()
    {
        // *8f062c7 49 minutes ago(HEAD -> develop)
        // |\  
        // | *bda6ba8 52 minutes ago
        // | *6f5cf19 54 minutes ago
        // * | 3b20f15 50 minutes ago
        // |/
        // *f5640b3 56 minutes ago
        // *2099a07 58 minutes ago(main)

        var configuration = ConfigBuilder.New
            .WithVersioningMode(VersioningMode.ContinuousDeployment)
            .WithoutAnyTrackMergeTargets().Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-ci.1", configuration);

        fixture.BranchTo("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.2", configuration);

        fixture.BranchTo("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.0", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.2", configuration);

        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.0", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);

        fixture.MergeNoFF("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.4", configuration);

        // cancel the release 1.0.0
        fixture.Repository.Branches.Remove("release/1.0.0");

        // ❌ expected: "0.1.0-alpha.6"
        fixture.AssertFullSemver("1.1.0-alpha.4", configuration);

        fixture.Repository.DumpGraph(Console.WriteLine);
    }

    [Test]
    public void ShouldConsiderTheMergeCommitFromMainToDevelopWhenReleaseHasBeenShippedToProduction()
    {
        // *   5d13120 48 minutes ago  (HEAD -> develop)
        // |\  
        // | *   8ddd9b0 49 minutes ago  (tag: 1.0.0, main)
        // | |\  
        // | | * 4b826b8 52 minutes ago 
        // | | * d4b0047 54 minutes ago 
        // * | | 0457671 50 minutes ago 
        // | |/  
        // |/|   
        // * | 5f31f30 56 minutes ago 
        // |/  
        // * 252971e 58 minutes ago

        var configuration = ConfigBuilder.New
            .WithVersioningMode(VersioningMode.ContinuousDeployment)
            .WithoutAnyTrackMergeTargets().Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-ci.1", configuration);

        fixture.BranchTo("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.2", configuration);

        fixture.BranchTo("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.0", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.2", configuration);

        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.0", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);

        fixture.Checkout("main");
        fixture.MergeNoFF("release/1.0.0");

        // ❌ expected: "0.0.1-ci.5"
        fixture.AssertFullSemver("1.0.0-ci.0", configuration);

        fixture.ApplyTag("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0", configuration);

        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);

        fixture.MergeNoFF("main");

        // ❌ expected: "1.1.0-alpha.2"
        fixture.AssertFullSemver("1.1.0-alpha.6", configuration);

        fixture.Repository.Branches.Remove("release/1.0.0");

        // ❌ expected: "1.1.0-alpha.2"
        fixture.AssertFullSemver("1.1.0-alpha.6", configuration);

        fixture.Repository.DumpGraph(Console.WriteLine);
    }
}
