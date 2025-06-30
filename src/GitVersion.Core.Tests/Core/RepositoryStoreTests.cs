using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Core.Tests.IntegrationTests;
using GitVersion.Git;
using GitVersion.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Core.Tests;

[TestFixture]
public class RepositoryStoreTests : TestBase
{
    private readonly ILog log;

    public RepositoryStoreTests()
    {
        var sp = ConfigureServices();
        this.log = sp.GetRequiredService<ILog>();
    }

    [Test]
    public void FindsCorrectMergeBaseForForwardMerge()
    {
        //*9dfb8b4 49 minutes ago(develop)
        //*54f21b2 53 minutes ago
        //    |\
        //    | | *a219831 51 minutes ago(HEAD -> release-2.0.0)
        //    | |/
        //    | *4441531 54 minutes ago
        //    | *89840df 56 minutes ago
        //    |/
        //*91bf945 58 minutes ago(main)
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit("initial");
        fixture.BranchTo("develop");
        var fixtureRepository = fixture.Repository.ToGitRepository();
        var expectedReleaseMergeBase = fixtureRepository.Head.Tip;

        // Create release from develop
        fixture.BranchTo("release-2.0.0");

        // Make some commits on release
        fixture.MakeACommit("release 1");
        fixture.MakeACommit("release 2");
        var expectedDevelopMergeBase = fixtureRepository.Head.Tip;

        // First forward merge release to develop
        fixture.Checkout("develop");
        fixture.MergeNoFF("release-2.0.0");

        // Make some new commit on release
        fixture.Checkout("release-2.0.0");
        fixture.MakeACommit("release 3 - after first merge");

        // Make new commit on develop
        fixture.Checkout("develop");

        // Checkout to release (no new commits)
        fixture.Checkout("release-2.0.0");

        var develop = fixtureRepository.FindBranch("develop");
        var release = fixtureRepository.FindBranch("release-2.0.0");
        var gitRepoMetadataProvider = new RepositoryStore(this.log, fixtureRepository);

        var releaseBranchMergeBase = gitRepoMetadataProvider.FindMergeBase(release, develop);

        var developMergeBase = gitRepoMetadataProvider.FindMergeBase(develop, release);

        fixtureRepository.DumpGraph();

        releaseBranchMergeBase.ShouldBe(expectedReleaseMergeBase);
        developMergeBase.ShouldBe(expectedDevelopMergeBase);
    }

    [Test]
    public void FindsCorrectMergeBaseForForwardMergeMovesOn()
    {
        //*9dfb8b4 49 minutes ago(develop)
        //*54f21b2 53 minutes ago
        //    |\
        //    | | *a219831 51 minutes ago(HEAD -> release-2.0.0)
        //    | |/
        //    | *4441531 54 minutes ago
        //    | *89840df 56 minutes ago
        //    |/
        //*91bf945 58 minutes ago(main)
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit("initial");
        fixture.AssertFullSemver("0.0.1-1");
        fixture.AssertCommitsSinceVersionSource(1);
        fixture.BranchTo("develop");
        fixture.AssertFullSemver("0.1.0-alpha.1");
        var fixtureRepository = fixture.Repository.ToGitRepository();
        var expectedReleaseMergeBase = fixtureRepository.Head.Tip;
        fixture.SequenceDiagram.NoteOver(string.Join(System.Environment.NewLine, ("Expected Release Merge Base" + System.Environment.NewLine + System.Environment.NewLine +
            "This is the first common ancestor of both develop and release, from release's perspective.").SplitIntoLines(30)), "main", "develop");

        // Create release from develop
        fixture.BranchTo("release-2.0.0");
        fixture.AssertFullSemver("2.0.0-beta.1+1");

        // Make some commits on release
        fixture.MakeACommit("release 1");
        fixture.AssertCommitsSinceVersionSource(2);
        fixture.AssertFullSemver("2.0.0-beta.1+2");
        fixture.MakeACommit("release 2");
        fixture.AssertCommitsSinceVersionSource(3);
        fixture.AssertFullSemver("2.0.0-beta.1+3");
        fixture.SequenceDiagram.NoteOver(string.Join(System.Environment.NewLine, ("Expected Develop Merge Base" + System.Environment.NewLine + System.Environment.NewLine +
            "This is a common ancestor from develop's perspective because it is aware of the merge from release." + System.Environment.NewLine +
            "It is NOT an common ancestor from release's perspective because release is NOT aware of the merge to develop.").SplitIntoLines(30)), "release-2.0.0");
        var expectedDevelopMergeBase = fixtureRepository.Head.Tip;

        // First forward merge release to develop
        fixture.Checkout("develop");
        fixture.MergeNoFF("release-2.0.0");
        fixture.AssertFullSemver("2.1.0-alpha.3");
        fixture.AssertCommitsSinceVersionSource(3);

        // Make some new commit on release
        fixture.Checkout("release-2.0.0");
        fixture.MakeACommit("release 3 - after first merge");
        fixture.AssertFullSemver("2.0.0-beta.1+4");
        fixture.AssertCommitsSinceVersionSource(4);

        // Make new commit on develop
        fixture.Checkout("develop");
        fixture.AssertFullSemver("2.1.0-alpha.3");
        // Checkout to release (no new commits)
        fixture.MakeACommit("develop after merge");
        fixture.AssertFullSemver("2.1.0-alpha.4");
        fixture.AssertCommitsSinceVersionSource(4);

        // Checkout to release (no new commits)
        fixture.Checkout("release-2.0.0");
        fixture.SequenceDiagram.NoteOver("Checkout release-2.0.0 again", "release-2.0.0");
        fixture.AssertFullSemver("2.0.0-beta.1+4");
        fixture.AssertCommitsSinceVersionSource(4);

        var develop = fixtureRepository.FindBranch("develop");
        var release = fixtureRepository.FindBranch("release-2.0.0");
        var gitRepoMetadataProvider = new RepositoryStore(this.log, fixtureRepository);

        var releaseBranchMergeBase = gitRepoMetadataProvider.FindMergeBase(release, develop);

        var developMergeBase = gitRepoMetadataProvider.FindMergeBase(develop, release);

        fixtureRepository.DumpGraph();

        releaseBranchMergeBase.ShouldBe(expectedReleaseMergeBase);
        developMergeBase.ShouldBe(expectedDevelopMergeBase);
    }

