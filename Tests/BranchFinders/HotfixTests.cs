using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;
using Tests.Helpers;

[TestFixture]
public class HotfixTests : Lg2sHelperBase
{
    [Test]
    public void No_commits_with_no_tag()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            const string branchName = "hotfix-0.1.4";

            var branchingCommit = repo.Branches["master"].Tip;
            var hotfixBranch = repo.Branches.Add(branchName, branchingCommit);

            var finder = new HotfixVersionFinder();

            var version = finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                Tip = branchingCommit,
                CurrentBranch = hotfixBranch,
            });

            Assert.AreEqual(0, version.Version.Major);
            Assert.AreEqual(1, version.Version.Minor);
            Assert.AreEqual(4, version.Version.Patch);
            Assert.AreEqual(Stability.Beta, version.Version.Stability);
            Assert.AreEqual(BranchType.Hotfix, version.BranchType);
            Assert.AreEqual(0, version.Version.PreReleasePartOne);
            Assert.IsNull(version.Version.PreReleasePartTwo, "PreReleasePartTwo null since there is no commits");
        }
    }

    [Test]
    public void First_commit_with_tag_pointing_at_branching_commit()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            const string branchName = "hotfix-0.1.3";

            var branchingCommit = repo.Branches["master"].Tip;
            repo.Branches.Add(branchName, branchingCommit).Checkout();

            AddOneCommitToHead(repo, "hotfix");

            var hotfixBranch = repo.Branches[branchName];
            var firstCommit = hotfixBranch.Tip;

            var sign = Constants.SignatureNow();
            repo.Tags.Add("0.1.3-beta4", branchingCommit, sign, "hotfix");

            var finder = new HotfixVersionFinder();

            var version = finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                Tip = firstCommit,
                CurrentBranch = hotfixBranch,
            });

            Assert.AreEqual(0, version.Version.Major);
            Assert.AreEqual(1, version.Version.Minor);
            Assert.AreEqual(3, version.Version.Patch);
            Assert.AreEqual(Stability.Beta, version.Version.Stability);
            Assert.AreEqual(BranchType.Hotfix, version.BranchType);
            Assert.AreEqual(4, version.Version.PreReleasePartOne, "PreReleasePartOne should be set to 4 from the tag");
            Assert.AreEqual(1, version.Version.PreReleasePartTwo, "PreReleasePartTwo should be set to 1 since there is a commit on the branch");
        }
    }

    [Test]
    public void Second_commit_with_tag_pointing_at_first_commit()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            const string branchName = "hotfix-0.1.3";

            var branchingCommit = repo.Branches["master"].Tip;
            repo.Branches.Add(branchName, branchingCommit).Checkout();

            var firstCommit = AddOneCommitToHead(repo, "hotfix");
            AddOneCommitToHead(repo, "hotfix");

            var hotfixBranch = repo.Branches[branchName];
            var secondCommit = hotfixBranch.Tip;

            var sign = Constants.SignatureNow();
            repo.Tags.Add("0.1.3-alpha5", firstCommit, sign, "hotfix");

            var finder = new HotfixVersionFinder();

            var version = finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                Tip = secondCommit,
                CurrentBranch = hotfixBranch,
            });

            Assert.AreEqual(0, version.Version.Major);
            Assert.AreEqual(1, version.Version.Minor);
            Assert.AreEqual(3, version.Version.Patch);
            Assert.AreEqual(Stability.Alpha, version.Version.Stability);
            Assert.AreEqual(BranchType.Hotfix, version.BranchType);
            Assert.AreEqual(5, version.Version.PreReleasePartOne, "PreReleasePartOne should be set to 5 from the tag");
            Assert.AreEqual(2, version.Version.PreReleasePartTwo, "PreReleasePartTwo should be set to 2 since there is a commit on the branch");
        }
    }

    [Test]
    public void Second_commit_with_no_tag()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            const string branchName = "hotfix-0.1.3";

            var branchingCommit = repo.Branches["master"].Tip;
            repo.Branches.Add(branchName, branchingCommit).Checkout();

            AddOneCommitToHead(repo, "hotfix");
            AddOneCommitToHead(repo, "hotfix");

            var hotfixBranch = repo.Branches[branchName];
            var secondCommit = hotfixBranch.Tip;

            var finder = new HotfixVersionFinder();

            var version = finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                Tip = secondCommit,
                CurrentBranch = hotfixBranch,
            });

            Assert.AreEqual(0, version.Version.Major);
            Assert.AreEqual(1, version.Version.Minor);
            Assert.AreEqual(3, version.Version.Patch);
            Assert.AreEqual(Stability.Beta, version.Version.Stability);
            Assert.AreEqual(BranchType.Hotfix, version.BranchType);
            Assert.AreEqual(0, version.Version.PreReleasePartOne);
            Assert.AreEqual(2, version.Version.PreReleasePartTwo, "PreReleasePartTwo should be set to 2 since there is a commit on the branch");
        }
    }

    [Test]
    public void Second_commit_with_tag_pointing_at_first_commit_and_unrelated_one_at_second_commit()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            const string branchName = "hotfix-0.1.3";

            var branchingCommit = repo.Branches["master"].Tip;
            repo.Branches.Add(branchName, branchingCommit).Checkout();

            var firstCommit = AddOneCommitToHead(repo, "hotfix");
            AddOneCommitToHead(repo, "hotfix");

            var hotfixBranch = repo.Branches[branchName];
            var secondCommit = hotfixBranch.Tip;

            var sign = Constants.SignatureNow();
            repo.Tags.Add("0.1.3-alpha5", firstCommit, sign, "hotfix");

            repo.Tags.Add("0.1.4-RC1", secondCommit, sign, "hotfix");

            var finder = new HotfixVersionFinder();

            var version = finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                Tip = secondCommit,
                CurrentBranch = hotfixBranch,
            });

            Assert.AreEqual(0, version.Version.Major);
            Assert.AreEqual(1, version.Version.Minor);
            Assert.AreEqual(3, version.Version.Patch);
            Assert.AreEqual(Stability.Alpha, version.Version.Stability);
            Assert.AreEqual(BranchType.Hotfix, version.BranchType);
            Assert.AreEqual(5, version.Version.PreReleasePartOne, "PreReleasePartOne should be set to 5 from the tag");
            Assert.AreEqual(2, version.Version.PreReleasePartTwo, "PreReleasePartTwo should be set to 2 since there is a commit on the branch");
        }
    }

    [Test]
    public void EnsureAHotfixBranchNameExposesAPatchSegment()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            const string branchName = "hotfix-0.3.0";

            var branchingCommit = repo.Branches["master"].Tip;
            var hotfixBranch = repo.Branches.Add(branchName, branchingCommit);

            var finder = new HotfixVersionFinder();

            Assert.Throws<ErrorException>(() => finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                Tip = branchingCommit,
                CurrentBranch = hotfixBranch,
            }));
        }
    }

    [Test]
    public void EnsureAHotfixBranchNameExposesAStability()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            const string branchName = "hotfix-0.3.1-alpha5";

            var branchingCommit = repo.Branches["master"].Tip;
            var hotfixBranch = repo.Branches.Add(branchName, branchingCommit);

            var finder = new HotfixVersionFinder();

            Assert.Throws<ErrorException>(() => finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                Tip = branchingCommit,
                CurrentBranch = hotfixBranch,
            }));
        }
    }
}
