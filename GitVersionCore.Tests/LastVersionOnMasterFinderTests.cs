using System;
using GitVersion;
using GitVersion.Configuration;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class LastVersionOnMasterFinderTests
{
    
    [Test]
    public void WhenMasterHasPatchTagEnsureLastMinorTagIsUsed()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            var stamp = new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero);
            fixture.Repository.MakeACommit(stamp);
            fixture.Repository.ApplyTag("1.2.0");
            fixture.Repository.MakeACommit(stamp); 
            fixture.Repository.ApplyTag("1.2.1");
            fixture.Repository.MakeACommit();

            var dateTimeOffset = LastMinorVersionFinder.Execute(fixture.Repository, fixture.Repository.Head.Tip);
            Assert.AreEqual(stamp,dateTimeOffset);
        }
    }

    [Test]
    public void WhenNoTagsOrMergeCommitsShouldUseFirstCommit()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            var stamp = new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero);
            fixture.Repository.MakeACommit(stamp);

            var dateTimeOffset = LastMinorVersionFinder.Execute(fixture.Repository, fixture.Repository.Head.Tip);
            Assert.AreEqual(stamp,dateTimeOffset);
        }
    }
    [Test]
    public void WhenSupportIsBranchedFromMasterEnsureLastMinorTagIsUsed()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            var stamp = new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero);
            fixture.Repository.MakeACommit(stamp);
            fixture.Repository.ApplyTag("1.2.0");
            fixture.Repository.CreateBranch("Support-1.2.0");
            fixture.Repository.Checkout("Support-1.2.0");
            fixture.Repository.MakeACommit();
            
            var dateTimeOffset = LastMinorVersionFinder.Execute(fixture.Repository, fixture.Repository.Head.Tip);
            Assert.AreEqual(stamp,dateTimeOffset);
        }
    }

    [Test]
    public void WhenSupportIsBranchedAndTaggedFromAnotherSupportEnsureNewMinorIsUsed()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("Support-1.2.0");
            fixture.Repository.Checkout("Support-1.2.0");
            fixture.Repository.MakeACommit();
            fixture.Repository.ApplyTag("1.2.0");

            fixture.Repository.CreateBranch("Support-1.3.0");
            fixture.Repository.Checkout("Support-1.3.0");
            var stamp = new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero);
            var commit = fixture.Repository.MakeACommit(stamp);
            fixture.Repository.ApplyTag("1.3.0", commit.Sha);

            //Move On
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();

            
            var dateTimeOffset = LastMinorVersionFinder.Execute(fixture.Repository, fixture.Repository.Head.Tip);
            Assert.AreEqual(stamp,dateTimeOffset);
        }
    }
    
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
    public void CanCorrectlyDetectCommitCountsAndReleaseDataWhenThatApplies()
    {
        using (var f = new CommitCountingRepoFixture())
        {
            ResetToP(f.Repository);
            EnsureMetaDataMatch(f, "develop");

            ResetToO(f.Repository);
            EnsureMetaDataMatch(f, "develop");

            ResetToN(f.Repository);
            EnsureMetaDataMatch(f, "master", r => (Commit)r.Tags["1.3.0"].Target);

            ResetToM(f.Repository);
            EnsureMetaDataMatch(f, "develop");

            ResetToL(f.Repository);
            EnsureMetaDataMatch(f, "hotfix-1.3.1", r => (Commit)r.Tags["1.3.0"].Target);

            ResetToK(f.Repository);
            EnsureMetaDataMatch(f, "feature");

            ResetToJ(f.Repository);
            EnsureMetaDataMatch(f, "feature");

            ResetToI(f.Repository);
            EnsureMetaDataMatch(f, "develop");

            ResetToH(f.Repository);
            EnsureMetaDataMatch(f, "master", r => (Commit)r.Tags["1.3.0"].Target);

            ResetToG(f.Repository);
            EnsureMetaDataMatch(f, "release-1.3.0");

            ResetToF(f.Repository);
            EnsureMetaDataMatch(f, "master", r => (Commit)r.Tags["1.2.0"].Target);

            ResetToE(f.Repository);
            EnsureMetaDataMatch(f, "develop");

            ResetToD(f.Repository);
            EnsureMetaDataMatch(f, "release-1.3.0");

            ResetToC(f.Repository);
            EnsureMetaDataMatch(f, "hotfix-1.2.1", r => (Commit)r.Tags["1.2.0"].Target);

            ResetToB(f.Repository);
            EnsureMetaDataMatch(f, "develop");
        }
    }

    static void EnsureMetaDataMatch(CommitCountingRepoFixture fixture, string branchName, Func<IRepository, Commit> commitFinder = null)
    {
        var referenceCommitFinder = commitFinder ?? (r => r.FindBranch(branchName).Tip);

        var commit = referenceCommitFinder(fixture.Repository);
        var releaseDate = LastMinorVersionFinder.Execute(fixture.Repository, commit);
        releaseDate.ShouldBe(commit.When());
    }

    void DropTags(IRepository repo, params string[] names)
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

    void DropBranches(IRepository repo, params string[] names)
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

    void ResetBranch(IRepository repo, string name, string committish)
    {
        var b = repo.Branches[name];
        Assert.NotNull(b);
        repo.Refs.UpdateTarget(b.CanonicalName, committish);
    }

    void ResetToP(IRepository repo)
    {
        ResetBranch(repo, "develop", "4d65c519f88773854f9345eaf5dbb30cb49f6a74");
    }

    void ResetToO(IRepository repo)
    {
        ResetBranch(repo, "develop", "7655537837096d925a4f974232f78ec589d86ebd");
    }

    void ResetToN(IRepository repo)
    {
        ResetBranch(repo, "develop", "0b7a2482ab7d167cefa4ecfc106db001dc5c17ff");
        repo.Refs.UpdateTarget("HEAD", "refs/heads/master");
    }

    void ResetToM(IRepository repo)
    {
        ResetBranch(repo, "develop", "0b7a2482ab7d167cefa4ecfc106db001dc5c17ff");
        ResetBranch(repo, "master", "5b84136c848fd48f1f8b3fa4e1b767a1f6101279");
        DropTags(repo, "1.3.1");
        repo.Refs.UpdateTarget("HEAD", "refs/heads/develop");
    }

    void ResetToL(IRepository repo)
    {
        ResetBranch(repo, "develop", "243f56dcdb543688fd0a99bd3e0e72dd9a786603");
        repo.Refs.UpdateTarget("HEAD", "refs/heads/hotfix-1.3.1");
    }

    void ResetToK(IRepository repo)
    {
        repo.Refs.UpdateTarget("HEAD", "refs/heads/feature");
        DropBranches(repo, "hotfix-1.3.1");
    }

    void ResetToJ(IRepository repo)
    {
        ResetBranch(repo, "feature", "0491c5dac30d706f4e54c5cb26d082baad8228d1");
    }

    void ResetToI(IRepository repo)
    {
        repo.Refs.UpdateTarget("HEAD", "refs/heads/develop");
        DropBranches(repo, "feature");
    }

    void ResetToH(IRepository repo)
    {
        ResetBranch(repo, "develop", "320f4b6820cf4b0853dc08ac153f04fbd4958200");
        repo.Refs.UpdateTarget("HEAD", "refs/heads/master");
    }

    void ResetToG(IRepository repo)
    {
        ResetBranch(repo, "master", "576a28e321cd6dc764b52c5fface672fa076f37f");
        repo.Refs.UpdateTarget("HEAD", "refs/heads/release-1.3.0");
        DropTags(repo, "1.3.0");
    }

    void ResetToF(IRepository repo)
    {
        ResetBranch(repo, "release-1.3.0", "b53054c614d36edc9d1bee8c35cd2ed575a43607");
        repo.Refs.UpdateTarget("HEAD", "refs/heads/master");
    }

    void ResetToE(IRepository repo)
    {
        ResetBranch(repo, "master", "8c890487ed143d5a72d151e64be1c5ddb314c908");
        repo.Refs.UpdateTarget("HEAD", "refs/heads/develop");
        DropTags(repo, "1.2.1");
    }

    void ResetToD(IRepository repo)
    {
        ResetBranch(repo, "develop", "fab69e28ee35dd912c0c95d5993dd84e4f2bcd92");
        repo.Refs.UpdateTarget("HEAD", "refs/heads/release-1.3.0");
    }

    void ResetToC(IRepository repo)
    {
        repo.Refs.UpdateTarget("HEAD", "refs/heads/hotfix-1.2.1");
        DropBranches(repo, "release-1.3.0");
    }

    void ResetToB(IRepository repo)
    {
        repo.Refs.UpdateTarget("HEAD", "refs/heads/develop");
        DropBranches(repo, "hotfix-1.2.1");
    }
}