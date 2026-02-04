using System.Globalization;
using System.IO.Abstractions;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using GitVersion.Output.GitVersionInfo;
using GitVersion.VersionCalculation;

namespace GitVersion.Output.Tests;

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
        var fileContents = GenerateGitVersionInformationFile(fileExtension);

        fileContents.ShouldMatchApproved(c => c.SubFolder(FileSystemHelper.Path.Combine("Approved", fileExtension)));
    }

    /// <summary>
    /// Regression test for issue #4196 (https://github.com/GitTools/GitVersion/issues/4196)
    /// </summary>
    [TestCase("cs")]
    [TestCase("fs")]
    [TestCase("vb")]
    public void ShouldProperlyOutputNamespaceDeclaration(string fileExtension)
    {
        const string targetNamespace = "My.Custom.Namespace";

        var fileContents = GenerateGitVersionInformationFile(fileExtension, targetNamespace);

        fileContents.ShouldMatchApproved(c => c.SubFolder(FileSystemHelper.Path.Combine("Approved", fileExtension)));
    }

    private static SemanticVersion CreateSemanticVersion()
    {
        var versionSourceSemVer = new SemanticVersion(1, 2, 2);
        return new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = "unstable4",
            BuildMetaData = new(
                versionSourceSemVer,
                "versionSourceSha",
                5,
                "feature1",
                "commitSha",
                "commitShortSha",
                DateTimeOffset.Parse("2014-03-06 23:59:59Z", CultureInfo.InvariantCulture),
                0)
        };
    }

    private static (string Directory, string FileName, string FullPath) CreateTempOutputPath(IFileSystem fileSystem, string fileExtension)
    {
        var directory = FileSystemHelper.Path.Combine(FileSystemHelper.Path.GetTempPath(), nameof(GitVersionInfoGeneratorTests), Guid.NewGuid().ToString());
        if (!fileSystem.Directory.Exists(directory))
            fileSystem.Directory.CreateDirectory(directory);

        var fileName = "GitVersionInformation.g." + fileExtension;
        var fullPath = FileSystemHelper.Path.Combine(directory, fileName);
        return (directory, fileName, fullPath);
    }

    private string GenerateGitVersionInformationFile(string fileExtension, string? targetNamespace = null)
    {
        var semanticVersion = CreateSemanticVersion();

        var sp = ConfigureServices();
        var fileSystem = sp.GetRequiredService<IFileSystem>();

        var (directory, fileName, fullPath) = CreateTempOutputPath(fileSystem, fileExtension);
        try
        {
            var variables = sp.GetRequiredService<IVariableProvider>()
                .GetVariablesFor(semanticVersion, EmptyConfigurationBuilder.New.Build(), 0);

            using var generator = sp.GetRequiredService<IGitVersionInfoGenerator>();
            generator.Execute(variables, new(directory, fileName, targetNamespace));

            return fileSystem.File.ReadAllText(fullPath);
        }
        finally
        {
            FileSystemHelper.Directory.DeleteDirectory(directory);
        }
    }
}
