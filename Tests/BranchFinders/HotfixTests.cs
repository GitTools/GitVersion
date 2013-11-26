using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;
using Tests.Helpers;

[TestFixture]
public class HotfixTests : Lg2sHelperBase
{
    [Test]
    public void No_commits()
    {
        string repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            var branchingCommit = repo.Branches["master"].Tip;
            var hotfixBranch = repo.Branches.Add("hotfix-0.1.4", branchingCommit);

            var sign = Constants.SignatureNow();
            repo.Tags.Add("0.1.4-alpha5", branchingCommit, sign, "hotfix");
            
            var finder = new HotfixVersionFinder
            {
                Repository = repo,
                Commit = branchingCommit,
                HotfixBranch = hotfixBranch,
            };

            var version = finder.FindVersion();

            Assert.AreEqual(0, version.Version.Major);
            Assert.AreEqual(1, version.Version.Minor);
            Assert.AreEqual(4, version.Version.Patch);
            Assert.AreEqual(Stability.Alpha, version.Version.Stability);
            Assert.AreEqual(BranchType.Hotfix, version.BranchType);
            Assert.AreEqual(5, version.Version.PreReleasePartOne, "PreReleasePartOne should be set to 5 from the tag");
            Assert.IsNull(version.Version.PreReleasePartTwo, "PreReleasePartTwo null since there is no commits");
        }
    }

    [Test]
    public void First_commit()
    {
        string repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            var branchingCommit = repo.Branches["master"].Tip;
            repo.Branches.Add("hotfix-0.1.3", branchingCommit).Checkout();

            AddOneCommitToHead(repo, "hotfix");

            var hotfixBranch = repo.Branches["hotfix-0.1.3"];
            var firstCommit = hotfixBranch.Tip;

            var sign = Constants.SignatureNow();
            repo.Tags.Add("0.1.3-beta4", branchingCommit, sign, "hotfix");

            var finder = new HotfixVersionFinder
            {
                Repository = repo,
                Commit = firstCommit,
                HotfixBranch = hotfixBranch,
            };

            var version = finder.FindVersion();

            Assert.AreEqual(0, version.Version.Major);
            Assert.AreEqual(1, version.Version.Minor);
            Assert.AreEqual(3, version.Version.Patch);
            Assert.AreEqual(Stability.Beta, version.Version.Stability);
            Assert.AreEqual(BranchType.Hotfix, version.BranchType);
            Assert.AreEqual(4, version.Version.PreReleasePartOne, "PreReleasePartOne should be set to 5 from the tag");
            Assert.AreEqual(1, version.Version.PreReleasePartTwo, "PreReleasePartTwo should be set to 1 since there is a commit on the branch");
        }
    }

    [Test]
    public void Second_commit()
    {
        string repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            var branchingCommit = repo.Branches["master"].Tip;
            repo.Branches.Add("hotfix-0.1.3", branchingCommit).Checkout();

            AddOneCommitToHead(repo, "hotfix");
            AddOneCommitToHead(repo, "hotfix");

            var hotfixBranch = repo.Branches["hotfix-0.1.3"];
            var firstCommit = hotfixBranch.Tip;

            var sign = Constants.SignatureNow();
            repo.Tags.Add("0.1.3-alpha5", branchingCommit, sign, "hotfix");

            var finder = new HotfixVersionFinder
            {
                Repository = repo,
                Commit = firstCommit,
                HotfixBranch = hotfixBranch,
            };

            var version = finder.FindVersion();

            Assert.AreEqual(0, version.Version.Major);
            Assert.AreEqual(1, version.Version.Minor);
            Assert.AreEqual(3, version.Version.Patch);
            Assert.AreEqual(Stability.Alpha, version.Version.Stability);
            Assert.AreEqual(BranchType.Hotfix, version.BranchType);
            Assert.AreEqual(5, version.Version.PreReleasePartOne, "PreReleasePartOne should be set to 5 from the tag");
            Assert.AreEqual(2, version.Version.PreReleasePartTwo, "PreReleasePartTwo should be set to 2 since there is a commit on the branch");
        }
    }
}
