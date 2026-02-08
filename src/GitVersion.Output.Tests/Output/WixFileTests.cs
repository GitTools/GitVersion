using System.IO.Abstractions;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using GitVersion.Output.WixUpdater;
using GitVersion.VersionCalculation;

namespace GitVersion.Output.Tests;

[TestFixture]
[Parallelizable(ParallelScope.None)]
internal class WixFileTests : TestBase
{
    private string workingDir;

    [OneTimeSetUp]
    public void OneTimeSetUp() => workingDir = FileSystemHelper.Path.Combine(FileSystemHelper.Path.GetTempPath(), "WixFileTests");

    [OneTimeTearDown]
    public void OneTimeTearDown() => FileSystemHelper.Directory.DeleteDirectory(workingDir);

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

        var loggerFactory = new TestLoggerFactory(s => stringBuilder.AppendLine(s));

        var sp = ConfigureServices(service => loggerFactory.RegisterWith(service));

        var fileSystem = sp.GetRequiredService<IFileSystem>();
        var variableProvider = sp.GetRequiredService<IVariableProvider>();
        var versionVariables = variableProvider.GetVariablesFor(semVer, EmptyConfigurationBuilder.New.Build(), 0);

        using var wixVersionFileUpdater = sp.GetRequiredService<IWixVersionFileUpdater>();

        wixVersionFileUpdater.Execute(versionVariables, new(workingDir));

        var file = FileSystemHelper.Path.Combine(workingDir, WixVersionFileUpdater.WixVersionFileName);
        fileSystem
            .File.ReadAllText(file)
            .ShouldMatchApproved(c => c.SubFolder(FileSystemHelper.Path.Combine("Approved")));
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

        var loggerFactory = new TestLoggerFactory(s => stringBuilder.AppendLine(s));

        var sp = ConfigureServices(service => loggerFactory.RegisterWith(service));

        var fileSystem = sp.GetRequiredService<IFileSystem>();
        var variableProvider = sp.GetRequiredService<IVariableProvider>();
        var versionVariables = variableProvider.GetVariablesFor(semVer, EmptyConfigurationBuilder.New.Build(), 0);

        using var wixVersionFileUpdater = sp.GetRequiredService<IWixVersionFileUpdater>();

        // fake an already existing file
        var file = FileSystemHelper.Path.Combine(workingDir, WixVersionFileUpdater.WixVersionFileName);
        if (!fileSystem.Directory.Exists(workingDir))
        {
            fileSystem.Directory.CreateDirectory(workingDir);
        }
        fileSystem.File.WriteAllText(file, new('x', 1024 * 1024));

        wixVersionFileUpdater.Execute(versionVariables, new(workingDir));

        fileSystem
            .File.ReadAllText(file)
            .ShouldMatchApproved(c => c.SubFolder(FileSystemHelper.Path.Combine("Approved")));
    }
}
