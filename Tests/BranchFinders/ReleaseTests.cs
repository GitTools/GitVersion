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