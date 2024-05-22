using GitVersion.Configuration;

namespace GitVersion.Core.Tests.IntegrationTests;

/// <summary>
/// Version not generated correct when creating a feature branch from a release branch #3101
/// </summary>
[TestFixture]
public class CreatingAFeatureBranchFromAReleaseBranchScenario
{
    [Test]
    public void ShouldTreatTheFeatureBranchLikeTheFirstReleaseBranchWhenItHasBeenBranchedFromMainAndFirstReleaseBranchButNotFromTheSecondReleaseBranch()
    {
        // *f59b84f in the future(HEAD -> release/ 1.0.0)
        // *d0f4669 in the future
        // |\  
        // | *471acec in the future
        // |/
        // | *266fa68 in the future(release/ 1.1.0, main)
        // |/
        // *e0b5034 6 seconds ago

        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.Repository.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        fixture.BranchTo("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.BranchTo("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-just-a-test.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-2", configuration);

        fixture.BranchTo("release/1.1.0");
        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.Checkout("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-just-a-test.1+1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-just-a-test.1+2", configuration);

        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.MergeNoFF("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);

        fixture.Repository.Branches.Remove("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+4", configuration);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void ShouldTreatTheFeatureBranchNotLikeTheReleaseBranchWhenItHasBeenBranchedFromDevelopAndFirstReleaseBranchButNotFromTheSecondReleaseBranch()
    {
        // *19ed1e8 in the future(HEAD -> release/ 1.0.0)
        // *1684169 in the future
        // |\  
        // | *07bd75c in the future
        // |/
        // | *ff34213 in the future(release/ 1.1.0, develop)
        // |/
        // *d5ac9aa in the future

        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture("develop");

        fixture.Repository.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.1", configuration);

        fixture.BranchTo("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        // 1.1.0 is correct because the base branch points to develop and release
        // maybe we can fix it somehow using the configuration with PreReleaseWeight?
        fixture.BranchTo("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-just-a-test.1+0", configuration);

        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.0", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);

        fixture.BranchTo("release/1.1.0");
        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.Checkout("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-just-a-test.1+1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-just-a-test.1+2", configuration);

        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.MergeNoFF("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);

        fixture.Repository.Branches.Remove("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+4", configuration);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void ShouldTreatTheHotfixBranchLikeTheFirstReleaseBranchWhenItHasBeenBranchedFromMainAndFirstReleaseBranchButNotFromTheSecondReleaseBranch()
    {
        // *2b9c8bf 42 minutes ago(HEAD -> release/ 1.0.0)
        // *66cfc66 44 minutes ago
        // |\  
        // | *e9978b9 45 minutes ago
        // |/
        // | *c2b96e5 47 minutes ago(release/ 1.1.0, main|develop)
        // |/
        // *e00f53d 49 minutes ago

        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();
        fixture.BranchTo("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.BranchTo("hotfix/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit();
        fixture.BranchTo("release/1.1.0");
        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.Checkout("hotfix/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+2", configuration);

        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.MergeNoFF("hotfix/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);

        fixture.Repository.Branches.Remove("hotfix/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+4", configuration);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void ShouldTreatTheHotfixBranchLikeTheFirstReleaseBranchWhenItHasBeenBranchedFromDevelopAndFirstReleaseBranchButNotFromTheSecondReleaseBranch()
    {
        // *2b9c8bf 42 minutes ago(HEAD -> release/ 1.0.0)
        // *66cfc66 44 minutes ago
        // |\  
        // | *e9978b9 45 minutes ago
        // |/
        // | *c2b96e5 47 minutes ago(release/ 1.1.0, main|develop)
        // |/
        // *e00f53d 49 minutes ago

        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);

        fixture.BranchTo("hotfix/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+2", configuration);

        fixture.Checkout("develop");
        fixture.MakeACommit();
        fixture.BranchTo("release/1.1.0");
        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);

        fixture.Checkout("hotfix/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+2", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+3", configuration);

        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);

        fixture.MergeNoFF("hotfix/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+4", configuration);

        fixture.Repository.Branches.Remove("hotfix/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+4", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+5", configuration);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void ShouldTreatTheFeatureBranchLikeTheFirstReleaseBranchWhenItHasBeenBranchedFromFirstButNotFromTheSecondReleaseBranchFromMain()
    {
        // * 3807c66 49 minutes ago  (HEAD -> release/1.0.0)
        // *   da32145 51 minutes ago 
        // |\  
        // | * 6a89f35 52 minutes ago 
        // |/  
        // * 19f2980 56 minutes ago 
        // | * 2282a59 54 minutes ago  (release/1.1.0, main)
        // |/  
        // * d7b7c5e 58 minutes ago 

        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();

        fixture.BranchTo("release/1.0.0");
        fixture.Repository.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);

        fixture.BranchTo("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-just-a-test.1+2", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit();
        fixture.BranchTo("release/1.1.0");
        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);

        fixture.Checkout("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-just-a-test.1+2", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-just-a-test.1+3", configuration);

        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);

        fixture.MergeNoFF("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+4", configuration);

        fixture.Repository.Branches.Remove("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+4", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+5", configuration);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void ShouldTreatTheFeatureBranchLikeTheFirstReleaseBranchWhenItHasBeenBranchedFromFirstButNotFromTheSecondReleaseBranchFromDevelop()
    {
        // *1525ad0 38 minutes ago(HEAD -> release/ 1.0.0)
        // *476fc51 40 minutes ago
        // |\  
        // | *c8c5030 41 minutes ago
        // |/
        // *d91061d 45 minutes ago
        // | *1ac98f5 43 minutes ago(release/ 1.1.0, develop)
        // |/
        // *22596b8 47 minutes ago

        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();

        fixture.BranchTo("develop");
        fixture.Repository.MakeACommit();

        fixture.BranchTo("release/1.0.0");
        fixture.Repository.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);

        fixture.BranchTo("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-just-a-test.1+3", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit();
        fixture.BranchTo("release/1.1.0");
        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);

        fixture.Checkout("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-just-a-test.1+3", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-just-a-test.1+4", configuration);

        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);

        fixture.MergeNoFF("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+5", configuration);

        fixture.Repository.Branches.Remove("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+5", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+6", configuration);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void ShouldTreatTheHotfixBranchLikeTheFirstReleaseBranchWhenItHasBeenBranchedFromFirstButNotFromTheSecondReleaseBranchOnMain()
    {
        // *1525ad0 38 minutes ago(HEAD -> release/ 1.0.0)
        // *476fc51 40 minutes ago
        // |\  
        // | *c8c5030 41 minutes ago
        // |/
        // *d91061d 45 minutes ago
        // | *1ac98f5 43 minutes ago(release/ 1.1.0, develop)
        // |/
        // *22596b8 47 minutes ago

        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();

        fixture.BranchTo("release/1.0.0");
        fixture.Repository.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);

        fixture.BranchTo("hotfix/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+2", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit();
        fixture.BranchTo("release/1.1.0");
        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);

        fixture.Checkout("hotfix/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+2", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+3", configuration);

        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);

        fixture.MergeNoFF("hotfix/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+4", configuration);

        fixture.Repository.Branches.Remove("hotfix/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+4", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+5", configuration);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void ShouldTreatTheHotfixBranchLikeTheFirstReleaseBranchWhenItHasBeenBranchedFromFirstButNotFromTheSecondReleaseBranchOnDevelop()
    {
        // *1525ad0 38 minutes ago(HEAD -> release/ 1.0.0)
        // *476fc51 40 minutes ago
        // |\  
        // | *c8c5030 41 minutes ago
        // |/
        // *d91061d 45 minutes ago
        // | *1ac98f5 43 minutes ago(release/ 1.1.0, develop)
        // |/
        // *22596b8 47 minutes ago

        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();

        fixture.BranchTo("develop");
        fixture.Repository.MakeACommit();

        fixture.BranchTo("release/1.0.0");
        fixture.Repository.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);

        fixture.BranchTo("hotfix/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+3", configuration);

        fixture.Checkout("develop");
        fixture.MakeACommit();
        fixture.BranchTo("release/1.1.0");
        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);

        fixture.Checkout("hotfix/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+3", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+4", configuration);

        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);

        fixture.MergeNoFF("hotfix/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+5", configuration);

        fixture.Repository.Branches.Remove("hotfix/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+5", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+6", configuration);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void ShouldTreatTheFeatureBranchLikeTheReleaseBranchWhenItHasBeenBranchedFromRelease()
    {
        // *588f0de in the future(HEAD -> release/ 1.0.0)
        // *56f660c in the future
        // |\  
        // | *9450fb0 in the future
        // |/
        // *9e557cd in the future
        // *2e022d7 in the future(main)

        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.Repository.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        fixture.BranchTo("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);

        fixture.BranchTo("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-just-a-test.1+2", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-just-a-test.1+3", configuration);

        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);

        fixture.MergeNoFF("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+4", configuration);

        fixture.Repository.Branches.Remove("feature/just-a-test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+4", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+5", configuration);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void ShouldTreatTheMergeFromReleaseToDevelopLikeTheReleaseBranchHasNeverBeenExistingWhenReleaseHasBeenCanceled()
    {
        // *809eaa7 in the future(HEAD -> develop)
        // *46e2cb8 in the future
        // |\  
        // | *08bd8ff in the future
        // | *9b741de in the future
        // * | 13206fd in the future
        // |/
        // *9dc9b22 in the future
        // *f708abd in the future(main)

        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        fixture.BranchTo("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.2", configuration);

        fixture.BranchTo("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+4", configuration);

        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.0", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);

        fixture.MergeNoFF("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.4", configuration);

        fixture.Repository.Branches.Remove("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.6", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.7", configuration);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void ShouldOnlyTrackTheCommitsOnDevelopBranchForNextReleaseWhenReleaseHasBeenShippedToProduction()
    {
        // *9afb0ca in the future(HEAD -> develop)
        // |\  
        // | *90c96f2 in the future(tag: 1.0.0, main)
        // | |\  
        // | | *7de3d63 in the future
        // | | *2ccf33b in the future
        // * | | e050757 in the future
        // | |/
        // |/|
        // * | cf1ff87 in the future
        // |/
        // *838a95b in the future

        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        fixture.BranchTo("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.2", configuration);

        fixture.BranchTo("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+4", configuration);

        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.0", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);

        fixture.Checkout("main");
        fixture.MergeNoFF("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-5", configuration);

        fixture.ApplyTag("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0", configuration);

        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);

        fixture.MergeNoFF("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.2", configuration);

        fixture.Repository.Branches.Remove("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.2", configuration);

        fixture.Repository.DumpGraph();
    }

    [Test]
    public void ShouldNotConsiderTheMergeCommitFromReleaseToMainWhenCommitHasNotBeenTagged()
    {
        // *457e0cd in the future(HEAD -> develop)
        // |\  
        // | *d9da657 in the future(tag: 1.0.0, main)
        // | |\  
        // | | *026a6cd in the future
        // | | *7f5de6e in the future
        // * | | 3db6e6f in the future
        // | |/
        // |/|
        // * | 845926e in the future
        // |/
        // *42db9ba in the future

        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        fixture.BranchTo("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.1", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.2", configuration);

        fixture.BranchTo("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+4", configuration);

        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.0", configuration);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);

        fixture.Checkout("main");
        fixture.MergeNoFF("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-5", configuration);

        fixture.Repository.Branches.Remove("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-5", configuration);

        fixture.ApplyTag("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0", configuration);

        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);

        fixture.MergeNoFF("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.2", configuration);

        fixture.Repository.DumpGraph();
    }
}
