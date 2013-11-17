using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;
using System.IO;
using Tests.Lg2sHelper;

[TestFixture]
public class GitFlowVersionFinderTests : Lg2sHelperBase
{
    [Test]
    public void FromExistingMaster()
    {
        using (var repo = new Repository(ASBMTestRepoWorkingDirPath))
        {
            const string branchName = "master";
            Branch master = repo.Branches[branchName];

            var gfvf = new GitFlowVersionFinder
            {
                Repository = repo,
                Branch = master,
                Commit = master.Tip
            };

            VersionAndBranch vab = gfvf.FindVersion();

            Assert.AreEqual(branchName, vab.BranchName);
            Assert.AreEqual(BranchType.Master, vab.BranchType);
            Assert.AreEqual(master.Tip.Sha, vab.Sha);

            SemanticVersion version = vab.Version;
            Assert.AreEqual(1, version.Major);
            Assert.AreEqual(0, version.Minor);
            Assert.AreEqual(1, version.Patch);
            Assert.AreEqual(Stability.Final, version.Stability);
        }
    }

    [Test]
    public void FromExistingDevelop()
    {
        using (var repo = new Repository(ASBMTestRepoWorkingDirPath))
        {
            const string branchName = "develop";
            Branch develop = repo.Branches[branchName];

            var gfvf = new GitFlowVersionFinder
            {
                Repository = repo,
                Branch = develop,
                Commit = develop.Tip
            };

            VersionAndBranch vab = gfvf.FindVersion();

            Assert.AreEqual(branchName, vab.BranchName);
            Assert.AreEqual(BranchType.Develop, vab.BranchType);
            Assert.AreEqual(develop.Tip.Sha, vab.Sha);

            SemanticVersion version = vab.Version;
            Assert.AreEqual(1, version.Major);
            Assert.AreEqual(1, version.Minor);
            Assert.AreEqual(0, version.Patch);
            Assert.AreEqual(Stability.Unstable, version.Stability);
        }
    }

    [Test]
    public void FromNewFeature()
    {
        string repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            const string branchName = "feature/new";
            repo.CreateBranch(branchName, repo.Branches["develop"].Tip).Checkout();

            string path = Path.Combine(repo.Info.WorkingDirectory, "README");
            File.AppendAllText(path, "Feature\n");

            repo.Index.Stage(path);
            Signature sign = Constants.SignatureNow();
            repo.Commit("feature new", sign, sign);

            var feature = repo.Branches[branchName];

            var gfvf = new GitFlowVersionFinder
            {
                Repository = repo,
                Branch = feature,
                Commit = feature.Tip
            };

            VersionAndBranch vab = gfvf.FindVersion();

            Assert.AreEqual(branchName, vab.BranchName);
            Assert.AreEqual(BranchType.Feature, vab.BranchType);
            Assert.AreEqual(feature.Tip.Sha, vab.Sha);

            SemanticVersion version = vab.Version;
            Assert.AreEqual(1, version.Major);
            Assert.AreEqual(1, version.Minor);
            Assert.AreEqual(0, version.Patch);
            Assert.AreEqual(Stability.Unstable, version.Stability);
        }
    }

    [Test]
    public void FromNewHotFix()
    {
        string repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            Branch master = repo.Branches["master"];

            const string branchName = "hotfix/1.0.2";
            repo.CreateBranch(branchName, master.Tip).Checkout();

            string path = Path.Combine(repo.Info.WorkingDirectory, "README");
            File.AppendAllText(path, "Hotfix\n");

            repo.Index.Stage(path);
            Signature sign = Constants.SignatureNow();
            repo.Commit("hotfix", sign, sign);

            var hotfix = repo.Branches[branchName];

            repo.Tags.Add("1.0.2-Beta1", hotfix.Tip, Constants.SignatureNow(), " ");

            var gfvf = new GitFlowVersionFinder
            {
                Repository = repo,
                Branch = hotfix,
                Commit = hotfix.Tip
            };

            VersionAndBranch vab = gfvf.FindVersion();

            Assert.AreEqual(branchName, vab.BranchName);
            Assert.AreEqual(BranchType.Hotfix, vab.BranchType);
            Assert.AreEqual(hotfix.Tip.Sha, vab.Sha);

            SemanticVersion version = vab.Version;
            Assert.AreEqual(1, version.Major);
            Assert.AreEqual(0, version.Minor);
            Assert.AreEqual(2, version.Patch);
            Assert.AreEqual(Stability.Beta, version.Stability);
            Assert.AreEqual(1, version.PreReleasePartOne);
        }
    }

    [Test]
    public void FromNewRelease()
    {
        string repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            Branch master = repo.Branches["develop"];

            const string branchName = "release/2.0.0";
            repo.CreateBranch(branchName, master.Tip).Checkout();

            string path = Path.Combine(repo.Info.WorkingDirectory, "README");
            File.AppendAllText(path, "Release\n");

            repo.Index.Stage(path);
            Signature sign = Constants.SignatureNow();
            repo.Commit("release", sign, sign);

            var release = repo.Branches[branchName];

            repo.Tags.Add("2.0.0-Beta1", release.Tip, Constants.SignatureNow(), " ");

            var gfvf = new GitFlowVersionFinder
            {
                Repository = repo,
                Branch = release,
                Commit = release.Tip
            };

            VersionAndBranch vab = gfvf.FindVersion();

            Assert.AreEqual(branchName, vab.BranchName);
            Assert.AreEqual(BranchType.Release, vab.BranchType);
            Assert.AreEqual(release.Tip.Sha, vab.Sha);

            SemanticVersion version = vab.Version;
            Assert.AreEqual(2, version.Major);
            Assert.AreEqual(0, version.Minor);
            Assert.AreEqual(0, version.Patch);
            Assert.AreEqual(Stability.Beta, version.Stability);
            Assert.AreEqual(1, version.PreReleasePartOne);
        }
    }
}
