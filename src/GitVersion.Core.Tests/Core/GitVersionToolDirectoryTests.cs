using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion.Core.Tests;

[TestFixture]
public class GitVersionTaskDirectoryTests : TestBase
{
    private string gitDirectory;
    private string workDirectory;

    [SetUp]
    public void SetUp()
    {
        this.workDirectory = PathHelper.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        this.gitDirectory = Repository.Init(this.workDirectory).TrimEnd(Path.DirectorySeparatorChar);
        Assert.That(this.gitDirectory, Is.Not.Null);
    }

    [TearDown]
    public void Cleanup() => Directory.Delete(this.workDirectory, true);

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
        var childDir = PathHelper.Combine(this.workDirectory, "child");
        Directory.CreateDirectory(childDir);

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
