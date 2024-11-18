using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion.Core.Tests;

[TestFixture]
public class DynamicRepositoryTests : TestBase
{
    private string? workDirectory;

    private static void ClearReadOnly(DirectoryInfo parentDirectory)
    {
        parentDirectory.Attributes = FileAttributes.Normal;
        foreach (var fi in parentDirectory.GetFiles())
        {
            fi.Attributes = FileAttributes.Normal;
        }
        foreach (var di in parentDirectory.GetDirectories())
        {
            ClearReadOnly(di);
        }
    }

    [SetUp]
    public void CreateTemporaryRepository()
    {
        // Note: we can't use guid because paths will be too long
        this.workDirectory = PathHelper.Combine(Path.GetTempPath(), "GV");

        // Clean directory upfront, some build agents are having troubles
        if (Directory.Exists(this.workDirectory))
        {
            var di = new DirectoryInfo(this.workDirectory);
            ClearReadOnly(di);

            Directory.Delete(this.workDirectory, true);
        }

        Directory.CreateDirectory(this.workDirectory);
    }

    [TearDown]
    public void Cleanup()
    {
    }

    // Note: use same name twice to see if changing commits works on same (cached) repository
    [NonParallelizable]
    [TestCase("GV_main", "https://github.com/GitTools/GitVersion", MainBranch, "2dc142a4a4df77db61a00d9fb7510b18b3c2c85a", "5.8.2-47")]
    [TestCase("GV_main", "https://github.com/GitTools/GitVersion", MainBranch, "efddf2f92c539a9c27f1904d952dcab8fb955f0e", "5.8.2-56")]
    public void FindsVersionInDynamicRepo(string name, string url, string targetBranch, string commitId, string expectedFullSemVer)
    {
        var root = PathHelper.Combine(this.workDirectory, name);
        var dynamicDirectory = PathHelper.Combine(root, "D"); // dynamic, keeping directory as short as possible
        var workingDirectory = PathHelper.Combine(root, "W"); // working, keeping directory as short as possible
        var gitVersionOptions = new GitVersionOptions
        {
            RepositoryInfo =
            {
                TargetUrl = url,
                ClonePath = dynamicDirectory,
                TargetBranch = targetBranch,
                CommitId = commitId
            },
            Settings = { NoFetch = false },
            WorkingDirectory = workingDirectory
        };
        var options = Options.Create(gitVersionOptions);

        Directory.CreateDirectory(dynamicDirectory);
        Directory.CreateDirectory(workingDirectory);

        var sp = ConfigureServices(services => services.AddSingleton(options));

        sp.DiscoverRepository();

        var gitPreparer = sp.GetRequiredService<IGitPreparer>();
        gitPreparer.Prepare();

        var gitVersionCalculator = sp.GetRequiredService<IGitVersionCalculateTool>();

        var versionVariables = gitVersionCalculator.CalculateVersionVariables();

        Assert.That(versionVariables.FullSemVer, Is.EqualTo(expectedFullSemVer));
    }
}
