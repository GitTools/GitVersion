using System.IO.Abstractions;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using GitVersion.OutputVariables;

namespace GitVersion.Core.Tests;

[TestFixture]
public class DocumentationTests : TestBase
{
    private IFileSystem fileSystem;
    private IDirectoryInfo docsDirectory;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        this.fileSystem = new FileSystem();
        this.docsDirectory = GetDocsDirectory();
    }

    [Test]
    public void ConfigurationDocumentationIsUpToDate()
    {
        var configurationDocumentationFile = ReadDocumentationFile("input/docs/reference/configuration.md");

        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance;
        var configProperties = typeof(GitVersionConfiguration)
            .GetProperties(bindingFlags)
            .Union(typeof(BranchConfiguration).GetProperties(bindingFlags))
            .Select(p => p.GetCustomAttribute<JsonPropertyNameAttribute>())
            .Where(a => a != null)
            .Select(a => a?.Name)
            .ToList();

        configProperties.ShouldNotBeEmpty();

        foreach (var configProperty in configProperties)
        {
            var formattedConfigProperty = $"### {configProperty}";
            configurationDocumentationFile.ShouldContain(formattedConfigProperty, Case.Insensitive,
                PathHelper.NewLine + configurationDocumentationFile);
        }
    }

    [Test]
    public void VariableDocumentationIsUpToDate()
    {
        var variableDocumentationFile = ReadDocumentationFile("input/docs/reference/variables.md");
        var variables = GitVersionVariables.AvailableVariables.ToList();

        variables.ShouldNotBeEmpty();

        foreach (var variable in variables)
        {
            variableDocumentationFile.ShouldContain(variable, Case.Insensitive,
                PathHelper.NewLine + variableDocumentationFile);
        }
    }

    private string ReadDocumentationFile(string relativeDocumentationFilePath)
    {
        var documentationFilePath = PathHelper.Combine(this.docsDirectory.FullName, relativeDocumentationFilePath);
        // Normalize path separators and such.
        documentationFilePath = fileSystem.FileInfo.New(documentationFilePath).FullName;

        if (!this.fileSystem.File.Exists(documentationFilePath))
        {
            throw new FileNotFoundException($"The documentation file '{documentationFilePath}' couldn't be found.", documentationFilePath);
        }

        return this.fileSystem.File.ReadAllText(documentationFilePath);
    }

    private IDirectoryInfo GetDocsDirectory()
    {
        var currentDirectory = this.fileSystem.FileInfo.New(typeof(DocumentationTests).Assembly.Location).Directory;
        while (currentDirectory != null)
        {
            var searchedDirectory = currentDirectory
                .EnumerateDirectories("docs", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

            if (searchedDirectory != null)
            {
                currentDirectory = searchedDirectory;
                break;
            }

            currentDirectory = currentDirectory.Parent;
        }

        if (currentDirectory?.Name.Equals("docs", StringComparison.Ordinal) != true)
        {
            throw new DirectoryNotFoundException("Couldn't find the 'docs' directory.");
        }

        return currentDirectory;
    }
}
