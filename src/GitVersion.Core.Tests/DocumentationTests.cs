using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using GitVersion.OutputVariables;

namespace GitVersion.Core.Tests;

[TestFixture]
public class DocumentationTests : TestBase
{
    private DirectoryInfo docsDirectory;

    [OneTimeSetUp]
    public void OneTimeSetUp() => this.docsDirectory = GetDocsDirectory();

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
        documentationFilePath = new FileInfo(documentationFilePath).FullName;

        if (!File.Exists(documentationFilePath))
        {
            throw new FileNotFoundException($"The documentation file '{documentationFilePath}' couldn't be found.", documentationFilePath);
        }

        return File.ReadAllText(documentationFilePath);
    }

    private static DirectoryInfo GetDocsDirectory()
    {
        var currentDirectory = new FileInfo(typeof(DocumentationTests).Assembly.Location).Directory;
        while (currentDirectory != null)
        {
            var docsDirectory = currentDirectory
                .EnumerateDirectories("docs", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

            if (docsDirectory != null)
            {
                currentDirectory = docsDirectory;
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
