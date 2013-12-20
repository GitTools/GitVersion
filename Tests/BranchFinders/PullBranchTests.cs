using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;
using Tests.Helpers;

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
        AssertInvalidPullBranchName("pull//merge");
        AssertInvalidPullBranchName("pull///merge");
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

            Assert.Throws<ErrorException>(() => finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                Tip = branchingCommit,
                CurrentBranch = pullBranch,
            }));
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

            var version = finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                Tip = pullBranch.Tip,
                CurrentBranch = pullBranch,
            });

            var masterVersion = FindersHelper.RetrieveMasterVersion(repo);

            Assert.AreEqual(masterVersion.Version.Major, version.Version.Major);
            Assert.AreEqual(masterVersion.Version.Minor + 1, version.Version.Minor, "Minor should be master.Minor+1");
            Assert.AreEqual(0, version.Version.Patch);
            Assert.AreEqual(Stability.Unstable, version.Version.Stability);
            Assert.AreEqual(BranchType.PullRequest, version.BranchType);
            Assert.AreEqual("1735", version.Version.Suffix, "Suffix should be the develop commit it was branched from");
            Assert.AreEqual(0, version.Version.PreReleasePartOne, "Prerelease is always 0 for pull requests");
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

            var version = finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                Tip = pullBranch.Tip,
                CurrentBranch = pullBranch,
            });

            var masterVersion = FindersHelper.RetrieveMasterVersion(repo);

            Assert.AreEqual(masterVersion.Version.Major, version.Version.Major);
            Assert.AreEqual(masterVersion.Version.Minor + 1, version.Version.Minor, "Minor should be master.Minor+1");
            Assert.AreEqual(0, version.Version.Patch);
            Assert.AreEqual(Stability.Unstable, version.Version.Stability);
            Assert.AreEqual(BranchType.PullRequest, version.BranchType);
            Assert.AreEqual("1735", version.Version.Suffix, "Suffix should be the develop commit it was branched from");
            Assert.AreEqual(0, version.Version.PreReleasePartOne, "Prerelease is always 0 for pull requests");
        }
    }
}
