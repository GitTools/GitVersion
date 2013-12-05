using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;
using System.IO;
using Tests.Helpers;

[TestFixture]
public class GitFlowVersionFinderTests : Lg2sHelperBase
{
    [Test]
    public void FromExistingMaster()
    {
        using (var repo = new Repository(ASBMTestRepoWorkingDirPath))
        {
            const string branchName = "master";
            var master = repo.Branches[branchName];

            var gfvf = new GitFlowVersionFinder
            {
                Repository = repo,
                Branch = master,
                Commit = master.Tip
            };

            var vab = gfvf.FindVersion();

            Assert.AreEqual(branchName, vab.BranchName);
            Assert.AreEqual(BranchType.Master, vab.BranchType);
            Assert.AreEqual(master.Tip.Sha, vab.Sha);

            var version = vab.Version;
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
            var develop = repo.Branches[branchName];

            var gfvf = new GitFlowVersionFinder
            {
                Repository = repo,
                Branch = develop,
                Commit = develop.Tip
            };

            var vab = gfvf.FindVersion();

            Assert.AreEqual(branchName, vab.BranchName);
            Assert.AreEqual(BranchType.Develop, vab.BranchType);
            Assert.AreEqual(develop.Tip.Sha, vab.Sha);

            var version = vab.Version;
            Assert.AreEqual(1, version.Major);
            Assert.AreEqual(1, version.Minor);
            Assert.AreEqual(0, version.Patch);
            Assert.AreEqual(Stability.Unstable, version.Stability);
        }
    }

    [Test]
    public void FromNewFeature()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            const string branchName = "feature/new";
            repo.CreateBranch(branchName, repo.Branches["develop"].Tip).Checkout();

            var path = Path.Combine(repo.Info.WorkingDirectory, "README");
            File.AppendAllText(path, "Feature\n");

            repo.Index.Stage(path);
            var sign = Constants.SignatureNow();
            repo.Commit("feature new", sign, sign);

            var feature = repo.Branches[branchName];

            var gfvf = new GitFlowVersionFinder
            {
                Repository = repo,
                Branch = feature,
                Commit = feature.Tip
            };

            var vab = gfvf.FindVersion();

            Assert.AreEqual(branchName, vab.BranchName);
            Assert.AreEqual(BranchType.Feature, vab.BranchType);
            Assert.AreEqual(feature.Tip.Sha, vab.Sha);

