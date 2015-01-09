using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitVersion;
using GitVersionTask;
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

    [TestCase("Major")]
    [TestCase("MajorMinor")]
    [TestCase("MajorMinorPatch")]
    [TestCase("mAjOr")]
    [TestCase("mAjOrMiNor")]
    [TestCase("mAjOrMiNorpatch")]
    public void StandardExecutionMode_CanAcceptAssemblyVersioningSchemes(string assemblyVersioningScheme)
    {
        var repoPath = CheckoutLocal(ASBMTestRepoWorkingDirPath, "refs/heads/master");

        // TODO Tasks need a way to overrride configuration in tests
        var task = new UpdateAssemblyInfo
        {
            BuildEngine = new MockBuildEngine(),
            SolutionDirectory = repoPath,
            // TODO AssemblyVersioningScheme = assemblyVersioningScheme
        };

        task.InnerExecute();
    }

    [SetUp]
    public void SetUp()
    {
        //avoid buildserver detection to make the tests pass on the buildserver
        BuildServerList.Selector = arguments => new List<IBuildServer>();
    }

    [TearDown]
    public new void TearDown()
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