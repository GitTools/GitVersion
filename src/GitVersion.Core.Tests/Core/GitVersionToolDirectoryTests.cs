using System.IO.Abstractions;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using LibGit2Sharp;

namespace GitVersion.Core.Tests;

[TestFixture]
public class GitVersionTaskDirectoryTests : TestBase
{
    private string gitDirectory;
    private string workDirectory;
    private IFileSystem fileSystem;

    [SetUp]
    public void SetUp()
    {
        var sp = ConfigureServices();
        this.fileSystem = sp.GetRequiredService<IFileSystem>();
        this.workDirectory = FileSystemHelper.Path.Combine(FileSystemHelper.Path.GetTempPath(), Guid.NewGuid().ToString());
        this.gitDirectory = Repository.Init(this.workDirectory).TrimEnd(FileSystemHelper.Path.DirectorySeparatorChar);
        Assert.That(this.gitDirectory, Is.Not.Null);
    }

    [TearDown]
    public void Cleanup() => this.fileSystem.Directory.Delete(this.workDirectory, true);

    [Test]
    public void FindsGitDirectory()
    {
        var exception = Assert.Catch(() =>
        {
            var options = Options.Create(new GitVersionOptions { WorkingDirectory = workDirectory, Settings = { NoFetch = true } });

            var sp = ConfigureServices(services => services.AddSingleton(options));

            var gitVersionCalculator = sp.GetRequiredService<IGitVersionCalculateTool>();

            gitVersionCalculator.CalculateVersionVariables();
        });
        exception.ShouldNotBeAssignableTo<RepositoryNotFoundException>();
    }

    [Test]
    public void FindsGitDirectoryInParent()
    {
        var childDir = FileSystemHelper.Path.Combine(this.workDirectory, "child");
        this.fileSystem.Directory.CreateDirectory(childDir);

        var exception = Assert.Catch(() =>
        {
            var options = Options.Create(new GitVersionOptions { WorkingDirectory = childDir, Settings = { NoFetch = true } });

            var sp = ConfigureServices(services => services.AddSingleton(options));

            var gitVersionCalculator = sp.GetRequiredService<IGitVersionCalculateTool>();

            gitVersionCalculator.CalculateVersionVariables();
        });
        exception.ShouldNotBeAssignableTo<RepositoryNotFoundException>();
    }
}
