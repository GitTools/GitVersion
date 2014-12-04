using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class FeatureBranchTests : Lg2sHelperBase
{
    [Test]
    [Ignore] //TODO Delete?
    public void Feature_branch_with_no_commit()
    {
        //this scenario should redirect to the develop finder since there is no diff btw this branch and the develop branch

        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            var branchingCommit = repo.Branches["develop"].Tip;
            var featureBranch = repo.Branches.Add("featureWithNoCommits", branchingCommit);

            var finder = new FeatureVersionFinder();

            var configuration = new Config();
            var version = finder.FindVersion(new GitVersionContext(repo, featureBranch, configuration));

            var masterVersion = FindersHelper.RetrieveMasterVersion(repo, configuration);

            Assert.AreEqual(masterVersion.Minor + 1, version.Minor, "Minor should be master.Minor+1");
            ObjectApprover.VerifyWithJson(version, Scrubbers.GuidScrubber);
        }
    }

    [Test]
    public void Feature_branch_with_1_commit()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            // Create a feature branch from the parent of current develop tip
            repo.Branches.Add("featureWithOneCommit", "develop~").ForceCheckout();
            //var branchingCommit = repo.Head.Tip;

            AddOneCommitToHead(repo, "feature");

            var featureBranch = repo.Branches["featureWithOneCommit"];

            var finder = new FeatureVersionFinder();

            var configuration = new Config();
            var version = finder.FindVersion(new GitVersionContext(repo, featureBranch, configuration));

            var masterVersion = FindersHelper.RetrieveMasterVersion(repo, configuration);

            Assert.AreEqual(masterVersion.Minor + 1, version.Minor, "Minor should be master.Minor+1");
            //TODO Assert.AreEqual(branchingCommit.Prefix(), version.Version.Suffix, "Suffix should be the develop commit it was branched from");

            ObjectApprover.VerifyWithJson(version, Scrubbers.GuidAndDateScrubber);
        }
    }

    [Test]
    public void Feature_branch_with_2_commits()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            // Create a feature branch from the parent of current develop tip
            repo.Branches.Add("featureWithOneCommit", "develop~").ForceCheckout();
            //var branchingCommit = repo.Head.Tip;

            AddOneCommitToHead(repo, "feature");
            AddOneCommitToHead(repo, "feature");

            var featureBranch = repo.Branches["featureWithOneCommit"];

            var finder = new FeatureVersionFinder();

            var configuration = new Config();
            var version = finder.FindVersion(new GitVersionContext(repo, featureBranch, configuration));

            var masterVersion = FindersHelper.RetrieveMasterVersion(repo, configuration);

            Assert.AreEqual(masterVersion.Minor + 1, version.Minor, "Minor should be master.Minor+1");
            //TODO Assert.AreEqual(branchingCommit.Prefix(), version.Version.Suffix, "Suffix should be the develop commit it was branched from");
            ObjectApprover.VerifyWithJson(version, Scrubbers.GuidAndDateScrubber);
        }
    }

    [Test]
    public void Feature_branch_with_2_commits_but_building_an_commit()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            // Create a feature branch from the parent of current develop tip
            repo.Branches.Add("featureWithOneCommit", "develop~").ForceCheckout();
            //var branchingCommit = repo.Head.Tip;

            AddOneCommitToHead(repo, "feature");
            AddOneCommitToHead(repo, "feature");

            var featureBranch = repo.Branches["featureWithOneCommit"];

            var finder = new FeatureVersionFinder();

            var configuration = new Config();
            var version = finder.FindVersion(new GitVersionContext(repo, featureBranch, configuration));

            var masterVersion = FindersHelper.RetrieveMasterVersion(repo, configuration);

            Assert.AreEqual(masterVersion.Minor + 1, version.Minor, "Minor should be master.Minor+1");
            //TODO Assert.AreEqual(branchingCommit.Prefix(), version.Version.Suffix, "Suffix should be the develop commit it was branched from");
            ObjectApprover.VerifyWithJson(version, Scrubbers.GuidAndDateScrubber);
        }
    }
}