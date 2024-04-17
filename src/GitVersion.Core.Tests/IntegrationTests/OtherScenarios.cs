using System.Globalization;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.VersionCalculation;
using LibGit2Sharp;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class OtherScenarios : TestBase
{
    // This is an attempt to automatically resolve the issue where you cannot build
    // when multiple branches point at the same commit
    // Current implementation favors main, then branches without - or / in their name
    [Test]
    public void DoNotBlowUpWhenMainAndDevelopPointAtSameCommit()
    {
        using var fixture = new RemoteRepositoryFixture();
        fixture.MakeACommit();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit();
        fixture.Repository.CreateBranch("develop");

        Commands.Fetch(fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Network.Remotes.First().Name, [], new(), null);
        Commands.Checkout(fixture.LocalRepositoryFixture.Repository, fixture.Repository.Head.Tip);
        fixture.LocalRepositoryFixture.Repository.Branches.Remove(MainBranch);
        fixture.InitializeRepo();
        fixture.AssertFullSemver("1.0.1-1");
    }

    [Test]
    public void AllowNotHavingMain()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit();
        fixture.BranchTo("develop");
        fixture.Repository.Branches.Remove(fixture.Repository.Branches[MainBranch]);

        fixture.AssertFullSemver("1.1.0-alpha.1");
    }

    [Test]
    public void AllowHavingVariantsStartingWithMaster()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit();
        fixture.BranchTo("masterfix");

        fixture.AssertFullSemver("1.0.1-masterfix.1+1");
    }

    [Test]
    public void AllowHavingMasterInsteadOfMain()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit("one");
        fixture.BranchTo("develop");
        fixture.BranchTo("master");
        fixture.Repository.Branches.Remove("main");

        fixture.AssertFullSemver("0.0.1-1");
    }

    [Test]
    public void AllowHavingVariantsStartingWithMain()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit();
        fixture.BranchTo("mainfix");
        fixture.AssertFullSemver("1.0.1-mainfix.1+1");
    }

    [Test]
    public void DoNotBlowUpWhenDevelopAndFeatureBranchPointAtSameCommit()
    {
        using var fixture = new RemoteRepositoryFixture();
        fixture.MakeACommit();
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit();
        fixture.Repository.CreateBranch("feature/someFeature");

        Commands.Fetch(fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Network.Remotes.First().Name, [], new(), null);
        Commands.Checkout(fixture.LocalRepositoryFixture.Repository, fixture.Repository.Head.Tip);
        fixture.LocalRepositoryFixture.Repository.Branches.Remove(MainBranch);
        fixture.InitializeRepo();
        fixture.AssertFullSemver("1.1.0-alpha.1");
    }

    [TestCase(true, 1)]
    [TestCase(false, 1)]
    [TestCase(true, 5)]
    [TestCase(false, 5)]
    public void HasDirtyFlagWhenUncommittedChangesAreInRepo(bool stageFile, int numberOfFiles)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();

        for (int i = 0; i < numberOfFiles; i++)
        {
            var tempFile = Path.GetTempFileName();
            var repoFile = PathHelper.Combine(fixture.RepositoryPath, Path.GetFileNameWithoutExtension(tempFile) + ".txt");
            File.Move(tempFile, repoFile);
            File.WriteAllText(repoFile, $"Hello world / testfile {i}");

            if (stageFile)
                Commands.Stage(fixture.Repository, repoFile);
        }

        var version = fixture.GetVersion();
        version.UncommittedChanges.ShouldBe(numberOfFiles.ToString(CultureInfo.InvariantCulture));
    }

    [Test]
    public void NoDirtyFlagInCleanRepository()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();

        var version = fixture.GetVersion();
        const int zero = 0;
        version.UncommittedChanges.ShouldBe(zero.ToString(CultureInfo.InvariantCulture));
    }

    [TestCase(false, "1.1.0-alpha.2")]
    [TestCase(true, "1.2.0-alpha.1")]
    public void EnsureTrackMergeTargetStrategyWhichWillLookForTaggedMergecommits(bool trackMergeTarget, string expectedVersion)
    {
        // * 9daa6ea 53 minutes ago  (HEAD -> develop)
        // | *   85536f2 55 minutes ago  (tag: 1.1.0, main)
        // | |\
        // | |/
        // |/|
        // * | 4a5ef1a 56 minutes ago
        // |/
        // * c7f68af 58 minutes ago  (tag: 1.0.0)

        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder.WithIsMainBranch(false))
            .WithBranch("develop", builder => builder
                .WithTrackMergeTarget(trackMergeTarget).WithTracksReleaseBranches(false)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.Checkout("main");
        fixture.MergeNoFF("develop");
        fixture.ApplyTag("1.1.0");
        fixture.Checkout("develop");
        fixture.MakeACommit();

        fixture.AssertFullSemver(expectedVersion, configuration);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void EnsurePreReleaseTagLabelWillBeConsideredIfCurrentBranchIsRelease()
    {
        var configuration = GitHubFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture("release/2.0.0");

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        fixture.ApplyTag("2.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.2+0", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.2+1", configuration);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void EnsurePreReleaseTagLabelWillBeConsideredIfNoLabelIsDefined(long patchNumber)
    {
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithLabel(null)
            .WithBranch("main", branchBuilder => branchBuilder
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithLabel(null).WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        fixture.ApplyTag($"0.0.{patchNumber}-alpha.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-alpha.1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-alpha.2+1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-alpha.2+2", configuration);

        fixture.MakeATaggedCommit($"0.0.{patchNumber}-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-beta.1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-beta.2+1", configuration);

        fixture.MakeATaggedCommit($"0.0.{patchNumber}-beta.2");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-beta.2", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-beta.3+1", configuration);

        fixture.ApplyTag($"0.0.{patchNumber}");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber + 1}-1+1", configuration);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void EnsurePreReleaseTagLabelWithInitialTagForPatchNumberOneWillBeConsideredIfNoLabelIsDefined()
    {
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithLabel(null)
            .WithBranch("main", branchBuilder => branchBuilder
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithLabel(null).WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-1+1", configuration);

        fixture.ApplyTag("1.0.1-alpha.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-alpha.1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-alpha.2+1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-alpha.2+2", configuration);

        fixture.MakeATaggedCommit("1.0.1-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-beta.1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-beta.2+1", configuration);

        fixture.MakeATaggedCommit("1.0.1-beta.2");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-beta.2", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-beta.3+1", configuration);

        fixture.ApplyTag("1.0.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.2-1+1", configuration);

        fixture.Repository.DumpGraph();
    }

    [TestCase(2)]
    [TestCase(3)]
    public void EnsurePreReleaseTagLabelWithInitialTagForPatchNumberTwoAndThreeWillBeConsideredIfNoLabelIsDefined(long patchNumber)
    {
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithLabel(null)
            .WithBranch("main", branchBuilder => branchBuilder
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithLabel(null).WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-1+1", configuration);

        fixture.ApplyTag($"1.0.{patchNumber}-alpha.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-alpha.1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-alpha.2+1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-alpha.2+2", configuration);

        fixture.MakeATaggedCommit($"1.0.{patchNumber}-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-beta.1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-beta.2+1", configuration);

        fixture.MakeATaggedCommit($"1.0.{patchNumber}-beta.2");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-beta.2", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-beta.3+1", configuration);

        fixture.ApplyTag($"1.0.{patchNumber}");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber + 1}-1+1", configuration);

        fixture.Repository.DumpGraph();
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void EnsurePreReleaseTagLabelWillBeConsideredIfLabelIsEmpty(long patchNumber)
    {
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithBranch("main", branchBuilder => branchBuilder
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithLabel(string.Empty).WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        fixture.ApplyTag($"0.0.{patchNumber}-alpha.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-1+1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-1+2", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-1+3", configuration);

        fixture.MakeATaggedCommit($"0.0.{patchNumber}-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-1+4", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-1+5", configuration);

        fixture.MakeATaggedCommit($"0.0.{patchNumber}-beta.2");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-1+6", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-1+7", configuration);

        fixture.ApplyTag($"0.0.{patchNumber}");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber + 1}-1+1", configuration);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void EnsurePreReleaseTagLabelWithInitialTagWillBeConsideredIfLabelIsEmpty(long patchNumber)
    {
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithBranch("main", branchBuilder => branchBuilder
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithLabel(string.Empty).WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-1+1", configuration);

        fixture.ApplyTag($"1.0.{patchNumber}-alpha.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-1+1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-1+2", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-1+3", configuration);

        fixture.MakeATaggedCommit($"1.0.{patchNumber}-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-1+4", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-1+5", configuration);

        fixture.MakeATaggedCommit($"1.0.{patchNumber}-beta.2");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-1+6", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-1+7", configuration);

        fixture.ApplyTag($"1.0.{patchNumber}");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber + 1}-1+1", configuration);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void EnsurePreReleaseTagLabelWillBeConsideredIfAlphaLabelIsDefined(long patchNumber)
    {
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithLabel(null)
            .WithBranch("main", branchBuilder => branchBuilder
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithLabel("alpha").WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-alpha.1+1", configuration);

        fixture.ApplyTag($"0.0.{patchNumber}-alpha.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-alpha.1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-alpha.2+1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-alpha.2+2", configuration);

        fixture.MakeATaggedCommit($"0.0.{patchNumber}-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-alpha.2+3", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-alpha.2+4", configuration);

        fixture.MakeATaggedCommit($"0.0.{patchNumber}-beta.2");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-alpha.2+5", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-alpha.2+6", configuration);

        fixture.ApplyTag($"0.0.{patchNumber}");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber + 1}-alpha.1+1", configuration);

        fixture.Repository.DumpGraph();
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void EnsurePreReleaseTagLabelWithInitialTagWillBeConsideredIfAlphaLabelIsDefined(long patchNumber)
    {
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithLabel(null)
            .WithBranch("main", branchBuilder => branchBuilder
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithLabel("alpha").WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-alpha.1+1", configuration);

        fixture.ApplyTag($"1.0.{patchNumber}-alpha.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-alpha.1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-alpha.2+1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-alpha.2+2", configuration);

        fixture.MakeATaggedCommit($"1.0.{patchNumber}-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-alpha.2+3", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-alpha.2+4", configuration);

        fixture.MakeATaggedCommit($"1.0.{patchNumber}-beta.2");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-alpha.2+5", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-alpha.2+6", configuration);

        fixture.ApplyTag($"1.0.{patchNumber}");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber + 1}-alpha.1+1", configuration);

        fixture.Repository.DumpGraph();
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void EnsurePreReleaseTagLabelWillBeConsideredIfBetaLabelIsDefined(long patchNumber)
    {
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithBranch("main", branchBuilder => branchBuilder
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithLabel("beta").WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+1", configuration);

        fixture.ApplyTag($"0.0.{patchNumber}-alpha.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-beta.1+1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-beta.1+2", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-beta.1+3", configuration);

        fixture.MakeATaggedCommit($"0.0.{patchNumber}-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-beta.1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-beta.2+1", configuration);

        fixture.MakeATaggedCommit($"0.0.{patchNumber}-beta.2");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-beta.2", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-beta.3+1", configuration);

        fixture.ApplyTag($"0.0.{patchNumber}");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber + 1}-beta.1+1", configuration);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void EnsurePreReleaseTagLabelWithInitialTagWillBeConsideredIfBetaLabelIsDefined(long patchNumber)
    {
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithBranch("main", branchBuilder => branchBuilder
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithLabel("beta").WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-beta.1+1", configuration);

        fixture.ApplyTag($"1.0.{patchNumber}-alpha.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-beta.1+1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-beta.1+2", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-beta.1+3", configuration);

        fixture.MakeATaggedCommit($"1.0.{patchNumber}-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-beta.1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-beta.2+1", configuration);

        fixture.MakeATaggedCommit($"1.0.{patchNumber}-beta.2");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-beta.2", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-beta.3+1", configuration);

        fixture.ApplyTag($"1.0.{patchNumber}");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber + 1}-beta.1+1", configuration);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void EnsurePreReleaseTagLabelWillBeConsideredIfGammaLabelIsDefined(long patchNumber)
    {
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithBranch("main", branchBuilder => branchBuilder
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithLabel("gamma").WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-gamma.1+1", configuration);

        fixture.ApplyTag($"0.0.{patchNumber}-alpha.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-gamma.1+1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-gamma.1+2", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-gamma.1+3", configuration);

        fixture.MakeATaggedCommit($"0.0.{patchNumber}-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-gamma.1+4", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-gamma.1+5", configuration);

        fixture.MakeATaggedCommit($"0.0.{patchNumber}-beta.2");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-gamma.1+6", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}-gamma.1+7", configuration);

        fixture.ApplyTag($"0.0.{patchNumber}");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber}", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"0.0.{patchNumber + 1}-gamma.1+1", configuration);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void EnsurePreReleaseTagLabelWithInitialTagWillBeConsideredIfGammaLabelIsDefined(long patchNumber)
    {
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithBranch("main", branchBuilder => branchBuilder
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithLabel("gamma").WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-gamma.1+1", configuration);

        fixture.ApplyTag($"1.0.{patchNumber}-alpha.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-gamma.1+1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-gamma.1+2", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-gamma.1+3", configuration);

        fixture.MakeATaggedCommit($"1.0.{patchNumber}-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-gamma.1+4", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-gamma.1+5", configuration);

        fixture.MakeATaggedCommit($"1.0.{patchNumber}-beta.2");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-gamma.1+6", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}-gamma.1+7", configuration);

        fixture.ApplyTag($"1.0.{patchNumber}");

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber}", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver($"1.0.{patchNumber + 1}-gamma.1+1", configuration);
    }

    [TestCase(null)]
    [TestCase("")]
    public void IncreaseVersionWithBumpMessageWhenCommitMessageIncrementIsEnabledAndIncrementStrategyIsNoneForBranchWithNoLabel(string? label)
    {
        var configuration = GitFlowConfigurationBuilder.New.WithLabel(null)
            .WithBranch("main", _ => _
                .WithCommitMessageIncrementing(CommitMessageIncrementMode.Enabled)
                .WithDeploymentMode(DeploymentMode.ContinuousDelivery)
                .WithIncrement(IncrementStrategy.None)
                .WithLabel(label)
                .WithIsMainBranch(false)
            )
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1", configuration);

        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-2", configuration);

        fixture.ApplyTag("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0", configuration);

        fixture.MakeACommit("+semver: major");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1", configuration);

        fixture.ApplyTag("2.0.0");
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1", configuration);
    }

    [Test]
    public void IncreaseVersionWithBumpMessageWhenCommitMessageIncrementIsEnabledAndIncrementStrategyIsNoneForBranchWithAlphaLabel()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", _ => _
                .WithCommitMessageIncrementing(CommitMessageIncrementMode.Enabled)
                .WithDeploymentMode(DeploymentMode.ContinuousDelivery)
                .WithIncrement(IncrementStrategy.None)
                .WithLabel("pre")
                .WithIsMainBranch(false)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-pre.1", configuration);

        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-pre.2", configuration);

        fixture.ApplyTag("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0", configuration);

        fixture.MakeACommit("+semver: major");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-pre.1", configuration);

        fixture.ApplyTag("2.0.0");
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-pre.1", configuration);
    }

    [Test]
    public void ShouldProvideTheCorrectVersionEvenIfPreReleaseLabelExistsInTheGitTagMain()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithNextVersion("5.0")
            .WithSemanticVersionFormat(SemanticVersionFormat.Loose)
            .WithBranch("main", _ => _
                .WithLabel("beta")
                .WithIncrement(IncrementStrategy.Patch)
                .WithDeploymentMode(DeploymentMode.ContinuousDelivery)
                .WithIsMainBranch(false)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("5.0.0-beta.1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("5.0.0-beta.2", configuration);

        fixture.ApplyTag("5.0.0-beta.3");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("5.0.0-beta.3", configuration);

        fixture.MakeATaggedCommit("5.0.0-rc.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("5.0.0-beta.4", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("5.0.0-beta.5", configuration);
    }

    /// <summary>
    /// https://github.com/GitTools/GitVersion/issues/2347
    /// </summary>
    [Test]
    public void EnsureThePreReleaseTagIsCorrectlyGeneratedWhenPreReleaseLabelIsEmpty()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", _ => _
                .WithLabel(string.Empty).WithIsMainBranch(false)
                .WithDeploymentMode(DeploymentMode.ContinuousDelivery)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeCommits(5);

        var _ = fixture.GetVersion(configuration);

        fixture.AssertFullSemver("0.0.1-5", configuration);
    }

    [TestCase("0.0.1-alpha.2", true, "0.0.1-alpha.2")]
    [TestCase("0.0.1-alpha.2", false, "0.0.1-alpha.3+0")]
    [TestCase("0.1.0-alpha.2", true, "0.1.0-alpha.2")]
    [TestCase("0.1.0-alpha.2", false, "0.1.0-alpha.3+0")]
    [TestCase("0.0.1", true, "0.0.1")]
    [TestCase("0.0.1", false, "0.1.0-alpha.1+0")]
    [TestCase("0.0.1-beta.2", true, "0.1.0-alpha.1+1")]
    [TestCase("0.0.1-beta.2", false, "0.1.0-alpha.1+1")]
    [TestCase("0.1.0-beta.2", true, "0.1.0-alpha.1+1")]
    [TestCase("0.1.0-beta.2", false, "0.1.0-alpha.1+1")]
    [TestCase("0.2.0-beta.2", true, "0.2.0-alpha.1+1")]
    [TestCase("0.2.0-beta.2", false, "0.2.0-alpha.1+1")]
    public void EnsurePreventIncrementWhenCurrentCommitTaggedOnDevelopWithDeploymentModeManualDeployment(
        string tag, bool preventIncrementWhenCurrentCommitTagged, string version)
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("develop", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithPreventIncrementWhenCurrentCommitTagged(preventIncrementWhenCurrentCommitTagged)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit("A");
        if (!tag.IsNullOrEmpty()) fixture.ApplyTag(tag);
        fixture.BranchTo("develop");

        fixture.AssertFullSemver(version, configuration);
    }

    [TestCase("0.0.1-alpha.2", true, "0.0.1-alpha.2")]
    [TestCase("0.0.1-alpha.2", false, "0.0.1-alpha.2")]
    [TestCase("0.1.0-alpha.2", true, "0.1.0-alpha.2")]
    [TestCase("0.1.0-alpha.2", false, "0.1.0-alpha.2")]
    [TestCase("0.0.1", true, "0.0.1")]
    [TestCase("0.0.1", false, "0.1.0-alpha.0")]
    [TestCase("0.0.1-beta.2", true, "0.1.0-alpha.1")]
    [TestCase("0.0.1-beta.2", false, "0.1.0-alpha.1")]
    [TestCase("0.1.0-beta.2", true, "0.1.0-alpha.1")]
    [TestCase("0.1.0-beta.2", false, "0.1.0-alpha.1")]
    [TestCase("0.2.0-beta.2", true, "0.2.0-alpha.1")]
    [TestCase("0.2.0-beta.2", false, "0.2.0-alpha.1")]
    public void EnsurePreventIncrementWhenCurrentCommitTaggedOnDevelopWithDeploymentModeContinuousDelivery(
        string tag, bool preventIncrementWhenCurrentCommitTagged, string version)
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("develop", _ => _
                .WithDeploymentMode(DeploymentMode.ContinuousDelivery)
                .WithPreventIncrementWhenCurrentCommitTagged(preventIncrementWhenCurrentCommitTagged)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit("A");
        if (!tag.IsNullOrEmpty()) fixture.ApplyTag(tag);
        fixture.BranchTo("develop");

        fixture.AssertFullSemver(version, configuration);
    }

    [TestCase("0.0.1-alpha.2", true, "0.0.1")]
    [TestCase("0.0.1-alpha.2", false, "0.0.1")]
    [TestCase("0.1.0-alpha.2", true, "0.1.0")]
    [TestCase("0.1.0-alpha.2", false, "0.1.0")]
    [TestCase("0.0.1", true, "0.0.1")]
    [TestCase("0.0.1", false, "0.1.0")]
    [TestCase("0.0.1-beta.2", true, "0.1.0")]
    [TestCase("0.0.1-beta.2", false, "0.1.0")]
    [TestCase("0.1.0-beta.2", true, "0.1.0")]
    [TestCase("0.1.0-beta.2", false, "0.1.0")]
    [TestCase("0.2.0-beta.2", true, "0.2.0")]
    [TestCase("0.2.0-beta.2", false, "0.2.0")]
    public void EnsurePreventIncrementWhenCurrentCommitTaggedOnDevelopWithDeploymentModeContinuousDeployment(
        string tag, bool preventIncrementWhenCurrentCommitTagged, string version)
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("develop", _ => _
                .WithDeploymentMode(DeploymentMode.ContinuousDeployment)
                .WithPreventIncrementWhenCurrentCommitTagged(preventIncrementWhenCurrentCommitTagged)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit("A");
        if (!tag.IsNullOrEmpty()) fixture.ApplyTag(tag);
        fixture.BranchTo("develop");

        fixture.AssertFullSemver(version, configuration);
    }

    [TestCase(true, "1.0.0")]
    [TestCase(false, "6.0.0-alpha.1+0")]
    public void EnsurePreventIncrementWhenCurrentCommitTaggedOnDevelopWithNextVersion(bool preventIncrementWhenCurrentCommitTagged, string semVersion)
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithNextVersion("6.0.0")
            .WithBranch("develop", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithPreventIncrementWhenCurrentCommitTagged(preventIncrementWhenCurrentCommitTagged)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("develop");

        fixture.AssertFullSemver(semVersion, configuration);
    }

    [TestCase(null, true, "6.0.0-beta.1+1")]
    [TestCase(null, false, "6.0.0-beta.1+1")]
    [TestCase(new[] { "5.0.0" }, true, "5.0.0")]
    [TestCase(new[] { "5.0.0" }, false, "6.0.0-beta.1+0")]
    [TestCase(new[] { "6.0.0" }, true, "6.0.0")]
    [TestCase(new[] { "6.0.0" }, false, "6.1.0-beta.1+0")]
    [TestCase(new[] { "7.0.0" }, true, "7.0.0")]
    [TestCase(new[] { "7.0.0" }, false, "7.1.0-beta.1+0")]
    [TestCase(new[] { "5.0.0-alpha.2" }, true, "6.0.0-beta.1+1")]
    [TestCase(new[] { "5.0.0-alpha.2" }, false, "6.0.0-beta.1+1")]
    [TestCase(new[] { "6.0.0-alpha.2" }, true, "6.0.0-beta.1+1")]
    [TestCase(new[] { "6.0.0-alpha.2" }, false, "6.0.0-beta.1+1")]
    [TestCase(new[] { "7.0.0-alpha.2" }, true, "7.0.0-beta.1+1")]
    [TestCase(new[] { "7.0.0-alpha.2" }, false, "7.0.0-beta.1+1")]
    [TestCase(new[] { "5.0.0-beta.2" }, true, "5.0.0-beta.2")]
    [TestCase(new[] { "5.0.0-beta.2" }, false, "6.0.0-beta.1+0")]
    [TestCase(new[] { "6.0.0-beta.2" }, true, "6.0.0-beta.2")]
    [TestCase(new[] { "6.0.0-beta.2" }, false, "6.0.0-beta.3+0")]
    [TestCase(new[] { "7.0.0-beta.2" }, true, "7.0.0-beta.2")]
    [TestCase(new[] { "7.0.0-beta.2" }, false, "7.0.0-beta.3+0")]
    [TestCase(new[] { "5.0.0", "6.0.0" }, true, "6.0.0")]
    [TestCase(new[] { "5.0.0", "6.0.0" }, false, "6.1.0-beta.1+0")]
    [TestCase(new[] { "6.0.0", "5.0.0" }, true, "6.0.0")]
    [TestCase(new[] { "6.0.0", "5.0.0" }, false, "6.1.0-beta.1+0")]
    [TestCase(new[] { "6.0.0", "7.0.0" }, true, "7.0.0")]
    [TestCase(new[] { "6.0.0", "7.0.0" }, false, "7.1.0-beta.1+0")]
    [TestCase(new[] { "7.0.0", "6.0.0" }, true, "7.0.0")]
    [TestCase(new[] { "7.0.0", "6.0.0" }, false, "7.1.0-beta.1+0")]
    [TestCase(new[] { "4.0.0", "5.0.0-alpha.2" }, true, "4.0.0")]
    [TestCase(new[] { "4.0.0", "5.0.0-alpha.2" }, false, "6.0.0-beta.1+0")]
    [TestCase(new[] { "5.0.0-alpha.2", "4.0.0" }, true, "4.0.0")]
    [TestCase(new[] { "5.0.0-alpha.2", "4.0.0" }, false, "6.0.0-beta.1+0")]
    [TestCase(new[] { "4.0.0", "5.0.0-beta.2" }, true, "5.0.0-beta.2")]
    [TestCase(new[] { "4.0.0", "5.0.0-beta.2" }, false, "6.0.0-beta.1+0")]
    [TestCase(new[] { "5.0.0-beta.2", "4.0.0" }, true, "5.0.0-beta.2")]
    [TestCase(new[] { "5.0.0-beta.2", "4.0.0" }, false, "6.0.0-beta.1+0")]
    [TestCase(new[] { "4.0.0-alpha.2", "5.0.0-beta.2" }, true, "5.0.0-beta.2")]
    [TestCase(new[] { "4.0.0-alpha.2", "5.0.0-beta.2" }, false, "6.0.0-beta.1+0")]
    [TestCase(new[] { "5.0.0-beta.2", "4.0.0-alpha.2" }, true, "5.0.0-beta.2")]
    [TestCase(new[] { "5.0.0-beta.2", "4.0.0-alpha.2" }, false, "6.0.0-beta.1+0")]
    public void EnsurePreventIncrementWhenCurrentCommitTaggedOnReleaseBranchAndIncrementMinor(
        string[]? tags, bool preventIncrementWhenCurrentCommitTagged, string semVersion)
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("release", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithPreventIncrementWhenCurrentCommitTagged(preventIncrementWhenCurrentCommitTagged)
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();

        if (tags is not null)
        {
            foreach (string tag in tags)
            {
                fixture.ApplyTag(tag);
            }
        }

        fixture.BranchTo("release/6.0.0");

        fixture.AssertFullSemver(semVersion, configuration);
    }

    [Test]
    public void EnsureVersionAfterMainIsMergedBackToDevelopIsCorrect()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("develop");
        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.1");

        fixture.Checkout("main");
        fixture.MakeACommit("B");
        fixture.BranchTo("hotfix/just-a-hotfix");
        fixture.MakeACommit("C +semver: major");
        fixture.MergeTo("main", removeBranchAfterMerging: true);
        fixture.Checkout("develop");
        fixture.MakeACommit("D");
        fixture.Checkout("main");
        fixture.MakeACommit("E");
        fixture.ApplyTag("1.0.1");
        fixture.Checkout("develop");
        fixture.MergeNoFF("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.3");
    }

    [TestCase(false, "2.0.0-alpha.2")]
    [TestCase(true, "2.0.0-alpha.2")]
    public void EnsureVersionAfterMainIsMergedBackToDevelopIsCorrectForTrunkBased(bool applyTag, string semanticVersion)
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithVersionStrategy(VersionStrategies.TrunkBased)
            .Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");
        fixture.ApplyTag("1.0.0");
        fixture.BranchTo("develop");
        fixture.MakeACommit("B +semver: major");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-alpha.1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");
        if (applyTag) fixture.ApplyTag("1.0.1");
        fixture.Checkout("develop");
        fixture.MergeNoFF("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver(semanticVersion, configuration);
    }

    [TestCase(false, "2.0.0-alpha.3")]
    [TestCase(true, "2.0.0-alpha.3")]
    public void EnsureVersionAfterMainIsMergedBackToDevelopIsCorrectForGitFlow(bool applyTag, string semanticVersion)
    {
        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");
        fixture.ApplyTag("1.0.0");
        fixture.BranchTo("develop");
        fixture.MakeACommit("B +semver: major");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-alpha.1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");
        if (applyTag) fixture.ApplyTag("1.0.1");
        fixture.Checkout("develop");
        fixture.MergeNoFF("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver(semanticVersion, configuration);
    }
}
