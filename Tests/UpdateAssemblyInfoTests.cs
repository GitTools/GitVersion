using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitFlowVersion;
using GitFlowVersionTask;
using LibGit2Sharp;
using NUnit.Framework;
using Tests.Helpers;

[TestFixture]
public class UpdateAssemblyInfoTests : Lg2sHelperBase
{
    [Test]
    public void StandardExecutionMode_LackOfAValidGitDirectoryDoesNotPreventExecution()
    {
        var task = new LocalUpdateAssemblyInfo
            {            
                BuildEngine = new MockBuildEngine(),
                SolutionDirectory = Path.GetTempPath(),
            };

        Assert.True(task.InnerExecute());
    }

    [Test]
    public void StandardExecutionMode_DoesNotRequireARemoteToOperate()
    {
        using (var repo = new Repository(ASBMTestRepoWorkingDirPath))
        {
            Assert.AreEqual(0, repo.Network.Remotes.Count());
        }

        var task = new LocalUpdateAssemblyInfo
        {
            BuildEngine = new MockBuildEngine(),
                SolutionDirectory = ASBMTestRepoWorkingDirPath,
            };

        Assert.True(task.InnerExecute());
    }

    [Test]
    public void StandardExecutionMode_CanDetermineTheVersionFromALocalMaster()
    {
        var repoPath = CheckoutLocal(ASBMTestRepoWorkingDirPath, "refs/heads/master");

        var task = new LocalUpdateAssemblyInfo
        {
            BuildEngine = new MockBuildEngine(),
                SolutionDirectory = repoPath,
            };

        Assert.True(task.InnerExecute());
    }

    [Test]
    public void StandardExecutionMode_CanDetermineTheVersionFromALocalDevelop()
    {
        var repoPath = CheckoutLocal(ASBMTestRepoWorkingDirPath, "refs/heads/develop");

        var task = new LocalUpdateAssemblyInfo
        {
            BuildEngine = new MockBuildEngine(),
                SolutionDirectory = repoPath,
            };

        Assert.True(task.InnerExecute());
    }


    [Test]
    public void StandardExecutionMode_CanDetermineTheVersionFromALocalFeature()
    {
        var repoPath = CheckoutLocal(ASBMTestRepoWorkingDirPath, "refs/heads/feature/one");

        var task = new LocalUpdateAssemblyInfo
        {
            BuildEngine = new MockBuildEngine(),
                SolutionDirectory = repoPath,
            };

        Assert.True(task.InnerExecute());
    }

    [Test]
    public void StandardExecutionMode_CannotDetermineTheVersionFromADetachedHead()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);

        using (var repo = new Repository(repoPath))
        {
            repo.Checkout(repo.Head.Tip);
            Assert.IsTrue(repo.Info.IsHeadDetached);
        }

        var task = new LocalUpdateAssemblyInfo
        {
            BuildEngine = new MockBuildEngine(),
                SolutionDirectory = repoPath,
            };

        var exception = Assert.Throws<ErrorException>(() => task.InnerExecute());
        Assert.AreEqual("It looks like the branch being examined is a detached Head pointing to commit '469f851'. Without a proper branch name GitFlowVersion cannot determine the build version.",exception.Message);
    }

    string CheckoutLocal(string repositoryPath, string monitoredReference)
    {
        var repoPath = Clone(repositoryPath);

        using (var repo = new Repository(repoPath))
        {
            repo.Branches[monitoredReference].ForceCheckout();
        }
        return repoPath;
    }


    public class LocalUpdateAssemblyInfo : UpdateAssemblyInfo
    {
        public override IEnumerable<IBuildServer> GetApplicableBuildServers(string gitDirectory)
        {
            yield break;
        }
    }
}