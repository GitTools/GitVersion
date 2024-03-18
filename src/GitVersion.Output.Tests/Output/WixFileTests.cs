using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.Output.WixUpdater;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Core.Tests;

[TestFixture]
[Parallelizable(ParallelScope.None)]
internal class WixFileTests : TestBase
{
    private string workingDir;

    [OneTimeSetUp]
    public void OneTimeSetUp() => workingDir = PathHelper.Combine(PathHelper.GetTempPath(), "WixFileTests");

    [OneTimeTearDown]
    public void OneTimeTearDown() => DirectoryHelper.DeleteDirectory(workingDir);

    [SetUp]
    public void Setup() => ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();

    [Test]
    public void UpdateWixVersionFile()
    {
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            BuildMetaData = new("5.Branch.develop")
            {
                VersionSourceSha = "versionSourceSha",
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                CommitDate = DateTimeOffset.Parse("2019-02-20 23:59:59Z")
            }
        };

        var stringBuilder = new StringBuilder();
        void Action(string s) => stringBuilder.AppendLine(s);

        var logAppender = new TestLogAppender(Action);
        var log = new Log(logAppender);

        var sp = ConfigureServices(service => service.AddSingleton<ILog>(log));

        var fileSystem = sp.GetRequiredService<IFileSystem>();
        var variableProvider = sp.GetRequiredService<IVariableProvider>();
        var versionVariables = variableProvider.GetVariablesFor(semVer, EmptyConfigurationBuilder.New.Build(), 0);

        using var wixVersionFileUpdater = sp.GetRequiredService<IWixVersionFileUpdater>();

        wixVersionFileUpdater.Execute(versionVariables, new(workingDir));

        var file = PathHelper.Combine(workingDir, WixVersionFileUpdater.WixVersionFileName);
        fileSystem
            .ReadAllText(file)
            .ShouldMatchApproved(c => c.SubFolder(PathHelper.Combine("Approved")));
    }

    [Test]
    public void UpdateWixVersionFileWhenFileAlreadyExists()
    {
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            BuildMetaData = new("5.Branch.develop")
            {
                VersionSourceSha = "versionSourceSha",
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                CommitDate = DateTimeOffset.Parse("2019-02-20 23:59:59Z")
            }
        };

        var stringBuilder = new StringBuilder();
        void Action(string s) => stringBuilder.AppendLine(s);

        var logAppender = new TestLogAppender(Action);
        var log = new Log(logAppender);

        var sp = ConfigureServices(service => service.AddSingleton<ILog>(log));

        var fileSystem = sp.GetRequiredService<IFileSystem>();
        var variableProvider = sp.GetRequiredService<IVariableProvider>();
        var versionVariables = variableProvider.GetVariablesFor(semVer, EmptyConfigurationBuilder.New.Build(), 0);

        using var wixVersionFileUpdater = sp.GetRequiredService<IWixVersionFileUpdater>();

        // fake an already existing file
        var file = PathHelper.Combine(workingDir, WixVersionFileUpdater.WixVersionFileName);
        fileSystem.WriteAllText(file, new('x', 1024 * 1024));

        wixVersionFileUpdater.Execute(versionVariables, new(workingDir));

        fileSystem
            .ReadAllText(file)
            .ShouldMatchApproved(c => c.SubFolder(PathHelper.Combine("Approved")));
    }
}
