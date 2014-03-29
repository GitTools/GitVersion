using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;
using System.IO;
using ObjectApproval;

[TestFixture]
public class GitVersionFinderTests : Lg2sHelperBase
{
    [Test]
    public void FromExistingMaster()
    {
        using (var repo = new Repository(ASBMTestRepoWorkingDirPath))
        {
            const string branchName = "master";
            var master = repo.Branches[branchName];

            var finder = new GitVersionFinder();

            var semanticVersion = finder.FindVersion(new GitVersionContext
            {
                Repository = repo,
                CurrentBranch = master,
            });

            Assert.AreEqual(branchName, semanticVersion.BuildMetaData.Branch);
            Assert.AreEqual(master.Tip.Sha, semanticVersion.BuildMetaData.Sha);

            ObjectApprover.VerifyWithJson(semanticVersion, Scrubbers.GuidScrubber);
        }
    }

    [Test]
    public void FromExistingDevelop()
    {
        using (var repo = new Repository(ASBMTestRepoWorkingDirPath))
        {
            const string branchName = "develop";
            var develop = repo.Branches[branchName];

            var finder = new GitVersionFinder();

            var semanticVersion = finder.FindVersion(new GitVersionContext
            {
                Repository = repo,
                CurrentBranch = develop,
            });

            Assert.AreEqual(branchName, semanticVersion.BuildMetaData.Branch);
            Assert.AreEqual(develop.Tip.Sha, semanticVersion.BuildMetaData.Sha);

            ObjectApprover.VerifyWithJson(semanticVersion, Scrubbers.GuidAndDateScrubber);
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

            var finder = new GitVersionFinder();

            var semanticVersion = finder.FindVersion(new GitVersionContext
            {
                Repository = repo,
                CurrentBranch = feature,
            });

            Assert.AreEqual(branchName, semanticVersion.BuildMetaData.Branch);
            Assert.AreEqual(feature.Tip.Sha, semanticVersion.BuildMetaData.Sha);

            ObjectApprover.VerifyWithJson(semanticVersion, Scrubbers.GuidAndDateScrubber);
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

            var finder = new GitVersionFinder();

            var semanticVersion = finder.FindVersion(new GitVersionContext
            {
                Repository = repo,
                CurrentBranch = hotfix
            });

            Assert.AreEqual(branchName, semanticVersion.BuildMetaData.Branch);
            Assert.AreEqual(hotfix.Tip.Sha, semanticVersion.BuildMetaData.Sha);

            ObjectApprover.VerifyWithJson(semanticVersion, Scrubbers.GuidAndDateScrubber);
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

            var finder = new GitVersionFinder();

            var versionAndBranch = finder.FindVersion(new GitVersionContext
            {
                Repository = repo,
                CurrentBranch = release,

            });

            ObjectApprover.VerifyWithJson(versionAndBranch, Scrubbers.GuidAndDateScrubber);
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

            var finder = new GitVersionFinder();

            Assert.Throws<ErrorException>(() => finder.FindVersion(new GitVersionContext
            {
                Repository = repo,
                CurrentBranch = repo.Head,
            }));
        }
    }

    [Test, Ignore("Need a way to enforce this check")]
    public void RequiresALocalDevelopBranch()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            repo.Branches["feature/one"].ForceCheckout();

            repo.Branches.Remove("develop");

            var finder = new GitVersionFinder();

            Assert.Throws<ErrorException>(() => finder.FindVersion(new GitVersionContext
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

            var finder = new GitVersionFinder();

            Assert.Throws<ErrorException>(() => finder.FindVersion(new GitVersionContext
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

            var finder = new GitVersionFinder();

            Assert.Throws<ErrorException>(() => finder.FindVersion(new GitVersionContext
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

            var finder = new GitVersionFinder();

            Assert.Throws<ErrorException>(() => finder.FindVersion(new GitVersionContext
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

            var finder = new GitVersionFinder();

            var versionAndBranch = finder.FindVersion(new GitVersionContext
            {
                Repository = repo,
                CurrentBranch = repo.Head,
            });

            ObjectApprover.VerifyWithJson(versionAndBranch, Scrubbers.GuidAndDateScrubber);
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

            var gfvf = new GitVersionFinder();

            Assert.Throws<ErrorException>(() => gfvf.FindVersion(new GitVersionContext
            {
                Repository = repo,
                CurrentBranch = feature,
            }));
        }
    }
}
