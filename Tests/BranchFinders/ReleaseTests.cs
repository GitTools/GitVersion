using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

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

}