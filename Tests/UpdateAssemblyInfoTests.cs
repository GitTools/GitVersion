using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitFlowVersion;
using GitFlowVersionTask;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class UpdateAssemblyInfoTests : Lg2sHelperBase
{
    [Test]
    public void StandardExecutionMode_LackOfAValidGitDirectoryDoesNotPreventExecution()
    {
        var task = new UpdateAssemblyInfo
        {
            BuildEngine = new MockBuildEngine(),
            SolutionDirectory = Path.GetTempPath(),
        };

        task.InnerExecute();
    }

    [Test]
    public void StandardExecutionMode_DoesNotRequireARemoteToOperate()
    {
        using (var repo = new Repository(ASBMTestRepoWorkingDirPath))
        {
            Assert.AreEqual(0, repo.Network.Remotes.Count());
        }

        var task = new UpdateAssemblyInfo
        {
            BuildEngine = new MockBuildEngine(),
            SolutionDirectory = ASBMTestRepoWorkingDirPath,
        };

        task.InnerExecute();
    }

    [Test]
    public void StandardExecutionMode_CanDetermineTheVersionFromALocalMaster()
    {
        var repoPath = CheckoutLocal(ASBMTestRepoWorkingDirPath, "refs/heads/master");

        var task = new UpdateAssemblyInfo
        {
            BuildEngine = new MockBuildEngine(),
            SolutionDirectory = repoPath,
        };

        task.InnerExecute();
    }

    [Test]
    public void StandardExecutionMode_CanDetermineTheVersionFromALocalDevelop()
    {
        var repoPath = CheckoutLocal(ASBMTestRepoWorkingDirPath, "refs/heads/develop");

        var task = new UpdateAssemblyInfo
        {
            BuildEngine = new MockBuildEngine(),
            SolutionDirectory = repoPath,
        };

        task.InnerExecute();
    }


    [Test]
    public void StandardExecutionMode_CanDetermineTheVersionFromALocalFeature()
    {
        var repoPath = CheckoutLocal(ASBMTestRepoWorkingDirPath, "refs/heads/feature/one");

        var task = new UpdateAssemblyInfo
        {
            BuildEngine = new MockBuildEngine(),
            SolutionDirectory = repoPath,
        };

        task.InnerExecute();
    }

    [Test]
    public void StandardExecutionMode_CannotDetermineTheVersionFromADetachedHead()
    {
        var repoPath = Clone(ASBMTestRepoWorkingDirPath);

        using (var repo = new Repository(repoPath))
        {
            repo.Checkout("469f851");
            Assert.IsTrue(repo.Info.IsHeadDetached);
        }

        var task = new UpdateAssemblyInfo
        {
            BuildEngine = new MockBuildEngine(),
            SolutionDirectory = repoPath,
        };

        var exception = Assert.Throws<ErrorException>(task.InnerExecute);
        Assert.AreEqual("It looks like the branch being examined is a detached Head pointing to commit '469f851'. Without a proper branch name GitFlowVersion cannot determine the build version.", exception.Message);
    }

    [SetUp]
    public void SetUp()
    {
        //avoid buildserver detection to make the tests pass on the buildserver
        BuildServerList.Selector = () => new List<IBuildServer>();
    }


    [TearDown]
    public void TearDown()
    {
        BuildServerList.ResetSelector();
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

}