using System.IO;
using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class GitVersionFinderTests : Lg2sHelperBase
{
    [Test]
    public void RequiresALocalMasterBranch()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            repo.Branches["feature/one"].ForceCheckout();

            repo.Branches.Remove("master");

            var finder = new GitVersionFinder();

            Assert.Throws<WarningException>(() => finder.FindVersion(new GitVersionContext(repo)));
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

            Assert.Throws<WarningException>(() => finder.FindVersion(new GitVersionContext(repo)));
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

            Assert.Throws<WarningException>(() => finder.FindVersion(new GitVersionContext(repo, feature)));
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

            Assert.Throws<WarningException>(() => finder.FindVersion(new GitVersionContext(repo, feature)));
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

            Assert.Throws<WarningException>(() => finder.FindVersion(new GitVersionContext(repo, pull)));
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

            var versionAndBranch = finder.FindVersion(new GitVersionContext(repo));

            ObjectApprover.VerifyWithJson(versionAndBranch, Scrubbers.GuidAndDateScrubber);
        }
    }

    [Test]
    public void AFeatureBranchPrefixIsNotIncludedInTag()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            repo.Branches["develop"].ForceCheckout();

            const string branchName = "feature/ABC-1234_SomeDescription";
            repo.Branches.Add(branchName, repo.Head.Tip).ForceCheckout();

            AddOneCommitToHead(repo, "code");

            var finder = new GitVersionFinder();

            var versionAndBranch = finder.FindVersion(new GitVersionContext(repo));

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
            var sign = SignatureBuilder.SignatureNow();
            repo.Commit("release unborn", sign, sign);

            var feature = repo.Branches[branchName];

            var finder = new GitVersionFinder();

            Assert.Throws<WarningException>(() => finder.FindVersion(new GitVersionContext(repo, feature)));
        }
    }
}