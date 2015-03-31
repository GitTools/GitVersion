using System.Diagnostics;
using System.Linq;
using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class GitHelperTests : Lg2sHelperBase
{
    [Test]
    public void CanDetermineTheVersionFromAFetchedMaster()
    {
        var gitDirectory = FakeTeamCityFetchAndCheckout(ASBMTestRepoWorkingDirPath, "refs/heads/master");

        var authentication = new Authentication();
        GitHelper.NormalizeGitDirectory(gitDirectory, authentication, false);

        using (var repository = new Repository(gitDirectory))
        {
            var semanticVersion = new GitVersionFinder().FindVersion(new GitVersionContext(repository, new Config()));
            Assert.IsNotNull(semanticVersion);
        }
    }

    [Test]
    public void CanDetermineTheVersionFromAPullRequest()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);
        CreateFakePullRequest(repoPath, "1735");

        var gitDirectory = FakeTeamCityFetchAndCheckout(repoPath, "refs/pull/1735/merge");

        var authentication = new Authentication();
        GitHelper.NormalizeGitDirectory(gitDirectory, authentication, false);

        using (var repository = new Repository(gitDirectory))
        {
            var semanticVersion = new GitVersionFinder().FindVersion(new GitVersionContext(repository, new Config()));
            Assert.IsNotNull(semanticVersion);
        }
    }

    [Test]
    public void CanDetermineTheVersionFromAFetchedDevelop()
    {
        var gitDirectory = FakeTeamCityFetchAndCheckout(ASBMTestRepoWorkingDirPath, "refs/heads/develop");

        var authentication = new Authentication();
        GitHelper.NormalizeGitDirectory(gitDirectory, authentication, false);

        using (var repository = new Repository(gitDirectory))
        {
            var semanticVersion = new GitVersionFinder().FindVersion(new GitVersionContext(repository, new Config()));
            Assert.IsNotNull(semanticVersion);
        }
    }

    [Test]
    public void CanDetermineTheVersionFromAFetchedFeature()
    {
        var gitDirectory = FakeTeamCityFetchAndCheckout(ASBMTestRepoWorkingDirPath, "refs/heads/feature/one");

        var authentication = new Authentication();
        GitHelper.NormalizeGitDirectory(gitDirectory, authentication, false);

        using (var repository = new Repository(gitDirectory))
        {
            repository.DumpGraph();
            var semanticVersion = new GitVersionFinder().FindVersion(new GitVersionContext(repository, new Config()));
            Assert.IsNotNull(semanticVersion);
        }
    }

    static void CreateFakePullRequest(string repoPath, string issueNumber)
    {
        // Fake an upstream repository as it would appear on GitHub
        // will pull requests stored under the refs/pull/ namespace
        using (var repo = new Repository(repoPath))
        {
            var branch = repo.CreateBranch("temp", repo.Branches["develop"].Tip);
            branch.Checkout();

            AddOneCommitToHead(repo, "code");
            AddOneCommitToHead(repo, "code");

            var c = repo.Head.Tip;
            repo.Refs.Add(string.Format("refs/pull/{0}/head", issueNumber), c.Id);

            var sign = SignatureBuilder.SignatureNow();
            var m = repo.ObjectDatabase.CreateCommit(
                sign, sign,
                string.Format("Merge pull request #{0} from nulltoken/ntk/fix/{0}", issueNumber)
                , c.Tree, new[] { repo.Branches["develop"].Tip, c }, true);

            repo.Refs.Add(string.Format("refs/pull/{0}/merge", issueNumber), m.Id);

            repo.Checkout("develop");
            repo.Branches.Remove("temp");
        }
    }

    string FakeTeamCityFetchAndCheckout(string upstreamRepository, string monitoredReference)
    {
        var repoPath = InitNewRepository();

        using (var repo = new Repository(repoPath))
        {
            var remote = repo.Network.Remotes.Add("origin", upstreamRepository);
            Debug.Assert(remote.FetchRefSpecs.Single().Specification == "+refs/heads/*:refs/remotes/origin/*");
            repo.Network.Fetch(remote);

            if (monitoredReference.StartsWith("refs/pull/"))
            {
                repo.Network.Fetch(remote, new[] { string.Format("+{0}:{0}", monitoredReference) });
            }

            var src = monitoredReference;
            var dst = monitoredReference.Replace("refs/heads/", "refs/remotes/origin/");

            var fetched = (DirectReference)repo.Refs[dst];
            if (fetched.IsRemoteTrackingBranch())
            {
                Assert.IsNull(repo.Refs[src]);
                repo.Refs.Add(src, fetched.Target.Id);

                var branch = repo.Branches[src];

                repo.Branches.Update(branch,
                    b => b.Remote = remote.Name,
                    b => b.UpstreamBranch = src);
            }

            repo.Checkout(src);

            if (monitoredReference.StartsWith("refs/pull/"))
            {
                repo.Refs.Remove(monitoredReference);
            }
        }

        return repoPath;
    }
}