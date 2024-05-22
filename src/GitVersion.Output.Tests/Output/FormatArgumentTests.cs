using GitVersion.Core.Tests.Helpers;
using GitVersion.Logging;
using GitVersion.Output.OutputGenerator;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion.Core.Tests;

[TestFixture]
public class FormatArgumentTests : TestBase
{
    [TestCase("{SemVer}", "1.1.0-foo.1")]
    [TestCase("{Major}.{Minor}", "1.1")]
    [TestCase("{Major}.{Minor}.{Patch}", "1.1.0")]
    [TestCase("{Major}.{Minor}.{Patch}.{PreReleaseTag}", "1.1.0.foo.1")]
    public void ShouldOutputFormatTests(string format, string expectedValue)
    {
        var fixture = CreateTestRepository();

        var consoleBuilder = new StringBuilder();
        IConsole consoleAdapter = new TestConsoleAdapter(consoleBuilder);

        var sp = ConfigureServices(services =>
        {
            var options = Options.Create(new GitVersionOptions { WorkingDirectory = fixture.RepositoryPath, RepositoryInfo = { TargetBranch = fixture.Repository.Head.CanonicalName }, Format = format, Output = { OutputType.Json } });
            var repository = fixture.Repository.ToGitRepository();

            services.AddSingleton(options);
            services.AddSingleton(repository);
            services.AddSingleton(consoleAdapter);
        });

        var versionVariables = sp.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();
        var outputGenerator = sp.GetRequiredService<IOutputGenerator>();

        outputGenerator.Execute(versionVariables, new());
        var output = consoleBuilder.ToString().Replace("\n", "").Replace("\r", "");
        output.ShouldBeEquivalentTo(expectedValue);
    }

    [TestCase("{Major}.{Minor}.{env:CustomVar}", "1.1.foo")]
    [TestCase("{Major}.{Minor}.{Patch}.{env:CustomVar}", "1.1.0.foo")]
    public void ShouldOutputFormatWithEnvironmentVariablesTests(string format, string expectedValue)
    {
        var fixture = CreateTestRepository();
        var consoleBuilder = new StringBuilder();
        IConsole console = new TestConsoleAdapter(consoleBuilder);
        IEnvironment environment = new TestEnvironment();
        environment.SetEnvironmentVariable("CustomVar", "foo");

        var sp = ConfigureServices(services =>
        {
            var options = Options.Create(new GitVersionOptions { WorkingDirectory = fixture.RepositoryPath, RepositoryInfo = { TargetBranch = fixture.Repository.Head.CanonicalName }, Format = format, Output = { OutputType.Json } });
            var repository = fixture.Repository.ToGitRepository();

            services.AddSingleton(options);
            services.AddSingleton(repository);
            services.AddSingleton(console);
            services.AddSingleton(environment);
        });

        var versionVariables = sp.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();
        var outputGenerator = sp.GetRequiredService<IOutputGenerator>();

        outputGenerator.Execute(versionVariables, new());
        var output = consoleBuilder.ToString().Replace("\n", "").Replace("\r", "");
        output.ShouldBeEquivalentTo(expectedValue);
    }

    private static EmptyRepositoryFixture CreateTestRepository()
    {
        var fixture = new EmptyRepositoryFixture();
        _ = fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
        var secondCommit = fixture.Repository.MakeACommit();
        _ = fixture.Repository.Tags.Add("1.0.0", secondCommit);
        var featureBranch = fixture.Repository.CreateBranch("feature/foo");
        Commands.Checkout(fixture.Repository, featureBranch);
        _ = fixture.Repository.MakeACommit();
        return fixture;
    }
}