    [Test]
    public void FindsCorrectMergeBaseForMultipleForwardMerges()
    {
        //*403b294 44 minutes ago(develop)
        //|\
        //| *306b243 45 minutes ago(HEAD -> release-2.0.0)
        //| *4cf5969 47 minutes ago
        //| *4814083 51 minutes ago
        //* | cddd3cc 49 minutes ago
        //* | 2b2b52a 53 minutes ago
        //|\ \
        //| |/
        //| *8113776 54 minutes ago
        //| *3c0235e 56 minutes ago
        //|/
        //*f6f1283 58 minutes ago(main)

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit("initial");
        fixture.BranchTo("develop");
        var fixtureRepository = fixture.Repository.ToGitRepository();
        var expectedReleaseMergeBase = fixtureRepository.Head.Tip;

        // Create release from develop
        fixture.BranchTo("release-2.0.0");

        // Make some commits on release
        fixture.MakeACommit("release 1");
        fixture.MakeACommit("release 2");

        // First forward merge release to develop
        fixture.Checkout("develop");
        fixture.MergeNoFF("release-2.0.0");

        // Make some new commit on release
        fixture.Checkout("release-2.0.0");
        fixture.MakeACommit("release 3 - after first merge");

        // Make new commit on develop
        fixture.Checkout("develop");
        // Checkout to release (no new commits)
        fixture.Checkout("release-2.0.0");
        fixture.Checkout("develop");
        fixture.MakeACommit("develop after merge");

        // Checkout to release (no new commits)
        fixture.Checkout("release-2.0.0");

        // Make some new commit on release
        fixture.MakeACommit("release 4");
        fixture.MakeACommit("release 5");
        var expectedDevelopMergeBase = fixtureRepository.Head.Tip;

        // Second merge release to develop
        fixture.Checkout("develop");
        fixture.MergeNoFF("release-2.0.0");

        // Checkout to release (no new commits)
        fixture.Checkout("release-2.0.0");

        var develop = fixtureRepository.FindBranch("develop");
        var release = fixtureRepository.FindBranch("release-2.0.0");

        var gitRepoMetadataProvider = new RepositoryStore(this.log, fixtureRepository);

        var releaseBranchMergeBase = gitRepoMetadataProvider.FindMergeBase(release, develop);

        var developMergeBase = gitRepoMetadataProvider.FindMergeBase(develop, release);

        fixtureRepository.DumpGraph();

        releaseBranchMergeBase.ShouldBe(expectedReleaseMergeBase);
        developMergeBase.ShouldBe(expectedDevelopMergeBase);
    }

    [Test]
    public void GetBranchesContainingCommitThrowsDirectlyOnNullCommit()
    {
        using var fixture = new EmptyRepositoryFixture();
        var fixtureRepository = fixture.Repository.ToGitRepository();
        var gitRepoMetadataProvider = new RepositoryStore(this.log, fixtureRepository);

        Assert.Throws<ArgumentNullException>(() => gitRepoMetadataProvider.GetBranchesContainingCommit(null!));
    }

    [Test]
    public void FindCommitBranchWasBranchedFromShouldReturnNullIfTheRemoteIsTheOnlySource()
    {
        using var fixture = new RemoteRepositoryFixture();
        fixture.MakeACommit("initial");

        var localRepository = fixture.LocalRepositoryFixture.Repository.ToGitRepository();

        var gitRepoMetadataProvider = new RepositoryStore(this.log, localRepository);

        var branch = localRepository.FindBranch("main");
        branch.ShouldNotBeNull();

        var configuration = GitFlowConfigurationBuilder.New.Build();
        var branchedCommit = gitRepoMetadataProvider.FindCommitBranchBranchedFrom(branch, configuration);
        branchedCommit.ShouldBe(BranchCommit.Empty);

        var branchedCommits = gitRepoMetadataProvider.FindCommitBranchesBranchedFrom(branch, configuration).ToArray();
        branchedCommits.ShouldBeEmpty();
    }
}
