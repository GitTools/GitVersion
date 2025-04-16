using System.IO.Abstractions;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion.Core.Tests;

[TestFixture]
public class DynamicRepositoryTests : TestBase
{
    private string? workDirectory;

    [SetUp]
    public void SetUp() => this.workDirectory = FileSystemHelper.Path.Combine(FileSystemHelper.Path.GetTempPathLegacy(), "GV");

    [TearDown]
    public void TearDown()
    {
    }

    // Note: use same name twice to see if changing commits works on same (cached) repository
    [NonParallelizable]
    [TestCase("GV_main", "https://github.com/GitTools/GitVersion", MainBranch, "2dc142a4a4df77db61a00d9fb7510b18b3c2c85a", "5.8.2-47")]
    [TestCase("GV_main", "https://github.com/GitTools/GitVersion", MainBranch, "efddf2f92c539a9c27f1904d952dcab8fb955f0e", "5.8.2-56")]
    public void FindsVersionInDynamicRepo(string name, string url, string targetBranch, string commitId, string expectedFullSemVer)
    {
        var root = FileSystemHelper.Path.Combine(this.workDirectory, name);
        var dynamicDirectory = FileSystemHelper.Path.Combine(root, "D"); // dynamic, keeping directory as short as possible
        var workingDirectory = FileSystemHelper.Path.Combine(root, "W"); // working, keeping directory as short as possible
        var gitVersionOptions = new GitVersionOptions
        {
            RepositoryInfo =
            {
                TargetUrl = url,
                ClonePath = dynamicDirectory,
                TargetBranch = targetBranch,
                CommitId = commitId
            },
            Settings = { NoFetch = false, NoCache = true },
            WorkingDirectory = workingDirectory
        };
        var options = Options.Create(gitVersionOptions);

        var sp = ConfigureServices(services => services.AddSingleton(options));

        sp.DiscoverRepository();

        var gitPreparer = sp.GetRequiredService<IGitPreparer>();
        gitPreparer.Prepare();

        var fileSystem = sp.GetRequiredService<IFileSystem>();
        fileSystem.Directory.CreateDirectory(dynamicDirectory);
        fileSystem.Directory.CreateDirectory(workingDirectory);

        var gitVersionCalculator = sp.GetRequiredService<IGitVersionCalculateTool>();

        var versionVariables = gitVersionCalculator.CalculateVersionVariables();

        Assert.That(versionVariables.FullSemVer, Is.EqualTo(expectedFullSemVer));
    }
}
