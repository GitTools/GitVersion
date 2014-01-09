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

            var finder = new GitFlowVersionFinder();

            var versionAndBranch = finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                CurrentBranch = master,
            });

            Assert.AreEqual(branchName, versionAndBranch.BranchName);
            Assert.AreEqual(BranchType.Master, versionAndBranch.BranchType);
            Assert.AreEqual(master.Tip.Sha, versionAndBranch.Sha);

            var version = versionAndBranch.Version;
            Assert.AreEqual(1, version.Major);
            Assert.AreEqual(0, version.Minor);
            Assert.AreEqual(1, version.Patch);
            Assert.Null(version.Tag);
        }
    }

    [Test]
    public void FromExistingDevelop()
    {
        using (var repo = new Repository(ASBMTestRepoWorkingDirPath))
        {
            const string branchName = "develop";
            var develop = repo.Branches[branchName];

            var finder = new GitFlowVersionFinder();

            var versionAndBranch = finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                CurrentBranch = develop,
            });

            Assert.AreEqual(branchName, versionAndBranch.BranchName);
            Assert.AreEqual(BranchType.Develop, versionAndBranch.BranchType);
            Assert.AreEqual(develop.Tip.Sha, versionAndBranch.Sha);

            var version = versionAndBranch.Version;
            Assert.AreEqual(1, version.Major);
            Assert.AreEqual(1, version.Minor);
            Assert.AreEqual(0, version.Patch);
            Assert.AreEqual("unstable3", version.Tag);
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

            var finder = new GitFlowVersionFinder();

            var versionAndBranch = finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                CurrentBranch = feature,
            });

            Assert.AreEqual(branchName, versionAndBranch.BranchName);
            Assert.AreEqual(BranchType.Feature, versionAndBranch.BranchType);
            Assert.AreEqual(feature.Tip.Sha, versionAndBranch.Sha);

            var version = versionAndBranch.Version;
            Assert.AreEqual(1, version.Major);
            Assert.AreEqual(1, version.Minor);
            Assert.AreEqual(0, version.Patch);
            Assert.AreEqual("unstable0", version.Tag);
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

            var finder = new GitFlowVersionFinder();

            var versionAndBranch = finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                CurrentBranch = hotfix,

            });

            Assert.AreEqual(branchName, versionAndBranch.BranchName);
            Assert.AreEqual(BranchType.Hotfix, versionAndBranch.BranchType);
            Assert.AreEqual(hotfix.Tip.Sha, versionAndBranch.Sha);

            var version = versionAndBranch.Version;
            Assert.AreEqual(1, version.Major);
            Assert.AreEqual(0, version.Minor);
            Assert.AreEqual(2, version.Patch);
            Assert.AreEqual("Beta1", version.Tag);
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

            var finder = new GitFlowVersionFinder();

            var versionAndBranch = finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                CurrentBranch = release,

            });

            Assert.AreEqual(branchName, versionAndBranch.BranchName);
            Assert.AreEqual(BranchType.Release, versionAndBranch.BranchType);
            Assert.AreEqual(release.Tip.Sha, versionAndBranch.Sha);

            var version = versionAndBranch.Version;
            Assert.AreEqual(2, version.Major);
            Assert.AreEqual(0, version.Minor);
            Assert.AreEqual(0, version.Patch);
            Assert.AreEqual("Beta1", version.Tag);
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

            var finder = new GitFlowVersionFinder();

            Assert.Throws<ErrorException>(() => finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                CurrentBranch = repo.Head,
            }));
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

            var finder = new GitFlowVersionFinder();

            Assert.Throws<ErrorException>(() => finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                CurrentBranch = repo.Head,
            }));
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

            var finder = new GitFlowVersionFinder();

            Assert.Throws<ErrorException>(() => finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                CurrentBranch = feature,
            }));
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

            var finder = new GitFlowVersionFinder();

            Assert.Throws<ErrorException>(() => finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                CurrentBranch = feature,
            }));
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

            var finder = new GitFlowVersionFinder();

            Assert.Throws<ErrorException>(() => finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                CurrentBranch = pull,
            }));
        }
    }

    [Test]
    public void AFeatureBranchDoesNotRequireASpecificPrefix()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            repo.Branches["develop"].ForceCheckout();

            const string branchName = "every-feature-is-welcome";
            repo.Branches.Add(branchName, repo.Head.Tip).ForceCheckout();

            AddOneCommitToHead(repo, "code");

            var finder = new GitFlowVersionFinder();

            var versionAndBranch = finder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                CurrentBranch = repo.Head,
            });

            Assert.AreEqual(branchName, versionAndBranch.BranchName);
            Assert.AreEqual(BranchType.Feature, versionAndBranch.BranchType);
            Assert.AreEqual(repo.Head.Tip.Sha, versionAndBranch.Sha);

            var version = versionAndBranch.Version;
            Assert.AreEqual(1, version.Major);
            Assert.AreEqual(1, version.Minor);
            Assert.AreEqual(0, version.Patch);
            Assert.AreEqual("unstable0", version.Tag);
            Assert.AreEqual(repo.Branches["develop"].Tip.Prefix(), version.Suffix);
        }
    }

    [Test]
    public void AReleaseBranchIsRequiredToBranchOffOfDevelopBranch()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            const string branchName = "release/1.2.0";

            // Create a new unborn release branch sharing no history with "develop"
            repo.Refs.UpdateTarget(repo.Refs.Head.CanonicalName, "refs/heads/" + branchName);

            var path = Path.Combine(repo.Info.WorkingDirectory, "README");
            File.AppendAllText(path, "Release\n");

            repo.Index.Stage(path);
            var sign = Constants.SignatureNow();
            repo.Commit("release unborn", sign, sign);

            var feature = repo.Branches[branchName];

            var gfvf = new GitFlowVersionFinder();

            Assert.Throws<ErrorException>(() => gfvf.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                CurrentBranch = feature,
            }));
        }
    }
}
