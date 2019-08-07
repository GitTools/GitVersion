using System.Diagnostics;
using System.IO;
using GitVersion;
using GitVersionCore.Tests;
using NUnit.Framework;

[TestFixture]
public class DynamicRepositoryTests : TestBase
{
    string workDirectory;


    [OneTimeSetUp]
    public void CreateTemporaryRepository()
    {
        // Note: we can't use guid because paths will be too long
        workDirectory = Path.Combine(Path.GetTempPath(), "GV");

        // Clean directory upfront, some build agents are having troubles
        if (Directory.Exists(workDirectory))
        {
            Directory.Delete(workDirectory, true);
        }

        Directory.CreateDirectory(workDirectory);
    }


    [OneTimeTearDown]
    public void Cleanup()
    {

    }

    //[Ignore("These tests are slow and fail on the second run in Test Explorer and need to be re-written")]
    [NonParallelizable]
    [TestCase("GV_master_1", "https://github.com/GitTools/GitVersion", "master", "4783d325521463cd6cf1b61074352da84451f25d", "4.0.0+1086")]
    [TestCase("GV_master_2", "https://github.com/GitTools/GitVersion", "master", "3bdcd899530b4e9b37d13639f317da04a749e728", "4.0.0+1092")]
    // Note: use same name twice to see if changing commits works on same (cached) repository
    [TestCase("Catel_develop_1", "https://github.com/Catel/Catel", "develop", "0e2b6c125a730d2fa5e24394ef64abe62c98e9e9", "5.12.0-alpha.188")]
    [TestCase("Catel_develop_1", "https://github.com/Catel/Catel", "develop", "71e71359f37581784e18c94e7a45eee72cbeeb30", "5.12.0-alpha.192")]
    [TestCase("Catel_master_1", "https://github.com/Catel/Catel", "master", "f5de8964c35180a5f8607f5954007d5828aa849f", "5.10.0")]
    public void FindsVersionInDynamicRepo(string name, string url, string targetBranch, string commitId, string expectedFullSemVer)
    {
        var root = Path.Combine(workDirectory, name);
        var dynamicDirectory = Path.Combine(root, "D"); // dynamic, keeping directory as short as possible
        var workingDirectory = Path.Combine(root, "W"); // working, keeping directory as short as possible

        //// Clear upfront
        //if (Directory.Exists(root))
        //{
        //    Directory.Delete(root, true);
        //}

        Directory.CreateDirectory(dynamicDirectory);
        Directory.CreateDirectory(workingDirectory);

        Logger.AddLoggersTemporarily(
            x => Debug.WriteLine($"[DEBUG]   {x}"),
            x => Debug.WriteLine($"[INFO]    {x}"),
            x => Debug.WriteLine($"[WARNING] {x}"),
            x => Debug.WriteLine($"[ERROR]   {x}"));

        var executeCore = new ExecuteCore(new TestFileSystem());

        var versionVariables = executeCore.ExecuteGitVersion(url, dynamicDirectory, null, targetBranch,
            false, workingDirectory, commitId);

        Assert.AreEqual(expectedFullSemVer, versionVariables.FullSemVer);
    }
}
