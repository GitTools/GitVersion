using System;
using System.IO;
using System.Linq;
using System.Reflection;

using GitVersion;

using NUnit.Framework;

using Shouldly;

using YamlDotNet.Serialization;

[TestFixture]
public class DocumentationTests
{
    private DirectoryInfo docsDirectory;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        docsDirectory = GetDocsDirectory();
    }

    [Test]
    public void ConfigurationDocumentationIsUpToDate()
    {
        var configurationDocumentationFile = ReadDocumentationFile("configuration.md");

        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance;
        var configProperties = typeof(Config)
            .GetProperties(bindingFlags)
            .Union(typeof(BranchConfig).GetProperties(bindingFlags))
            .Select(p => p.GetCustomAttribute<YamlMemberAttribute>())
            .Where(a => a != null)
            .Select(a => a.Alias)
            .ToList();

        configProperties.ShouldNotBeEmpty();

        foreach (var configProperty in configProperties)
        {
            var formattedConfigProperty = string.Format("### {0}", configProperty);
            configurationDocumentationFile.ShouldContain(formattedConfigProperty,
                                                         Environment.NewLine + configurationDocumentationFile);
        }
    }


    [Test]
    public void VariableDocumentationIsUpToDate()
    {
        var variableDocumentationFile = ReadDocumentationFile("more-info/variables.md");
        var variables = VersionVariables.AvailableVariables.ToList();

        variables.ShouldNotBeEmpty();

        foreach (var variable in variables)
        {
            variableDocumentationFile.ShouldContain(variable,
                                                    Environment.NewLine + variableDocumentationFile);
        }
    }

    [Test]
    public void DocumentationIndexIsUpToDate()
    {
        var documentationIndexFile = ReadDocumentationFile("../mkdocs.yml");
        var docsDirectoryPath = new Uri(docsDirectory.FullName, UriKind.Absolute);

        Console.WriteLine(docsDirectoryPath);

        foreach (var markdownFile in docsDirectory.EnumerateFiles("*.md", SearchOption.AllDirectories))
        {
            var fullPath = new Uri(markdownFile.FullName, UriKind.Absolute);
            var relativePath = docsDirectoryPath
                .MakeRelativeUri(fullPath)
                .ToString()
                .Replace("docs/", string.Empty);

            Console.WriteLine(fullPath);
            Console.WriteLine(relativePath);

            documentationIndexFile.ShouldContain(relativePath, () => string.Format("The file '{0}' is not listed in 'mkdocs.yml'.", relativePath));
        }
    }

    private string ReadDocumentationFile(string relativeDocumentationFilePath)
    {
        var documentationFilePath = Path.Combine(docsDirectory.FullName, relativeDocumentationFilePath);
        // Normalize path separators and such.
        documentationFilePath = new FileInfo(documentationFilePath).FullName;

        if (!File.Exists(documentationFilePath))
        {
            throw new FileNotFoundException(string.Format("The documentation file '{0}' couldn't be found.", documentationFilePath), documentationFilePath);
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

        if (currentDirectory == null || !currentDirectory.Name.Equals("docs", StringComparison.Ordinal))
        {
            throw new DirectoryNotFoundException("Couldn't find the 'docs' directory.");
        }

        return currentDirectory;
    }
}