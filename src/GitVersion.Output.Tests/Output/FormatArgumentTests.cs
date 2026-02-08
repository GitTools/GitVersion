using GitVersion.Core.Tests;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Output.OutputGenerator;
using GitVersion.Testing.Extensions;
using LibGit2Sharp;

namespace GitVersion.Output.Tests;

[TestFixture]
public class FormatArgumentTests : TestBase
{
    [TestCase("{SemVer}", "1.1.0-foo.1")]
    [TestCase("{Major}.{Minor}", "1.1")]
    [TestCase("{Major}.{Minor}.{Patch}", "1.1.0")]
    [TestCase("{Major}.{Minor}.{Patch}.{PreReleaseTag}", "1.1.0.foo.1")]
    public void ShouldOutputFormatTests(string format, string expectedValue)
    {
        using var fixture = CreateTestRepository();

        var consoleBuilder = new StringBuilder();
        IConsole consoleAdapter = new TestConsoleAdapter(consoleBuilder);

        var sp = fixture.ConfigureServices((services, testFixture) =>
        {
            var options = Options.Create(new GitVersionOptions { WorkingDirectory = testFixture.RepositoryPath, RepositoryInfo = { TargetBranch = testFixture.Repository.Head.CanonicalName }, Format = format, Output = { OutputType.Json } });
            var repository = testFixture.Repository.ToGitRepository();

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
        using var fixture = CreateTestRepository();
        var consoleBuilder = new StringBuilder();
        IConsole console = new TestConsoleAdapter(consoleBuilder);
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable("CustomVar", "foo");

        var sp = fixture.ConfigureServices((services, testFixture) =>
        {
            var options = Options.Create(new GitVersionOptions { WorkingDirectory = testFixture.RepositoryPath, RepositoryInfo = { TargetBranch = testFixture.Repository.Head.CanonicalName }, Format = format, Output = { OutputType.Json } });
            var repository = testFixture.Repository.ToGitRepository();

            services.AddSingleton(options);
            services.AddSingleton(repository);
            services.AddSingleton(console);
            services.AddSingleton<IEnvironment>(environment);
        });

        var versionVariables = sp.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();
        var outputGenerator = sp.GetRequiredService<IOutputGenerator>();

        outputGenerator.Execute(versionVariables, new());
        var output = consoleBuilder.ToString().Replace("\n", "").Replace("\r", "");
        output.ShouldBeEquivalentTo(expectedValue);
    }

    [TestCase("Major", "'1'")]
    [TestCase("MajorMinorPatch", "'1.1.0'")]
    [TestCase("SemVer", "'1.1.0-foo.1'")]
    [TestCase("PreReleaseTagWithDash", "'-foo.1'")]
    [TestCase("AssemblySemFileVer", "'1.1.0.0'")]
    [TestCase("BranchName", "'feature/foo'")]
    [TestCase("FullSemVer", "'1.1.0-foo.1+1'")]
    public void ShouldOutputDotEnvEntries(string variableName, string expectedValue)
    {
        using var fixture = CreateTestRepository();

        var consoleBuilder = new StringBuilder();
        IConsole consoleAdapter = new TestConsoleAdapter(consoleBuilder);

        var sp = fixture.ConfigureServices((services, testFixture) =>
        {
            var options = Options.Create(new GitVersionOptions { WorkingDirectory = testFixture.RepositoryPath, RepositoryInfo = { TargetBranch = testFixture.Repository.Head.CanonicalName }, Output = { OutputType.DotEnv } });
            var repository = testFixture.Repository.ToGitRepository();

            services.AddSingleton(options);
            services.AddSingleton(repository);
            services.AddSingleton(consoleAdapter);
        });

        var versionVariables = sp.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();
        var outputGenerator = sp.GetRequiredService<IOutputGenerator>();

        outputGenerator.Execute(versionVariables, new());
        var output = consoleBuilder.ToString();
        output.ShouldContain($"GitVersion_{variableName}={expectedValue}{SysEnv.NewLine}");
    }

    [TestCase]
    public void ShouldOutputAllCalculatedVariablesAsDotEnvEntries()
    {
        using var fixture = CreateTestRepository();

        var consoleBuilder = new StringBuilder();
        IConsole consoleAdapter = new TestConsoleAdapter(consoleBuilder);

        var sp = fixture.ConfigureServices((services, testFixture) =>
        {
            var options = Options.Create(new GitVersionOptions { WorkingDirectory = testFixture.RepositoryPath, RepositoryInfo = { TargetBranch = testFixture.Repository.Head.CanonicalName }, Output = { OutputType.DotEnv } });
            var repository = testFixture.Repository.ToGitRepository();

            services.AddSingleton(options);
            services.AddSingleton(repository);
            services.AddSingleton(consoleAdapter);
        });

        var versionVariables = sp.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();
        var outputGenerator = sp.GetRequiredService<IOutputGenerator>();

        outputGenerator.Execute(versionVariables, new());
        var output = consoleBuilder.ToString();
        var totalOutputLines = output.Split(SysEnv.NewLine).Length - 1; // ignore last item that also ends with the newline string
        Assert.That(totalOutputLines, Is.EqualTo(versionVariables.Count()));
    }

    [TestCase("Major", "'0'")]
    [TestCase("MajorMinorPatch", "'0.0.1'")]
    [TestCase("SemVer", "'0.0.1-1'")]
    [TestCase("BuildMetaData", "''")]
    [TestCase("AssemblySemVer", "'0.0.1.0'")]
    [TestCase("PreReleaseTagWithDash", "'-1'")]
    [TestCase("BranchName", "'main'")]
    [TestCase("PreReleaseLabel", "''")]
    [TestCase("PreReleaseLabelWithDash", "''")]
    public void ShouldOutputAllDotEnvEntriesEvenForMinimalRepositories(string variableName, string expectedValue)
    {
        using var fixture = CreateMinimalTestRepository();

        var consoleBuilder = new StringBuilder();
        IConsole consoleAdapter = new TestConsoleAdapter(consoleBuilder);

        var sp = fixture.ConfigureServices((services, testFixture) =>
        {
            var options = Options.Create(new GitVersionOptions { WorkingDirectory = testFixture.RepositoryPath, RepositoryInfo = { TargetBranch = testFixture.Repository.Head.CanonicalName }, Output = { OutputType.DotEnv } });
            var repository = testFixture.Repository.ToGitRepository();

            services.AddSingleton(options);
            services.AddSingleton(repository);
            services.AddSingleton(consoleAdapter);
        });

        var versionVariables = sp.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();
        var outputGenerator = sp.GetRequiredService<IOutputGenerator>();

        outputGenerator.Execute(versionVariables, new());
        var output = consoleBuilder.ToString();
        output.ShouldContain($"GitVersion_{variableName}={expectedValue}{SysEnv.NewLine}");
    }

    [TestCase]
    public void ShouldOutputAllCalculatedVariablesAsDotEnvEntriesEvenForMinimalRepositories()
    {
        using var fixture = CreateMinimalTestRepository();

        var consoleBuilder = new StringBuilder();
        IConsole consoleAdapter = new TestConsoleAdapter(consoleBuilder);

        var sp = fixture.ConfigureServices((services, testFixture) =>
        {
            var options = Options.Create(new GitVersionOptions { WorkingDirectory = testFixture.RepositoryPath, RepositoryInfo = { TargetBranch = testFixture.Repository.Head.CanonicalName }, Output = { OutputType.DotEnv } });
            var repository = testFixture.Repository.ToGitRepository();

            services.AddSingleton(options);
            services.AddSingleton(repository);
            services.AddSingleton(consoleAdapter);
        });

        var versionVariables = sp.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();
        var outputGenerator = sp.GetRequiredService<IOutputGenerator>();

        outputGenerator.Execute(versionVariables, new());
        var output = consoleBuilder.ToString();
        var totalOutputLines = output.Split(SysEnv.NewLine).Length - 1; // ignore last item that also ends with the newline string
        Assert.That(totalOutputLines, Is.EqualTo(versionVariables.Count()));
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

    private static EmptyRepositoryFixture CreateMinimalTestRepository()
    {
        var fixture = new EmptyRepositoryFixture();
        _ = fixture.Repository.MakeACommit();
        return fixture;
    }
}
