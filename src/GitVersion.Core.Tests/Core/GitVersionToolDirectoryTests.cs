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
        try
        {
            var options = Options.Create(new GitVersionOptions { WorkingDirectory = workDirectory, Settings = { NoFetch = true } });

            var sp = ConfigureServices(services => services.AddSingleton(options));

            var gitVersionCalculator = sp.GetRequiredService<IGitVersionCalculateTool>();

            gitVersionCalculator.CalculateVersionVariables();
        }
        catch (Exception ex)
        {
            // `RepositoryNotFoundException` means that it couldn't find the .git directory,
            // any other exception means that the .git was found but there was some other issue that this test doesn't care about.
            Assert.IsNotAssignableFrom<RepositoryNotFoundException>(ex);
        }
    }

    [Test]
    public void FindsGitDirectoryInParent()
    {
        var childDir = PathHelper.Combine(this.workDirectory, "child");
        Directory.CreateDirectory(childDir);

        try
        {
            var options = Options.Create(new GitVersionOptions { WorkingDirectory = childDir, Settings = { NoFetch = true } });

            var sp = ConfigureServices(services => services.AddSingleton(options));

            var gitVersionCalculator = sp.GetRequiredService<IGitVersionCalculateTool>();

            gitVersionCalculator.CalculateVersionVariables();
        }
        catch (Exception ex)
        {
            // TODO I think this test is wrong.. It throws a different exception
            // `RepositoryNotFoundException` means that it couldn't find the .git directory,
            // any other exception means that the .git was found but there was some other issue that this test doesn't care about.
            Assert.IsNotAssignableFrom<RepositoryNotFoundException>(ex);
        }
    }
}
