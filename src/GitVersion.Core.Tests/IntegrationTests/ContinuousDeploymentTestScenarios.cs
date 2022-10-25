using GitTools.Testing;
using GitVersion.Core.Tests.Helpers;
using GitVersion.VersionCalculation;
using NUnit.Framework;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class ContinuousDeploymentTestScenarios
{
    [Test]
    public void ShouldUseTheFallbackVersionOnMainWhenNoVersionsAreAvailable()
    {
        // * 2373a87 58 minutes ago  (HEAD -> main)

        var configuration = GitFlowConfigurationBuilder.New
            .WithVersioningMode(VersioningMode.ContinuousDeployment)
            .WithBranch("main", builder => builder.WithVersioningMode(VersioningMode.ContinuousDeployment))
            .Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-ci.1", configuration);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void ShouldUseTheFallbackVersionOnDevelopWhenNoVersionsAreAvailable()
    {
        // * a831d61 58 minutes ago  (HEAD -> develop)

        var configuration = GitFlowConfigurationBuilder.New.WithVersioningMode(VersioningMode.ContinuousDeployment).Build();

        using var fixture = new EmptyRepositoryFixture("develop");

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.1", configuration);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void ShouldUseConfiguredNextVersionOnMainWhenNoHigherVersionsAvailable()
    {
        // * 8c64db3 58 minutes ago  (HEAD -> main)

        var configuration = GitFlowConfigurationBuilder.New
            .WithNextVersion("1.0.0").WithVersioningMode(VersioningMode.ContinuousDeployment)
            .WithBranch("main", builder => builder.WithVersioningMode(VersioningMode.ContinuousDeployment))
            .Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-ci.1", configuration);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void ShouldNotMatterWhenConfiguredNextVersionIsEqualsToTheTaggeVersion()
    {
        // * 858f71b 58 minutes ago  (HEAD -> main, tag: 1.0.0)

        var configuration = GitFlowConfigurationBuilder.New
            .WithVersioningMode(VersioningMode.ContinuousDeployment)
            .WithNextVersion("1.0.0").Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeATaggedCommit("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0", configuration);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void ShouldUseTaggedVersionWhenGreaterThanConfiguredNextVersion()
    {
        // * ba74727 58 minutes ago  (HEAD -> main, tag: 1.1.0)

        var configuration = GitFlowConfigurationBuilder.New
            .WithVersioningMode(VersioningMode.ContinuousDeployment)
            .WithNextVersion("1.0.0").Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeATaggedCommit("1.1.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0", configuration);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void ShouldCalculateTheCorrectVersionWhenMergingFromMainToFeatureBranch()
    {
        // *94f03f8 55 minutes ago(HEAD -> main)
        // |\  
        // | *b1f41a4 56 minutes ago
        // |/
        // *ec77f9c 58 minutes ago

        var configuration = GitFlowConfigurationBuilder.New
            .WithVersioningMode(VersioningMode.ContinuousDeployment)
            .WithBranch("main", builder => builder.WithVersioningMode(VersioningMode.ContinuousDeployment))
            .WithBranch("feature", builder => builder.WithVersioningMode(VersioningMode.ContinuousDeployment))
            .Build();

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

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void ShouldCalculateTheCorrectVersionWhenMergingFromDevelopToFeatureBranch()
    {
        // *2c475bf 55 minutes ago(HEAD -> develop)
        // |\  
        // | *e05365d 56 minutes ago
        // |/
        // *67acc03 58 minutes ago(main)

        var configuration = GitFlowConfigurationBuilder.New
            .WithVersioningMode(VersioningMode.ContinuousDeployment)
            .WithBranch("develop", builder => builder.WithVersioningMode(VersioningMode.ContinuousDeployment))
            .WithBranch("feature", builder => builder.WithVersioningMode(VersioningMode.ContinuousDeployment))
            .Build();

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

        fixture.Repository.DumpGraph();
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

        var configuration = GitFlowConfigurationBuilder.New
            .WithVersioningMode(VersioningMode.ContinuousDeployment)
            .WithBranch("main", builder => builder.WithVersioningMode(VersioningMode.ContinuousDeployment))
            .WithBranch("release", builder => builder.WithVersioningMode(VersioningMode.ContinuousDeployment))
            .WithBranch("feature", builder => builder.WithVersioningMode(VersioningMode.ContinuousDeployment))
            .Build();

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

        fixture.Repository.DumpGraph();
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

        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithVersioningMode(VersioningMode.ContinuousDeployment).WithTrackMergeTarget(false)
            )
            .WithBranch("develop", builder => builder
                .WithVersioningMode(VersioningMode.ContinuousDeployment).WithTrackMergeTarget(false)
            )
            .WithBranch("release", builder => builder
                .WithVersioningMode(VersioningMode.ContinuousDeployment).WithTrackMergeTarget(false)
            )
            .Build();

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

        // ❔ expected: "0.1.0-alpha.6"
        fixture.AssertFullSemver("1.1.0-alpha.4", configuration);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void ShouldConsiderTheMergeCommitFromMainToDevelopWhenReleaseHasBeenMergedAndTaggedOnMain()
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

        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithVersioningMode(VersioningMode.ContinuousDeployment).WithTrackMergeTarget(false)
            )
            .WithBranch("develop", builder => builder
                .WithVersioningMode(VersioningMode.ContinuousDeployment).WithTrackMergeTarget(false)
            )
            .WithBranch("release", builder => builder
                .WithVersioningMode(VersioningMode.ContinuousDeployment).WithTrackMergeTarget(false)
            )
            .Build();

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

        // ❔ expected: "0.0.1-ci.5"
        fixture.AssertFullSemver("1.0.0-ci.0", configuration);

        fixture.ApplyTag("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0", configuration);

        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);

        fixture.MergeNoFF("main");

        // ❔ expected: "1.1.0-alpha.2"
        fixture.AssertFullSemver("1.1.0-alpha.6", configuration);

        fixture.Repository.Branches.Remove("release/1.0.0");

        // ❔ expected: "1.1.0-alpha.2
        fixture.AssertFullSemver("1.1.0-alpha.6", configuration);

        fixture.Repository.DumpGraph();
    }
}
