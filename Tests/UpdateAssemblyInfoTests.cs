using System;
using System.Collections;
using System.IO;
using System.Linq;
using GitFlowVersionTask;
using LibGit2Sharp;
using Microsoft.Build.Framework;
using NUnit.Framework;
using Tests.Helpers;

[TestFixture]
public class UpdateAssemblyInfoTests : Lg2sHelperBase
{
    [Test]
    public void StandardExecutionMode_LackOfAValidGitDirectoryDoesNotPreventExecution()
    {
        var task = BuildTask(Path.GetTempPath());

        Assert.True(task.Execute());
    }

    [Test]
    public void TeamCityExecutionMode_RequiresAValidGitDirectoryToOperate()
    {
        var task = BuildTask(Path.GetTempPath());

        using (new FakeTeamCityContext())
        {
            Assert.False(task.Execute());
        }
    }

    [Test]
    public void StandardExecutionMode_DoesNotRequireARemoteToOperate()
    {
        using (var repo = new Repository(ASBMTestRepoWorkingDirPath))
        {
            Assert.AreEqual(0, repo.Network.Remotes.Count());
        }

        var task = BuildTask(ASBMTestRepoWorkingDirPath);

        Assert.True(task.Execute());
    }

    [Test]
    public void TeamCityExecutionMode_RequiresARemoteToOperate()
    {
        using (var repo = new Repository(ASBMTestRepoWorkingDirPath))
        {
            Assert.AreEqual(0, repo.Network.Remotes.Count());
        }

        var task = BuildTask(ASBMTestRepoWorkingDirPath);

        using (new FakeTeamCityContext())
        {
            Assert.False(task.Execute());
        }
    }

    [Test]
    public void TeamCityExecutionMode_CanDetermineTheVersionFromAFetchedMaster()
    {
        AssertVersionFromFetchedRemote("refs/heads/master");
    }

    [Test]
    public void StandardExecutionMode_CanDetermineTheVersionFromALocalMaster()
    {
        AssertVersionFromLocal("refs/heads/master");
    }

    [Test]
    public void TeamCityExecutionMode_CanDetermineTheVersionFromAFetchedDevelop()
    {
        AssertVersionFromFetchedRemote("refs/heads/develop");
    }

    [Test]
    public void StandardExecutionMode_CanDetermineTheVersionFromALocalDevelop()
    {
        AssertVersionFromLocal("refs/heads/develop");
    }

    [Test]
    public void TeamCityExecutionMode_CanDetermineTheVersionFromAFetchedFeature()
    {
        AssertVersionFromFetchedRemote("refs/heads/feature/one");
    }

    [Test]
    public void StandardExecutionMode_CanDetermineTheVersionFromALocalFeature()
    {
        AssertVersionFromLocal("refs/heads/feature/one");
    }

    [Test]
    public void StandardExecutionMode_CannotDetermineTheVersionFromADetachedHead()
    {
        string repoPath = Clone(ASBMTestRepoWorkingDirPath);

        using (var repo = new Repository(repoPath))
        {
            repo.Checkout(repo.Head.Tip);
            Assert.IsTrue(repo.Info.IsHeadDetached);
        }

        var task = BuildTask(repoPath);

        Assert.False(task.Execute());
    }

    [Test]
    public void TeamCityExecutionMode_CanDetermineTheVersionFromAPullRequest()
    {
        string repoPath = Clone(ASBMTestRepoWorkingDirPath);
        CreateFakePullRequest(repoPath, "1735");

        AssertVersionFromFetchedRemote(repoPath, "refs/pull/1735/merge");
    }

    private static void CreateFakePullRequest(string repoPath, string issueNumber)
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

            Signature sign = Constants.SignatureNow();
            var m = repo.ObjectDatabase.CreateCommit(
                string.Format("Merge pull request #{0} from nulltoken/ntk/fix/{0}", issueNumber)
                , sign, sign, c.Tree, new[] {repo.Branches["develop"].Tip, c});

