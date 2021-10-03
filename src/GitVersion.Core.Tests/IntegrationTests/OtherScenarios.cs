using System.Globalization;
using GitTools.Testing;
using GitVersion.Core.Tests.Helpers;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;

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
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeACommit();
        fixture.Repository.CreateBranch("develop");

        Commands.Fetch((Repository)fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Network.Remotes.First().Name, Array.Empty<string>(), new FetchOptions(), null);
        Commands.Checkout(fixture.LocalRepositoryFixture.Repository, fixture.Repository.Head.Tip);
        fixture.LocalRepositoryFixture.Repository.Branches.Remove(MainBranch);
        fixture.InitializeRepo();
        fixture.AssertFullSemver("1.0.1+1");
    }

    [Test]
    public void AllowNotHavingMain()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
        fixture.Repository.Branches.Remove(fixture.Repository.Branches[MainBranch]);

        fixture.AssertFullSemver("1.1.0-alpha.1");
    }

    [Test]
    public void AllowHavingVariantsStartingWithMaster()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("masterfix"));

        fixture.AssertFullSemver("1.0.1-masterfix.1+1");
    }

    [Test]
    public void AllowHavingMasterInsteadOfMain()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("master"));
        fixture.Repository.Branches.Remove(fixture.Repository.Branches["main"]);

        fixture.AssertFullSemver("0.1.0+0");
    }

    [Test]
    public void AllowHavingVariantsStartingWithMain()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("mainfix"));

        fixture.AssertFullSemver("1.0.1-mainfix.1+1");
    }

    [Test]
    public void DoNotBlowUpWhenDevelopAndFeatureBranchPointAtSameCommit()
    {
        using var fixture = new RemoteRepositoryFixture();
        fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeACommit();
        fixture.Repository.CreateBranch("feature/someFeature");

        Commands.Fetch((Repository)fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Network.Remotes.First().Name, Array.Empty<string>(), new FetchOptions(), null);
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
        fixture.Repository.MakeACommit();

        for (int i = 0; i < numberOfFiles; i++)
        {
            var tempFile = Path.GetTempFileName();
            var repoFile = Path.Combine(fixture.RepositoryPath, Path.GetFileNameWithoutExtension(tempFile) + ".txt");
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
        fixture.Repository.MakeACommit();

        var version = fixture.GetVersion();
        var zero = 0;
        version.UncommittedChanges.ShouldBe(zero.ToString(CultureInfo.InvariantCulture));
    }
}
