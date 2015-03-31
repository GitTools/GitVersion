using System;
using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class VersionAndBranchFinderTests: Lg2sHelperBase
{
    [Test]
    public void ShouldBeAbleGetVersionFromGitDir()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        using (var repo = new Repository(repoPath))
        {
            // Create a pull request branch from the parent of current develop tip
            repo.Branches.Add("pull/1735/merge", "develop~").ForceCheckout();

            AddOneCommitToHead(repo, "code");

        }

        Tuple<CachedVersion, GitVersionContext> versionAndBranch;
        VersionAndBranchFinder.TryGetVersion(ASBMTestRepoWorkingDirPath, out versionAndBranch, new Config(), false);
        Assert.IsNotNull(versionAndBranch);
    }
}