            repo.Refs.Add(string.Format("refs/pull/{0}/merge", issueNumber), m.Id);

            repo.Checkout("develop");
            repo.Branches.Remove("temp");
        }
    }

    private void AssertVersionFromFetchedRemote(string repositoryPath, string monitoredReference)
    {
        string wd = FakeTeamCityFetchAndCheckout(repositoryPath, monitoredReference);

        var task = BuildTask(wd);

        using (new FakeTeamCityContext())
        {
            Assert.True(task.Execute());
        }
    }

    private void AssertVersionFromFetchedRemote(string monitoredReference)
    {
        AssertVersionFromFetchedRemote(ASBMTestRepoWorkingDirPath, monitoredReference);
    }

    private void AssertVersionFromLocal(string repositoryPath, string monitoredReference)
    {
        var repoPath = CheckoutLocal(repositoryPath, monitoredReference);

        var task = BuildTask(repoPath);

        Assert.True(task.Execute());
    }

    private void AssertVersionFromLocal(string monitoredReference)
    {
        AssertVersionFromLocal(ASBMTestRepoWorkingDirPath, monitoredReference);
    }

    private string CheckoutLocal(string repositoryPath, string monitoredReference)
    {
        string repoPath = Clone(repositoryPath);

        using (var repo = new Repository(repoPath))
        {
            repo.Checkout(repo.Branches[monitoredReference]);
        }
        return repoPath;
    }

    private string FakeTeamCityFetchAndCheckout(string upstreamRepository, string monitoredReference)
    {
        string repoPath = InitNewRepository();

        using (var repo = new Repository(repoPath))
        {
            Remote remote;

            if (monitoredReference.StartsWith("refs/pull/"))
            {
                remote = repo.Network.Remotes.Add("origin", upstreamRepository,
                    string.Format("+{0}:{0}", monitoredReference));

                repo.Network.Fetch(remote);

                repo.Config.Unset("remote.origin.url");
                repo.Config.Unset("remote.origin.fetch");
            }

            remote = repo.Network.Remotes.Add("origin", upstreamRepository, "+refs/heads/*:refs/remotes/origin/*");

            repo.Network.Fetch(remote);

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

    private static UpdateAssemblyInfo BuildTask(string workingDirectory)
    {
        var task = new UpdateAssemblyInfo(true)
        {
            BuildEngine = new MockBuildEngine(),
            SolutionDirectory = workingDirectory,
            CompileFiles = new ITaskItem[] { },
        };
        return task;
    }

    private class FakeTeamCityContext : IDisposable
    {
        const string VariableName = "GitFlowVersion.Fake.TEAMCITY_VERSION";

        public FakeTeamCityContext()
        {
            Assert.False(IsEnvironmentVariableSet());
            Environment.SetEnvironmentVariable(VariableName, "FAKE");
        }

        public void Dispose()
        {
            Assert.True(IsEnvironmentVariableSet());
            Environment.SetEnvironmentVariable(VariableName, "");
        }

        private static bool IsEnvironmentVariableSet()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(VariableName));
        }
    }

    private class MockBuildEngine : IBuildEngine
    {
        public void LogErrorEvent(BuildErrorEventArgs e)
        { }

        public void LogWarningEvent(BuildWarningEventArgs e)
        { }

        public void LogMessageEvent(BuildMessageEventArgs e)
        { }

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties,
            IDictionary targetOutputs)
        {
            throw new System.NotImplementedException();
        }

        public bool ContinueOnError
        {
            get { throw new System.NotImplementedException(); }
        }

        public int LineNumberOfTaskNode
        {
            get { throw new System.NotImplementedException(); }
        }

        public int ColumnNumberOfTaskNode
        {
            get { throw new System.NotImplementedException(); }
        }

        public string ProjectFileOfTaskNode
        {
            get { throw new System.NotImplementedException(); }
        }
    }
}

