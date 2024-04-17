using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using LibGit2Sharp;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class ReleaseBranchScenarios : TestBase
{
    [Test]
    public void NoMergeBacksToDevelopInCaseThereAreNoChangesInReleaseBranch()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.BranchTo("develop");
        fixture.Repository.MakeCommits(3);
        var releaseBranch = fixture.Repository.CreateBranch("release/1.0.0");
        fixture.Checkout(MainBranch);
        fixture.Repository.MergeNoFF("release/1.0.0");
        fixture.Repository.ApplyTag("1.0.0");
        fixture.Checkout("develop");

        fixture.Repository.Branches.Remove(releaseBranch);

        fixture.AssertFullSemver("1.1.0-alpha.0");
    }

    [Test]
    public void NoMergeBacksToDevelopInCaseThereAreChangesInReleaseBranch()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.BranchTo("develop");
        fixture.Repository.MakeCommits(3);
        fixture.BranchTo("release/1.0.0");
        fixture.Repository.MakeACommit();

        // Merge to main
        fixture.Checkout(MainBranch);
        fixture.Repository.MergeNoFF("release/1.0.0");
        fixture.Repository.ApplyTag("1.0.0");

        // Merge to develop
        fixture.Checkout("develop");
        fixture.AssertFullSemver("1.1.0-alpha.0");
        fixture.Repository.MergeNoFF("release/1.0.0");
        fixture.AssertFullSemver("1.1.0-alpha.1");
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("1.1.0-alpha.2");
        fixture.Repository.Branches.Remove("release/1.0.0");
        fixture.AssertFullSemver("1.1.0-alpha.2");
    }

    [Test]
    public void CanTakeVersionFromReleaseBranch()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.3");
        fixture.Repository.MakeCommits(5);
        fixture.Repository.CreateBranch("release-2.0.0");
        fixture.Checkout("release-2.0.0");

        fixture.AssertFullSemver("2.0.0-beta.1+5");
        fixture.Repository.MakeCommits(2);
        fixture.AssertFullSemver("2.0.0-beta.1+7");
    }

    [Test]
    public void CanTakeVersionFromReleasesBranch()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.3");
        fixture.Repository.MakeCommits(5);
        fixture.Repository.CreateBranch("releases/2.0.0");
        fixture.Checkout("releases/2.0.0");

        fixture.AssertFullSemver("2.0.0-beta.1+5");
        fixture.Repository.MakeCommits(2);
        fixture.AssertFullSemver("2.0.0-beta.1+7");
    }

    [Test]
    public void CanTakePreReleaseVersionFromReleasesBranchWithNumericPreReleaseTag()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeCommits(5);
        fixture.Repository.CreateBranch("releases/2.0.0");
        fixture.Checkout("releases/2.0.0");
        fixture.Repository.ApplyTag("v2.0.0-beta.1");

        var variables = fixture.GetVersion();
        Assert.That(variables.FullSemVer, Is.EqualTo("2.0.0-beta.2+0"));
    }

    [Test]
    public void ReleaseBranchWithNextVersionSetInConfig()
    {
        var configuration = GitFlowConfigurationBuilder.New.WithNextVersion("2.0.0").Build();
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeCommits(5);
        fixture.BranchTo("release-2.0.0");

        fixture.AssertFullSemver("2.0.0-beta.1+5", configuration);
        fixture.Repository.MakeCommits(2);
        fixture.AssertFullSemver("2.0.0-beta.1+7", configuration);
    }

    [Test]
    public void CanTakeVersionFromReleaseBranchWithLabelOverridden()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("release", builder => builder.WithLabel("rc"))
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.3");
        fixture.Repository.MakeCommits(5);
        fixture.Repository.CreateBranch("release-2.0.0");
        fixture.Checkout("release-2.0.0");

        fixture.AssertFullSemver("2.0.0-rc.1+5", configuration);
        fixture.Repository.MakeCommits(2);
        fixture.AssertFullSemver("2.0.0-rc.1+7", configuration);
    }

    [Test]
    public void CanHandleReleaseBranchWithStability()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.3");
        fixture.Repository.CreateBranch("develop");
        fixture.Repository.MakeCommits(5);
        fixture.Repository.CreateBranch("release-2.0.0-Final");
        fixture.Checkout("release-2.0.0-Final");

        fixture.AssertFullSemver("2.0.0-beta.1+5");
        fixture.Repository.MakeCommits(2);
        fixture.AssertFullSemver("2.0.0-beta.1+7");
    }

    [Test]
    public void WhenReleaseBranchOffDevelopIsMergedIntoMainAndDevelopVersionIsTakenWithIt()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.3");
        fixture.Repository.CreateBranch("develop");
        fixture.Repository.MakeCommits(1);

        fixture.Repository.CreateBranch("release-2.0.0");
        fixture.Checkout("release-2.0.0");
        fixture.Repository.MakeCommits(4);
        fixture.Checkout(MainBranch);
        fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

        fixture.AssertFullSemver("2.0.0-6");
        fixture.Repository.MakeCommits(2);
        fixture.AssertFullSemver("2.0.0-8");
    }

    [Test]
    public void WhenReleaseBranchOffMainIsMergedIntoMainVersionIsTakenWithIt()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.3");
        fixture.Repository.MakeCommits(1);
        fixture.Repository.CreateBranch("release-2.0.0");
        fixture.Checkout("release-2.0.0");
        fixture.Repository.MakeCommits(4);
        fixture.Checkout(MainBranch);
        fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

        fixture.AssertFullSemver("2.0.0-6");
    }

    [Test]
    public void MainVersioningContinuousCorrectlyAfterMergingReleaseBranch()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.3");
        fixture.Repository.MakeCommits(1);
        fixture.Repository.CreateBranch("release-2.0.0");
        fixture.Checkout("release-2.0.0");
        fixture.Repository.MakeCommits(4);
        fixture.Checkout(MainBranch);
        fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

        fixture.AssertFullSemver("2.0.0-6");
        fixture.Repository.Branches.Remove("release-2.0.0");
        fixture.AssertFullSemver("2.0.0-6");
        fixture.Repository.ApplyTag("2.0.0");
        fixture.Repository.MakeCommits(1);
        fixture.AssertFullSemver("2.0.1-1");
    }

    [Test]
    public void WhenReleaseBranchIsMergedIntoDevelopHighestVersionIsTakenWithIt()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.3");
        fixture.Repository.CreateBranch("develop");
        fixture.Repository.MakeCommits(1);

        fixture.Repository.CreateBranch("release-2.0.0");
        fixture.Checkout("release-2.0.0");
        fixture.Repository.MakeCommits(4);
        fixture.Checkout("develop");
        fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

        fixture.Repository.CreateBranch("release-1.0.0");
        fixture.Checkout("release-1.0.0");
        fixture.Repository.MakeCommits(4);
        fixture.Checkout("develop");
        fixture.Repository.MergeNoFF("release-1.0.0", Generate.SignatureNow());

        fixture.AssertFullSemver("2.1.0-alpha.11");
    }

    [Test]
    public void WhenReleaseBranchIsMergedIntoMainHighestVersionIsTakenWithIt()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.3");
        fixture.Repository.MakeCommits(1);

        fixture.Repository.CreateBranch("release-2.0.0");
        fixture.Checkout("release-2.0.0");
        fixture.Repository.MakeCommits(4);
        fixture.Checkout(MainBranch);
        fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

        fixture.Repository.CreateBranch("release-1.0.0");
        fixture.Checkout("release-1.0.0");
        fixture.Repository.MakeCommits(4);
        fixture.Checkout(MainBranch);
        fixture.Repository.MergeNoFF("release-1.0.0", Generate.SignatureNow());

        fixture.AssertFullSemver("2.0.0-11");
    }

    [Test]
    public void WhenReleaseBranchIsMergedIntoMainHighestVersionIsTakenWithItEvenWithMoreThanTwoActiveBranches()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.3");
        fixture.Repository.MakeCommits(1);

        fixture.Repository.CreateBranch("release-3.0.0");
        fixture.Checkout("release-3.0.0");
        fixture.Repository.MakeCommits(4);
        fixture.Checkout(MainBranch);
        fixture.Repository.MergeNoFF("release-3.0.0", Generate.SignatureNow());

        fixture.Repository.CreateBranch("release-2.0.0");
        fixture.Checkout("release-2.0.0");
        fixture.Repository.MakeCommits(4);
        fixture.Checkout(MainBranch);
        fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

        fixture.Repository.CreateBranch("release-1.0.0");
        fixture.Checkout("release-1.0.0");
        fixture.Repository.MakeCommits(4);
        fixture.Checkout(MainBranch);
        fixture.Repository.MergeNoFF("release-1.0.0", Generate.SignatureNow());

        fixture.AssertFullSemver("3.0.0-16");
    }

    [Test]
    public void WhenMergingReleaseBackToDevShouldNotResetBetaVersion()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.Repository.CreateBranch("develop");
        fixture.Checkout("develop");

        fixture.Repository.MakeCommits(1);
        fixture.AssertFullSemver("1.1.0-alpha.1");

        fixture.Repository.CreateBranch("release-2.0.0");
        fixture.Checkout("release-2.0.0");
        fixture.Repository.MakeCommits(1);

        fixture.AssertFullSemver("2.0.0-beta.1+2");

        //tag it to bump to beta 2
        fixture.Repository.ApplyTag("2.0.0-beta1");

        fixture.Repository.MakeCommits(1);

        fixture.AssertFullSemver("2.0.0-beta.2+1");

        //merge down to develop
        fixture.Checkout("develop");
        fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

        //but keep working on the release
        fixture.Checkout("release-2.0.0");
        fixture.AssertFullSemver("2.0.0-beta.2+1");
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.0.0-beta.2+2");
    }

    [Test]
    public void HotfixOffReleaseBranchShouldNotResetCount()
    {
        var configuration = GitFlowConfigurationBuilder.New.Build();
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.Repository.CreateBranch("develop");
        fixture.Checkout("develop");

        fixture.Repository.MakeCommits(1);

        fixture.Repository.CreateBranch("release-2.0.0");
        fixture.Checkout("release-2.0.0");
        fixture.Repository.MakeCommits(1);

        fixture.AssertFullSemver("2.0.0-beta.1+2", configuration);

        //tag it to bump to beta 2
        fixture.Repository.MakeCommits(4);

        fixture.AssertFullSemver("2.0.0-beta.1+6", configuration);

        //merge down to develop
        fixture.Repository.CreateBranch("hotfix-2.0.0");
        fixture.Repository.MakeCommits(2);

        //but keep working on the release
        fixture.Checkout("release-2.0.0");
        fixture.Repository.MergeNoFF("hotfix-2.0.0", Generate.SignatureNow());
        fixture.Repository.Branches.Remove(fixture.Repository.Branches["hotfix-2.0.0"]);
        fixture.AssertFullSemver("2.0.0-beta.1+8", configuration);
    }

    [Test]
    public void MergeOnReleaseBranchShouldNotResetCount()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithAssemblyVersioningScheme(AssemblyVersioningScheme.MajorMinorPatchTag)
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.Repository.CreateBranch("develop");
        fixture.Checkout("develop");
        fixture.Repository.MakeACommit();

        fixture.Repository.CreateBranch("release/2.0.0");

        fixture.Repository.CreateBranch("release/2.0.0-xxx");
        fixture.Checkout("release/2.0.0-xxx");
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("2.0.0-beta.1+2", configuration);

        fixture.Checkout("release/2.0.0");
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("2.0.0-beta.1+2", configuration);

        fixture.Repository.MergeNoFF("release/2.0.0-xxx");
        fixture.AssertFullSemver("2.0.0-beta.1+4", configuration);
    }

    [Test]
    public void CommitOnDevelopAfterReleaseBranchMergeToDevelopShouldNotResetCount()
    {
        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit("initial");
        fixture.BranchTo("develop");

        // Create release from develop
        fixture.BranchTo("release-2.0.0");
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        // Make some commits on release
        fixture.MakeACommit("release 1");
        fixture.MakeACommit("release 2");
        fixture.AssertFullSemver("2.0.0-beta.1+3", configuration);

        // First forward merge release to develop
        fixture.Checkout("develop");
        fixture.MergeNoFF("release-2.0.0");

        // Make some new commit on release
        fixture.Checkout("release-2.0.0");
        fixture.Repository.MakeACommit("release 3 - after first merge");
        fixture.AssertFullSemver("2.0.0-beta.1+4", configuration);

        // Make new commit on develop
        fixture.Checkout("develop");
        // Checkout to release (no new commits)
        fixture.Checkout("release-2.0.0");
        fixture.AssertFullSemver("2.0.0-beta.1+4", configuration);
        fixture.Checkout("develop");
        fixture.Repository.MakeACommit("develop after merge");

        // Checkout to release (no new commits)
        fixture.Checkout("release-2.0.0");
        fixture.AssertFullSemver("2.0.0-beta.1+4", configuration);

        // Make some new commit on release
        fixture.Repository.MakeACommit("release 4");
        fixture.Repository.MakeACommit("release 5");
        fixture.AssertFullSemver("2.0.0-beta.1+6", configuration);

        // Second merge release to develop
        fixture.Checkout("develop");
        fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

        // Checkout to release (no new commits)
        fixture.Checkout("release-2.0.0");
        fixture.AssertFullSemver("2.0.0-beta.1+6", configuration);
    }

    [Test]
    public void CommitBeetweenMergeReleaseToDevelopShouldNotResetCount()
    {
        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit("initial");
        fixture.Repository.CreateBranch("develop");
        Commands.Checkout(fixture.Repository, "develop");
        fixture.Repository.CreateBranch("release-2.0.0");
        Commands.Checkout(fixture.Repository, "release-2.0.0");
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        // Make some commits on release
        var commit1 = fixture.Repository.MakeACommit();
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("2.0.0-beta.1+3", configuration);

        // Merge release to develop - emulate commit between other person release commit push and this commit merge to develop
        Commands.Checkout(fixture.Repository, "develop");
        fixture.Repository.Merge(commit1, Generate.SignatureNow(), new() { FastForwardStrategy = FastForwardStrategy.NoFastForward });
        fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

        // Check version on release after merge to develop
        Commands.Checkout(fixture.Repository, "release-2.0.0");
        fixture.AssertFullSemver("2.0.0-beta.1+3", configuration);

        // Check version on release after making some new commits
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("2.0.0-beta.1+5", configuration);
    }

    [Test]
    public void ReleaseBranchShouldUseBranchNameVersionDespiteBumpInPreviousCommit()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithSemanticVersionFormat(SemanticVersionFormat.Loose)
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0");
        fixture.Repository.MakeACommit("+semver:major");
        fixture.Repository.MakeACommit();

        fixture.BranchTo("release/2.0");

        fixture.AssertFullSemver("2.0.0-beta.1+2", configuration);
    }

    [Test]
    public void ReleaseBranchWithACommitShouldUseBranchNameVersionDespiteBumpInPreviousCommit()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeACommit("+semver:major");
        fixture.Repository.MakeACommit();

        fixture.BranchTo("release/2.0.0");

        fixture.Repository.MakeACommit();

        fixture.AssertFullSemver("2.0.0-beta.1+3");
    }

    [Test]
    public void ReleaseBranchedAtCommitWithSemverMessageShouldUseBranchNameVersion()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeACommit("+semver:major");

        fixture.BranchTo("release/2.0.0");

        fixture.AssertFullSemver("2.0.0-beta.1+1");
    }

    [Test]
    public void FeatureFromReleaseBranchShouldNotResetCount()
    {
        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit("initial");
        fixture.Repository.CreateBranch("develop");
        Commands.Checkout(fixture.Repository, "develop");
        fixture.Repository.CreateBranch("release-2.0.0");
        Commands.Checkout(fixture.Repository, "release-2.0.0");
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        // Make some commits on release
        fixture.Repository.MakeCommits(10);
        fixture.AssertFullSemver("2.0.0-beta.1+11", configuration);

        // Create feature from release
        fixture.BranchTo("feature/xxx");
        fixture.Repository.MakeACommit("feature 1");
        fixture.Repository.MakeACommit("feature 2");

        // Check version on release
        Commands.Checkout(fixture.Repository, "release-2.0.0");
        fixture.AssertFullSemver("2.0.0-beta.1+11", configuration);
        fixture.Repository.MakeACommit("release 11");
        fixture.AssertFullSemver("2.0.0-beta.1+12", configuration);

        // Make new commit on feature
        Commands.Checkout(fixture.Repository, "feature/xxx");
        fixture.Repository.MakeACommit("feature 3");

        // Checkout to release (no new commits)
        Commands.Checkout(fixture.Repository, "release-2.0.0");
        fixture.AssertFullSemver("2.0.0-beta.1+12", configuration);

        // Merge feature to release
        fixture.Repository.MergeNoFF("feature/xxx", Generate.SignatureNow());
        fixture.AssertFullSemver("2.0.0-beta.1+16", configuration);

        fixture.Repository.MakeACommit("release 13 - after feature merge");
        fixture.AssertFullSemver("2.0.0-beta.1+17", configuration);
    }

    [Test]
    public void AssemblySemFileVerShouldBeWeightedByPreReleaseWeight()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithAssemblyFileVersioningFormat("{Major}.{Minor}.{Patch}.{WeightedPreReleaseNumber}")
            .WithBranch("release", builder => builder.WithPreReleaseWeight(1000))
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.3");
        fixture.Repository.MakeCommits(5);
        fixture.Repository.CreateBranch("release-2.0.0");
        fixture.Checkout("release-2.0.0");

        var variables = fixture.GetVersion(configuration);
        Assert.That(variables.AssemblySemFileVer, Is.EqualTo("2.0.0.1001"));
    }

    [Test]
    public void AssemblySemFileVerShouldBeWeightedByDefaultPreReleaseWeight()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithAssemblyFileVersioningFormat("{Major}.{Minor}.{Patch}.{WeightedPreReleaseNumber}")
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.3");
        fixture.Repository.MakeCommits(5);
        fixture.Repository.CreateBranch("release-2.0.0");
        fixture.Checkout("release-2.0.0");
        var variables = fixture.GetVersion(configuration);
        Assert.That(variables.AssemblySemFileVer, Is.EqualTo("2.0.0.30001"));
    }

    /// <summary>
    /// Create a feature branch from a release branch, and merge back, then delete it
    /// </summary>
    [Test]
    public void FeatureOnReleaseFeatureBranchDeleted()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithAssemblyVersioningScheme(AssemblyVersioningScheme.MajorMinorPatchTag)
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        const string release450 = "release/4.5.0";
        const string featureBranch = "feature/some-bug-fix";

        fixture.Repository.MakeACommit("initial");
        fixture.Repository.CreateBranch("develop");
        Commands.Checkout(fixture.Repository, "develop");

        // begin the release branch
        fixture.Repository.CreateBranch(release450);
        Commands.Checkout(fixture.Repository, release450);
        fixture.AssertFullSemver("4.5.0-beta.1+1", configuration);

        fixture.Repository.CreateBranch(featureBranch);
        Commands.Checkout(fixture.Repository, featureBranch);
        fixture.Repository.MakeACommit("blabla"); // commit 1
        Commands.Checkout(fixture.Repository, release450);
        fixture.Repository.MergeNoFF(featureBranch, Generate.SignatureNow()); // commit 2
        fixture.Repository.Branches.Remove(featureBranch);

        fixture.AssertFullSemver("4.5.0-beta.1+3", configuration);
    }

    /// <summary>
    /// Create a feature branch from a release branch, and merge back, but don't delete it
    /// </summary>
    [Test]
    public void FeatureOnReleaseFeatureBranchNotDeleted()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithAssemblyVersioningScheme(AssemblyVersioningScheme.MajorMinorPatchTag)
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        const string release450 = "release/4.5.0";
        const string featureBranch = "feature/some-bug-fix";

        fixture.Repository.MakeACommit("initial");
        fixture.Repository.CreateBranch("develop");
        Commands.Checkout(fixture.Repository, "develop");

        // begin the release branch
        fixture.Repository.CreateBranch(release450);
        Commands.Checkout(fixture.Repository, release450);
        fixture.AssertFullSemver("4.5.0-beta.1+1", configuration);

        fixture.Repository.CreateBranch(featureBranch);
        Commands.Checkout(fixture.Repository, featureBranch);
        fixture.Repository.MakeACommit("blabla"); // commit 1
        Commands.Checkout(fixture.Repository, release450);
        fixture.Repository.MergeNoFF(featureBranch, Generate.SignatureNow()); // commit 2

        fixture.AssertFullSemver("4.5.0-beta.1+3", configuration);
    }

    [TestCase("release/1.2.0", "1.2.0-beta.1+1", SemanticVersionFormat.Loose)]
    [TestCase("release/1.2.0", "1.2.0-beta.1+1", SemanticVersionFormat.Strict)]
    [TestCase("release/1.2", "1.2.0-beta.1+1", SemanticVersionFormat.Loose)]
    [TestCase("release/1", "1.0.0-beta.1+1", SemanticVersionFormat.Loose)]
    public void ShouldDetectVersionInReleaseBranch(string branchName, string expectedVersion, SemanticVersionFormat semanticVersionFormat)
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithSemanticVersionFormat(semanticVersionFormat)
            .Build();

        using var fixture = new EmptyRepositoryFixture(branchName);
        fixture.MakeACommit();
        fixture.AssertFullSemver(expectedVersion, configuration);
    }
}
