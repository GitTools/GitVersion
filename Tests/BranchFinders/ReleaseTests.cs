using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;
using Tests.Helpers;

[TestFixture]
public class ReleaseTests : Lg2sHelperBase
{
    [Test]
    public void No_commits()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            const string branchName = "release-0.3.0";

            var branchingCommit = repo.Branches["develop"].Tip;
            var releaseBranch = repo.Branches.Add(branchName, branchingCommit);

            var sign = Constants.SignatureNow();
            repo.Tags.Add("0.3.0-alpha5", branchingCommit, sign, "release");

            var finder = new ReleaseVersionFinder
            {
                Repository = repo,
                Commit = branchingCommit,
                ReleaseBranch = releaseBranch,
            };

            var version = finder.FindVersion();
            Assert.AreEqual(0, version.Version.Major);
            Assert.AreEqual(3, version.Version.Minor);
            Assert.AreEqual(0, version.Version.Patch);
            Assert.AreEqual(Stability.Alpha, version.Version.Stability);
            Assert.AreEqual(BranchType.Release, version.BranchType);
            Assert.AreEqual(5, version.Version.PreReleasePartOne, "PreReleasePartOne should be set to 5 from the tag");
            Assert.IsNull(version.Version.PreReleasePartTwo, "PreReleasePartTwo null since there is no commits");
        }
    }

    [Test]
    public void First_commit()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            const string branchName = "release-0.5.0";

            var branchingCommit = repo.Branches["develop"].Tip;
            repo.Branches.Add(branchName, branchingCommit).Checkout();

            AddOneCommitToHead(repo, "first commit on release");

            var releaseBranch = repo.Branches[branchName];
            var firstCommit = releaseBranch.Tip;

            var sign = Constants.SignatureNow();
            repo.Tags.Add("0.5.0-alpha5", branchingCommit, sign, "release");

            var finder = new ReleaseVersionFinder
            {
                Repository = repo,
                Commit = firstCommit,
                ReleaseBranch = releaseBranch,
            };

            var version = finder.FindVersion();
            Assert.AreEqual(0, version.Version.Major);
            Assert.AreEqual(5, version.Version.Minor);
            Assert.AreEqual(0, version.Version.Patch);
            Assert.AreEqual(Stability.Alpha, version.Version.Stability);
            Assert.AreEqual(BranchType.Release, version.BranchType);
            Assert.AreEqual(5, version.Version.PreReleasePartOne, "PreReleasePartOne should be set to 5 from the tag");
            Assert.AreEqual(1, version.Version.PreReleasePartTwo, "PreReleasePartTwo should be set to 1 since there is 1 commit");
        } 
    }

    [Test]
    public void Second_commit()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            const string branchName = "release-0.4.0";

            var branchingCommit = repo.Branches["develop"].Tip;
            repo.Branches.Add(branchName, branchingCommit).Checkout();

            AddOneCommitToHead(repo, "first commit on release");
            AddOneCommitToHead(repo, "second commit on release");

            var releaseBranch = repo.Branches[branchName];
            var secondCommit = releaseBranch.Tip;

            var sign = Constants.SignatureNow();
            repo.Tags.Add("0.4.0-alpha5", branchingCommit, sign, "release");

            var finder = new ReleaseVersionFinder
            {
                Repository = repo,
                Commit = secondCommit,
                ReleaseBranch = releaseBranch,
            };

            var version = finder.FindVersion();
            Assert.AreEqual(0, version.Version.Major);
            Assert.AreEqual(4, version.Version.Minor);
            Assert.AreEqual(0, version.Version.Patch);
            Assert.AreEqual(Stability.Alpha, version.Version.Stability);
            Assert.AreEqual(BranchType.Release, version.BranchType);
            Assert.AreEqual(5, version.Version.PreReleasePartOne, "PreReleasePartOne should be set to 5 from the tag");
            Assert.AreEqual(2, version.Version.PreReleasePartTwo, "PreReleasePartTwo should be set to 2 since there is 2 commits on the branch");
        }
    }

    [Test]
    public void Second_commit_with_no_tag()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            const string branchName = "release-0.4.0";

            var branchingCommit = repo.Branches["develop"].Tip;
            repo.Branches.Add(branchName, branchingCommit).Checkout();

            AddOneCommitToHead(repo, "first commit on release");
            AddOneCommitToHead(repo, "second commit on release");

            var releaseBranch = repo.Branches[branchName];
            var secondCommit = releaseBranch.Tip;

            var finder = new ReleaseVersionFinder
            {
                Repository = repo,
                Commit = secondCommit,
                ReleaseBranch = releaseBranch,
            };

            var version = finder.FindVersion();
            Assert.AreEqual(0, version.Version.Major);
            Assert.AreEqual(4, version.Version.Minor);
            Assert.AreEqual(0, version.Version.Patch);
            Assert.AreEqual(Stability.Final, version.Version.Stability);
            Assert.AreEqual(BranchType.Release, version.BranchType);
            Assert.IsNull(version.Version.PreReleasePartOne);
            Assert.AreEqual(2, version.Version.PreReleasePartTwo, "PreReleasePartTwo should be set to 2 since there is 2 commits on the branch");
        }
    }

    [Test]
    public void Override_stage_using_tag()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            const string branchName = "release-0.4.0";

            var branchingCommit = repo.Branches["develop"].Tip;
            repo.Branches.Add(branchName, branchingCommit).Checkout();

            AddOneCommitToHead(repo, "first commit on release");

            var releaseBranch = repo.Branches[branchName];
            var firstCommit = releaseBranch.Tip;

            var sign = Constants.SignatureNow();
            repo.Tags.Add("0.4.0-RC4", branchingCommit, sign, "release");

            var finder = new ReleaseVersionFinder
            {
                Repository = repo,
                Commit = firstCommit,
                ReleaseBranch = releaseBranch,
            };

            var version = finder.FindVersion();
            //tag: 0.4.0-RC1 => 
            Assert.AreEqual(0, version.Version.Major);
            Assert.AreEqual(4, version.Version.Minor);
            Assert.AreEqual(0, version.Version.Patch);
            Assert.AreEqual(Stability.ReleaseCandidate, version.Version.Stability);
            Assert.AreEqual(BranchType.Release, version.BranchType);
            Assert.AreEqual(4, version.Version.PreReleasePartOne);
        }
    }

    [Test]
    public void EnsureAReleaseBranchNameDoesNotExposeAPatchSegment()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            const string branchName = "release-0.3.1";

            var branchingCommit = repo.Branches["develop"].Tip;
            var releaseBranch = repo.Branches.Add(branchName, branchingCommit);

            var finder = new ReleaseVersionFinder
            {
                Repository = repo,
                Commit = branchingCommit,
                ReleaseBranch = releaseBranch,
            };

            Assert.Throws<ErrorException>(() => finder.FindVersion());
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

            var finder = new ReleaseVersionFinder
            {
                Repository = repo,
                Commit = branchingCommit,
                ReleaseBranch = releaseBranch,
            };

            Assert.Throws<ErrorException>(() => finder.FindVersion());
        }
    }
        //TODO:
    //[Test]
    //[ExpectedException]
    //public void Override_stage_using_tag_should_throw_on_version_mismatch()
    //{
    //    var version = FinderWrapper.FindVersionForCommit("34dbc768fcbdd57d6089fe28f9d37472b9e97e35", "release-0.5.0");
    //}

    [Test, Ignore("Not really going to happen in real life se we skip this for now")]
    public void After_merge_to_master()
    {
        //TODO
        //Assert.Throws<Exception>(() => FinderWrapper.FindVersionForCommit("TODO", "release-0.5.0"));
    }

}