using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class PullBranchTests : Lg2sHelperBase
{
    [Test, Ignore("Not valid since Github wont allow empty pulls")]
    public void Pull_request_with_no_commit()
    {
    }

    [Test]
    public void Invalid_pull_branch_name()
    {
        AssertInvalidPullBranchName("pull");
        AssertInvalidPullBranchName("pull/1735");
        AssertInvalidPullBranchName("pull/merge");
        AssertInvalidPullBranchName("pull/1735/a/merge");
        AssertInvalidPullBranchName("pull/1735-a/merge");
        AssertInvalidPullBranchName("pull/-1735/merge");
        AssertInvalidPullBranchName("pull/1735.1/merge");
        AssertInvalidPullBranchName("pull/merge/1735");
        AssertInvalidPullBranchName("merge/1735/pull");
    }

    void AssertInvalidPullBranchName(string invalidFakePullBranchName)
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            var branchingCommit = repo.Branches["develop"].Tip;
            var pullBranch = repo.Branches.Add(invalidFakePullBranchName, branchingCommit);

            var finder = new PullVersionFinder();

            Assert.Throws<WarningException>(() => finder.FindVersion(new GitVersionContext(repo, pullBranch, new Config())));
        }
    }

    [Test]
    public void Pull_branch_with_1_commit()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            // Create a pull request branch from the parent of current develop tip
            repo.Branches.Add("pull/1735/merge", "develop~").ForceCheckout();

            AddOneCommitToHead(repo, "code");

            var pullBranch = repo.Head;

            var finder = new PullVersionFinder();

            var configuration = new Config();
            var version = finder.FindVersion(new GitVersionContext(repo, pullBranch, configuration));

            var masterVersion = FindersHelper.RetrieveMasterVersion(repo, configuration);

            Assert.AreEqual(masterVersion.Minor + 1, version.Minor, "Minor should be master.Minor+1");
            Assert.AreEqual(1735, version.PreReleaseTag.Number);
            ObjectApprover.VerifyWithJson(version, Scrubbers.GuidAndDateScrubber);
        }
    }

    [Test]
    public void Pull_branch_with_2_commits()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            // Create a pull request branch from the parent of current develop tip
            repo.Branches.Add("pull/1735/merge", "develop~").ForceCheckout();

            AddOneCommitToHead(repo, "code");
            AddOneCommitToHead(repo, "more code");

            var pullBranch = repo.Head;

            var finder = new PullVersionFinder();

            var configuration = new Config();
            var version = finder.FindVersion(new GitVersionContext(repo, pullBranch, configuration));

            var masterVersion = FindersHelper.RetrieveMasterVersion(repo, configuration);

            Assert.AreEqual(masterVersion.Minor + 1, version.Minor, "Minor should be master.Minor+1");
            Assert.AreEqual(1735, version.PreReleaseTag.Number);
            ObjectApprover.VerifyWithJson(version, Scrubbers.GuidAndDateScrubber);
        }
    }
}