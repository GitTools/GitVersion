using System.Globalization;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
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

        Commands.Fetch(fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Network.Remotes.First().Name, Array.Empty<string>(), new FetchOptions(), null);
        Commands.Checkout(fixture.LocalRepositoryFixture.Repository, fixture.Repository.Head.Tip);
        fixture.LocalRepositoryFixture.Repository.Branches.Remove(MainBranch);
        fixture.InitializeRepo();
        fixture.AssertFullSemver("1.0.1+1");
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

        fixture.AssertFullSemver("0.0.1+1");
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

        Commands.Fetch(fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Network.Remotes.First().Name, Array.Empty<string>(), new FetchOptions(), null);
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
            .WithBranch("main", builder => builder.WithIsMainline(false))
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

    [TestCase(1)]
    [TestCase(2)]
    public void EnsurePreReleaseTagLabelWillBeConsideredIfNoLabelIsDefined(long patchNumber)
    {
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithLabel(null)
            .WithBranch("main", branchBuilder => branchBuilder
                .WithLabel(null).WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1+1", configuration);

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
        fixture.AssertFullSemver($"0.0.{patchNumber + 1}+1", configuration);

        fixture.Repository.DumpGraph();
    }
}
