using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using GitVersion.Output.GitVersionInfo;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Core.Tests;

[TestFixture]
[Parallelizable(ParallelScope.None)]
public class GitVersionInfoGeneratorTests : TestBase
{
    [SetUp]
    public void Setup() => ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestCaseAttribute>();

    [TestCase("cs")]
    [TestCase("fs")]
    [TestCase("vb")]
    public void ShouldCreateFile(string fileExtension)
    {
        var directory = PathHelper.Combine(PathHelper.GetTempPath(), "GitVersionInfoGeneratorTests", Guid.NewGuid().ToString());
        var fileName = "GitVersionInformation.g." + fileExtension;
        var fullPath = PathHelper.Combine(directory, fileName);

        var semanticVersion = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = "unstable4",
            BuildMetaData = new("versionSourceSha", 5,
                "feature1", "commitSha", "commitShortSha", DateTimeOffset.Parse("2014-03-06 23:59:59Z"), 0)
        };

        var sp = ConfigureServices();

        var fileSystem = sp.GetRequiredService<IFileSystem>();
        var variableProvider = sp.GetRequiredService<IVariableProvider>();

        var variables = variableProvider.GetVariablesFor(semanticVersion, EmptyConfigurationBuilder.New.Build(), 0);
        using var generator = sp.GetRequiredService<IGitVersionInfoGenerator>();

        generator.Execute(variables, new(directory, fileName, fileExtension));

        fileSystem.ReadAllText(fullPath).ShouldMatchApproved(c => c.SubFolder(PathHelper.Combine("Approved", fileExtension)));

        DirectoryHelper.DeleteDirectory(directory);
    }
}
