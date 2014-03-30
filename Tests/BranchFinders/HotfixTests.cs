using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;
using ObjectApproval;

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

            var version = finder.FindVersion(new GitVersionContext
            {
                Repository = repo,
                CurrentBranch = hotfixBranch,
            });

            ObjectApprover.VerifyWithJson(version, Scrubbers.GuidScrubber);
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

            var sign = Constants.SignatureNow();
            repo.Tags.Add("0.1.3-beta4", branchingCommit, sign, "hotfix");

            var finder = new HotfixVersionFinder();

            var version = finder.FindVersion(new GitVersionContext
            {
                Repository = repo,
                CurrentBranch = hotfixBranch,
            });

            Assert.AreEqual(1, version.BuildMetaData.CommitsSinceTag, "BuildMetaData should be set to 1 since there is a commit on the branch");
            ObjectApprover.VerifyWithJson(version, Scrubbers.GuidAndDateScrubber);
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

            var sign = Constants.SignatureNow();
            repo.Tags.Add("0.1.3-alpha5", firstCommit, sign, "hotfix");

            var finder = new HotfixVersionFinder();

            var version = finder.FindVersion(new GitVersionContext
            {
                Repository = repo,
                CurrentBranch = hotfixBranch,
            });

            Assert.AreEqual(2, version.BuildMetaData.CommitsSinceTag, "BuildMetaData should be set to 2 since there is a commit on the branch");
            ObjectApprover.VerifyWithJson(version, Scrubbers.GuidAndDateScrubber);
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

            var finder = new HotfixVersionFinder();

            var version = finder.FindVersion(new GitVersionContext
            {
                Repository = repo,
                CurrentBranch = hotfixBranch,
            });

            Assert.AreEqual(2, version.BuildMetaData.CommitsSinceTag, "BuildMetaData should be set to 2 since there is a commit on the branch");
            ObjectApprover.VerifyWithJson(version, Scrubbers.GuidAndDateScrubber);
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

            var version = finder.FindVersion(new GitVersionContext
            {
                Repository = repo,
                CurrentBranch = hotfixBranch,
            });

            ObjectApprover.VerifyWithJson(version, Scrubbers.GuidAndDateScrubber);
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

            Assert.Throws<ErrorException>(() => finder.FindVersion(new GitVersionContext
            {
                Repository = repo,
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

            Assert.Throws<ErrorException>(() => finder.FindVersion(new GitVersionContext
            {
                Repository = repo,
                CurrentBranch = hotfixBranch,
            }));
        }
    }
}
