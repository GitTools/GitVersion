using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class ReleaseTests : Lg2sHelperBase
{
    [Test]
    public void EnsureAReleaseBranchNameDoesNotExposeAPatchSegment()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            const string branchName = "release-0.3.1";

            var branchingCommit = repo.Branches["develop"].Tip;
            var releaseBranch = repo.Branches.Add(branchName, branchingCommit);

            var finder = new ReleaseVersionFinder();

            Assert.Throws<WarningException>(() => finder.FindVersion(new GitVersionContext(repo, releaseBranch)));
        }
    }

    [Test]
    public void EnsureAReleaseBranchNameDoesNotExposeAStability()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            const string branchName = "release-0.3.0-Final";

            var branchingCommit = repo.Branches["develop"].Tip;
            var releaseBranch = repo.Branches.Add(branchName, branchingCommit);

            var finder = new ReleaseVersionFinder();

            Assert.Throws<WarningException>(() => finder.FindVersion(new GitVersionContext(repo, releaseBranch)));
        }
    }

    [Test]
    public void Tag_on_commit_should_be_exact_tag()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            // Create a release branch from the parent of current develop tip
            repo.Branches.Add("release-5.0.0", "develop~").ForceCheckout();

            AddOneCommitToHead(repo, "code");
            AddTag(repo, "5.0.0-beta2");

            var finder = new ReleaseVersionFinder();

            var version = finder.FindVersion(new GitVersionContext(repo, repo.Head));
            ObjectApprover.VerifyWithJson(version, Scrubbers.GuidAndDateScrubber);
        }
    }
}