using System.IO;
using GitVersion;
using NUnit.Framework;

[TestFixture]
public class DynamicRepositoryTests
{
    string workDirectory;


    [SetUp]
    public void CreateTemporaryRepository()
    {
        // Note: we can't use guid because paths will be too long
        workDirectory = Path.Combine(Path.GetTempPath(), "DynRepoTests");
    }


    //[TearDown]
    //public void Cleanup()
    //{
    //    Directory.Delete(workDirectory, true);
    //}

    [Ignore("These tests are slow and fail on the second run in Test Explorer and need to be re-written")]
    [TestCase("GV_master_1", "https://github.com/GitTools/GitVersion", "master", "4783d325521463cd6cf1b61074352da84451f25d", "4.0.0+1126")]
    [TestCase("GV_master_2", "https://github.com/GitTools/GitVersion", "master", "3bdcd899530b4e9b37d13639f317da04a749e728", "4.0.0+1132")]
    public void FindsVersionInDynamicRepo(string name, string url, string targetBranch, string commitId, string expectedFullSemVer)
    {
        var root = Path.Combine(workDirectory, name);
        var dynamicDirectory = Path.Combine(root, "dynamic");
        var workingDirectory = Path.Combine(root, "working");

        // Clear upfront
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }

        Directory.CreateDirectory(dynamicDirectory);
        Directory.CreateDirectory(workingDirectory);

        var executeCore = new ExecuteCore(new TestFileSystem());

        var versionVariables = executeCore.ExecuteGitVersion(url, dynamicDirectory, null, targetBranch,
            false, workingDirectory, commitId);

        Assert.AreEqual(expectedFullSemVer, versionVariables.FullSemVer);
    }
}