            var version = vab.Version;
            Assert.AreEqual(1, version.Major);
            Assert.AreEqual(1, version.Minor);
            Assert.AreEqual(0, version.Patch);
            Assert.AreEqual(Stability.Unstable, version.Stability);
        }
    }

    [Test]
    public void FromNewHotFix()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            var master = repo.Branches["master"];

            const string branchName = "hotfix/1.0.2";
            repo.CreateBranch(branchName, master.Tip).Checkout();

            var path = Path.Combine(repo.Info.WorkingDirectory, "README");
            File.AppendAllText(path, "Hotfix\n");

            repo.Index.Stage(path);
            var sign = Constants.SignatureNow();
            repo.Commit("hotfix", sign, sign);

            var hotfix = repo.Branches[branchName];

            repo.Tags.Add("1.0.2-Beta1", hotfix.Tip, Constants.SignatureNow(), " ");

            var gfvf = new GitFlowVersionFinder
            {
                Repository = repo,
                Branch = hotfix,
                Commit = hotfix.Tip
            };

            var vab = gfvf.FindVersion();

            Assert.AreEqual(branchName, vab.BranchName);
            Assert.AreEqual(BranchType.Hotfix, vab.BranchType);
            Assert.AreEqual(hotfix.Tip.Sha, vab.Sha);

            var version = vab.Version;
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
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            var master = repo.Branches["develop"];

            const string branchName = "release/2.0.0";
            repo.CreateBranch(branchName, master.Tip).Checkout();

            var path = Path.Combine(repo.Info.WorkingDirectory, "README");
            File.AppendAllText(path, "Release\n");

            repo.Index.Stage(path);
            var sign = Constants.SignatureNow();
            repo.Commit("release", sign, sign);

            var release = repo.Branches[branchName];

            repo.Tags.Add("2.0.0-Beta1", release.Tip, Constants.SignatureNow(), " ");

            var gfvf = new GitFlowVersionFinder
            {
                Repository = repo,
                Branch = release,
                Commit = release.Tip
            };

            var vab = gfvf.FindVersion();

            Assert.AreEqual(branchName, vab.BranchName);
            Assert.AreEqual(BranchType.Release, vab.BranchType);
            Assert.AreEqual(release.Tip.Sha, vab.Sha);

            var version = vab.Version;
            Assert.AreEqual(2, version.Major);
            Assert.AreEqual(0, version.Minor);
            Assert.AreEqual(0, version.Patch);
            Assert.AreEqual(Stability.Beta, version.Stability);
            Assert.AreEqual(1, version.PreReleasePartOne);
        }
    }

    [Test]
    public void RequiresALocalMasterBranch()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            repo.Branches["feature/one"].ForceCheckout();

            repo.Branches.Remove("master");

            var gfvf = new GitFlowVersionFinder
            {
                Repository = repo,
                Branch = repo.Head,
                Commit = repo.Head.Tip
            };

            Assert.Throws<ErrorException>(() => gfvf.FindVersion());
        }
    }

    [Test]
    public void RequiresALocalDevelopBranch()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            repo.Branches["feature/one"].ForceCheckout();

            repo.Branches.Remove("develop");

            var gfvf = new GitFlowVersionFinder
            {
                Repository = repo,
                Branch = repo.Head,
                Commit = repo.Head.Tip
            };

            Assert.Throws<ErrorException>(() => gfvf.FindVersion());
        }
    }

    [Test]
    public void AFeatureBranchIsRequiredToBranchOffOfDevelopBranch()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            const string branchName = "feature/unborn";

            // Create a new unborn feature branch sharing no history with "develop"
            repo.Refs.UpdateTarget(repo.Refs.Head.CanonicalName, "refs/heads/" + branchName);

            AddOneCommitToHead(repo, "feature");

            var feature = repo.Branches[branchName];

            var gfvf = new GitFlowVersionFinder
            {
                Repository = repo,
                Branch = feature,
                Commit = feature.Tip
            };

            Assert.Throws<ErrorException>(() => gfvf.FindVersion());
        }
    }

    [Test]
    public void AHotfixBranchIsRequiredToBranchOffOfMasterBranch()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            const string branchName = "hotfix/1.0.2";

            // Create a new unborn hotfix branch sharing no history with "master"
            repo.Refs.UpdateTarget(repo.Refs.Head.CanonicalName, "refs/heads/" + branchName);

            AddOneCommitToHead(repo, "hotfix");

            var feature = repo.Branches[branchName];

            var gfvf = new GitFlowVersionFinder
            {
                Repository = repo,
                Branch = feature,
                Commit = feature.Tip
            };

            Assert.Throws<ErrorException>(() => gfvf.FindVersion());
        }
    }

    [Test]
    public void APullRequestBranchIsRequiredToBranchOffOfDevelopBranch()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            const string branchName = "pull/1735/merge";

            // Create a new unborn pull request branch sharing no history with "develop"
            repo.Refs.UpdateTarget(repo.Refs.Head.CanonicalName, "refs/heads/" + branchName);

            AddOneCommitToHead(repo, "code");

            var pull = repo.Branches[branchName];

            var gfvf = new GitFlowVersionFinder
            {
                Repository = repo,
                Branch = pull,
                Commit = pull.Tip
            };

            Assert.Throws<ErrorException>(() => gfvf.FindVersion());
        }
    }

    [Test]
    public void CannotBuildAVersionFromAnBranchWithAnInvalidPrefix()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            repo.Branches.Add("members-only", repo.Head.Tip).ForceCheckout();

            var gfvf = new GitFlowVersionFinder
            {
                Repository = repo,
                Branch = repo.Head,
                Commit = repo.Head.Tip
            };

            Assert.Throws<ErrorException>(() => gfvf.FindVersion());
        }
    }
}
