using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class GitVersionFinderTests : Lg2sHelperBase
{
    [Test]
    public void RequiresALocalMasterBranch()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            repo.Branches["feature/one"].ForceCheckout();

            repo.Branches.Remove("master");

            var finder = new GitVersionFinder();

            Assert.Throws<WarningException>(() => finder.FindVersion(new GitVersionContext(repo, new Config())));
        }
    }

    [Test]
    public void AFeatureBranchDoesNotRequireASpecificPrefix()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            repo.Branches["develop"].ForceCheckout();

            const string branchName = "every-feature-is-welcome";
            repo.Branches.Add(branchName, repo.Head.Tip).ForceCheckout();

            AddOneCommitToHead(repo, "code");

            var finder = new GitVersionFinder();

            var versionAndBranch = finder.FindVersion(new GitVersionContext(repo, new Config()));

            ObjectApprover.VerifyWithJson(versionAndBranch, Scrubbers.GuidAndDateScrubber);
        }
    }

    [Test]
    public void AFeatureBranchPrefixIsNotIncludedInTag()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            repo.Branches["develop"].ForceCheckout();

            const string branchName = "feature/ABC-1234_SomeDescription";
            repo.Branches.Add(branchName, repo.Head.Tip).ForceCheckout();

            AddOneCommitToHead(repo, "code");

            var finder = new GitVersionFinder();

            var versionAndBranch = finder.FindVersion(new GitVersionContext(repo, new Config()));

            ObjectApprover.VerifyWithJson(versionAndBranch, Scrubbers.GuidAndDateScrubber);
        }
    }
}