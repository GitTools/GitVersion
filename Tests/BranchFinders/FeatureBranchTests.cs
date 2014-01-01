using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;
using Tests.Helpers;

[TestFixture]
public class FeatureBranchTests : Lg2sHelperBase
{
    [Test]
    public void Feature_branch_with_no_commit()
    {
        //this scenario should redirect to the develop finder since there is no diff btw this branch and the develop branch

        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            var branchingCommit = repo.Branches["develop"].Tip;
            var featureBranch = repo.Branches.Add("featureWithNoCommits", branchingCommit);

            var finder = new FeatureVersionFinder();

            var version = finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                CurrentBranch = featureBranch
            });

            var masterVersion = FindersHelper.RetrieveMasterVersion(repo);

            Assert.AreEqual(masterVersion.Version.Major, version.Version.Major);
            Assert.AreEqual(masterVersion.Version.Minor + 1, version.Version.Minor, "Minor should be master.Minor+1");
            Assert.AreEqual(0, version.Version.Patch);
            Assert.AreEqual(Stability.Unstable, version.Version.Stability);
            Assert.AreEqual(BranchType.Feature, version.BranchType);
            Assert.AreEqual(null, version.Version.Suffix);
            Assert.AreEqual(3, version.Version.PreReleasePartOne, "Should be the number of commits ahead of master");
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
            var branchingCommit = repo.Head.Tip;

            AddOneCommitToHead(repo, "feature");

            var featureBranch = repo.Branches["featureWithOneCommit"];

            var finder = new FeatureVersionFinder();

            var version = finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                CurrentBranch = featureBranch
            });

            var masterVersion = FindersHelper.RetrieveMasterVersion(repo);

            Assert.AreEqual(masterVersion.Version.Major, version.Version.Major);
            Assert.AreEqual(masterVersion.Version.Minor + 1, version.Version.Minor, "Minor should be master.Minor+1");
            Assert.AreEqual(0, version.Version.Patch);
            Assert.AreEqual(Stability.Unstable, version.Version.Stability);
            Assert.AreEqual(BranchType.Feature, version.BranchType);
            Assert.AreEqual(branchingCommit.Prefix(), version.Version.Suffix, "Suffix should be the develop commit it was branched from");
            Assert.AreEqual(0, version.Version.PreReleasePartOne, "Prerelease is always 0 for feature branches");
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
            var branchingCommit = repo.Head.Tip;

            AddOneCommitToHead(repo, "feature");
            AddOneCommitToHead(repo, "feature");

            var featureBranch = repo.Branches["featureWithOneCommit"];

            var finder = new FeatureVersionFinder();

            var version = finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                CurrentBranch = featureBranch
            });

            var masterVersion = FindersHelper.RetrieveMasterVersion(repo);

            Assert.AreEqual(masterVersion.Version.Major, version.Version.Major);
            Assert.AreEqual(masterVersion.Version.Minor + 1, version.Version.Minor, "Minor should be master.Minor+1");
            Assert.AreEqual(0, version.Version.Patch);
            Assert.AreEqual(Stability.Unstable, version.Version.Stability);
            Assert.AreEqual(BranchType.Feature, version.BranchType);
            Assert.AreEqual(branchingCommit.Prefix(), version.Version.Suffix, "Suffix should be the develop commit it was branched from");
            Assert.AreEqual(0, version.Version.PreReleasePartOne, "Prerelease is always 0 for feature branches");
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
            var branchingCommit = repo.Head.Tip;

            AddOneCommitToHead(repo, "feature");
            AddOneCommitToHead(repo, "feature");

            var featureBranch = repo.Branches["featureWithOneCommit"];

            var finder = new FeatureVersionFinder();

            var version = finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                CurrentBranch = featureBranch,
            });

            var masterVersion = FindersHelper.RetrieveMasterVersion(repo);

            Assert.AreEqual(masterVersion.Version.Major, version.Version.Major);
            Assert.AreEqual(masterVersion.Version.Minor + 1, version.Version.Minor, "Minor should be master.Minor+1");
            Assert.AreEqual(0, version.Version.Patch);
            Assert.AreEqual(Stability.Unstable, version.Version.Stability);
            Assert.AreEqual(BranchType.Feature, version.BranchType);
            Assert.AreEqual(branchingCommit.Prefix(), version.Version.Suffix, "Suffix should be the develop commit it was branched from");
            Assert.AreEqual(0, version.Version.PreReleasePartOne, "Prerelease is always 0 for feature branches");
        }
    }
}
