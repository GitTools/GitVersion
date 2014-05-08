using System;
using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class MetaDataByCommitFixture : Lg2sHelperBase
{
    /*
     *  hotfix-1.2.1       -----------C--      
     *                    /              \     
     *  master           A----------------F-----H-------N
     *                    \                    / \     /
     *  hotfix-1.3.1       \                  /   ----L
     *                      \                /         \
     *  release-1.3.0        \        -D----G---        \
     *                        \      /          \        \
     *  develop                -----B----E-------I-----M--O--P
     *                                    \           /
     *  feature                            -------J-K-
     * 
     *
     *  - A is tagged `1.2.0`
     *  - F is tagged `1.2.1`
     *  - H is tagged `1.3.0`
     *  - N is tagged `1.3.1`
    */

    [Test]
    public void CanCorrectlyDetectCommitCountsWhenThatApplies()
    {
        var repoPath = Clone(CCTestRepoWorkingDirPath);

        using (var repo = new Repository(repoPath))
        {
            ResetToP(repo);
            EnsureMetaDataMatch(repo, "develop", 7);

            ResetToO(repo);
            EnsureMetaDataMatch(repo, "develop", 6);

            ResetToN(repo);
            EnsureMetaDataMatch(repo, "master", null, r => (Commit)r.Tags["1.3.0"].Target);

            ResetToM(repo);
            EnsureMetaDataMatch(repo, "develop", 5);

            ResetToL(repo);
            EnsureMetaDataMatch(repo, "hotfix-1.3.1", 1, r => (Commit)r.Tags["1.3.0"].Target);

            ResetToK(repo);
            EnsureMetaDataMatch(repo, "feature", 2);

            ResetToJ(repo);
            EnsureMetaDataMatch(repo, "feature", 1);

            ResetToI(repo);
            EnsureMetaDataMatch(repo, "develop", 2);

            ResetToH(repo);
            EnsureMetaDataMatch(repo, "master", null, r => (Commit)r.Tags["1.3.0"].Target);

            ResetToG(repo);
            EnsureMetaDataMatch(repo, "release-1.3.0", 2);

            ResetToF(repo);
            EnsureMetaDataMatch(repo, "master", null, r => (Commit)r.Tags["1.2.0"].Target);

            ResetToE(repo);
            EnsureMetaDataMatch(repo, "develop", 2);

            ResetToD(repo);
            EnsureMetaDataMatch(repo, "release-1.3.0", 1);

            ResetToC(repo);
            EnsureMetaDataMatch(repo, "hotfix-1.2.1", 1, r => (Commit)r.Tags["1.2.0"].Target);

            ResetToB(repo);
            EnsureMetaDataMatch(repo, "develop", 1);
        }
    }

    static void EnsureMetaDataMatch(
        Repository repo, string branchName,
        int? expectedCommitCount, Func<Repository, Commit> commitFinder = null)
    {
        var sv = GetSemanticVersionFrom(repo, branchName);

        var referenceCommitFinder = commitFinder ?? (r => r.FindBranch(branchName).Tip);

        var commit = referenceCommitFinder(repo);

        Assert.AreEqual(expectedCommitCount, sv.BuildMetaData.CommitsSinceTag);

        var releaseDate = sv.BuildMetaData.ReleaseDate;
        Assert.AreEqual(commit.Sha, releaseDate.OriginalCommitSha);
        Assert.AreEqual(commit.Committer.When, releaseDate.OriginalDate);
    }

    static SemanticVersion GetSemanticVersionFrom(Repository repo, string branchName)
    {
        var gvf = new GitVersionFinder();
        var context = new GitVersionContext
                                {
                                    CurrentBranch = repo.Branches[branchName],
                                    Repository = repo
                                };

        var sv = gvf.FindVersion(context);
        return sv;
    }

    void DropTags(Repository repo, params string[] names)
    {
        foreach (var name in names)
        {
            if (repo.Tags[name] == null)
            {
                continue;
            }

            repo.Tags.Remove(name);
        }
    }

    void DropBranches(Repository repo, params string[] names)
    {
        foreach (var name in names)
        {
            if (repo.Branches[name] == null)
            {
                continue;
            }

            repo.Branches.Remove(name);
        }
    }

    void ResetBranch(Repository repo, string name, string committish)
    {
        var b = repo.Branches[name];
        Assert.IsNotNull(b);
        repo.Refs.UpdateTarget(b.CanonicalName, committish);
    }

    void ResetToP(Repository repo)
    {
        ResetBranch(repo, "develop", "4d65c519f88773854f9345eaf5dbb30cb49f6a74");
    }

    void ResetToO(Repository repo)
    {
        ResetBranch(repo, "develop", "7655537837096d925a4f974232f78ec589d86ebd");
    }

    void ResetToN(Repository repo)
    {
        ResetBranch(repo, "develop", "0b7a2482ab7d167cefa4ecfc106db001dc5c17ff");
    }

    void ResetToM(Repository repo)
    {
        ResetBranch(repo, "develop", "0b7a2482ab7d167cefa4ecfc106db001dc5c17ff");
        ResetBranch(repo, "master", "5b84136c848fd48f1f8b3fa4e1b767a1f6101279");
        DropTags(repo, "1.3.1");
    }

    void ResetToL(Repository repo)
    {
        ResetBranch(repo, "develop", "243f56dcdb543688fd0a99bd3e0e72dd9a786603");
    }

    void ResetToK(Repository repo)
    {
        DropBranches(repo, "hotfix-1.3.1");
    }

    void ResetToJ(Repository repo)
    {
        ResetBranch(repo, "feature", "0491c5dac30d706f4e54c5cb26d082baad8228d1");
    }

    void ResetToI(Repository repo)
    {
        DropBranches(repo, "feature");
    }

    void ResetToH(Repository repo)
    {
        ResetBranch(repo, "develop", "320f4b6820cf4b0853dc08ac153f04fbd4958200");
    }

    void ResetToG(Repository repo)
    {
        ResetBranch(repo, "master", "576a28e321cd6dc764b52c5fface672fa076f37f");
        DropTags(repo, "1.3.0");
    }

    void ResetToF(Repository repo)
    {
        ResetBranch(repo, "release-1.3.0", "b53054c614d36edc9d1bee8c35cd2ed575a43607");
    }

    void ResetToE(Repository repo)
    {
        ResetBranch(repo, "master", "8c890487ed143d5a72d151e64be1c5ddb314c908");
        DropTags(repo, "1.2.1");
    }

    void ResetToD(Repository repo)
    {
        ResetBranch(repo, "develop", "fab69e28ee35dd912c0c95d5993dd84e4f2bcd92");
    }

    void ResetToC(Repository repo)
    {
        DropBranches(repo, "release-1.3.0");
    }

    void ResetToB(Repository repo)
    {
        DropBranches(repo, "hotfix-1.2.1");
    }
}
