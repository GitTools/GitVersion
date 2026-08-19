using GitVersion.Configuration;
using GitVersion.Git;

namespace GitVersion.Tests;

[TestFixture]
public class BranchRepositoryTests : TestBase
{
    [TestCase("refs/heads/release/legacy")]
    [TestCase("refs/remotes/origin/release/legacy")]
    [TestCase("refs/remotes/upstream/release/legacy")]
    public void IsBranchIgnored_MatchesFriendlyNameWithoutRemoteName(string canonicalName)
    {
        var configuration = new IgnoreConfiguration { Branches = ["^release/legacy$"] };

        configuration.IsBranchIgnored(new ReferenceName(canonicalName)).ShouldBeTrue();
    }

    [Test]
    public void GetMainBranches_IgnorePatternsUseCaseInsensitiveOrSemanticsWithoutRemote()
    {
        var localMain = GitRepositoryTestingExtensions.CreateMockBranch("refs/heads/main");
        var remoteMain = GitRepositoryTestingExtensions.CreateMockBranch("refs/remotes/origin/main");
        var master = GitRepositoryTestingExtensions.CreateMockBranch("refs/heads/master");
        var repositoryStore = CreateRepositoryStore(localMain, remoteMain, master);
        var configuration = GitFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Branches = ["^legacy/", "^MAIN$"] })
            .Build();

        var actual = new BranchRepository(repositoryStore).GetMainBranches(configuration).ToArray();

        actual.ShouldBe([master]);
    }

    [Test]
    public void FindSourceBranchesOf_IgnoredBranchIsExcludedButSharedCandidatesRemain()
    {
        var current = GitRepositoryTestingExtensions.CreateMockBranch("refs/heads/feature/current");
        var main = GitRepositoryTestingExtensions.CreateMockBranch("refs/remotes/origin/main");
        var develop = GitRepositoryTestingExtensions.CreateMockBranch("refs/heads/develop");
        var configuration = GitFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Branches = ["^main$"] })
            .Build();

        var actual = new SourceBranchFinder([main, develop], configuration, excludeIgnoredBranches: true)
            .FindSourceBranchesOf(current).ToArray();

        actual.ShouldBe([develop]);
    }

    private static IRepositoryStore CreateRepositoryStore(params IBranch[] branches)
    {
        var branchCollection = Substitute.For<IBranchCollection>();
        branchCollection.MockCollectionReturn(branches);
        var repositoryStore = Substitute.For<IRepositoryStore>();
        repositoryStore.Branches.Returns(branchCollection);
        return repositoryStore;
    }
}
