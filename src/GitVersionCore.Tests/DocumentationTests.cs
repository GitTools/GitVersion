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
            var formattedConfigProperty = string.Format("**`{0}:`**", configProperty);
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


    static string ReadDocumentationFile(string relativeDocumentationFilePath)
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

        var documentationFilePath = Path.Combine(currentDirectory.FullName, relativeDocumentationFilePath);
        // Normalize path separators and such.
        documentationFilePath = new FileInfo(documentationFilePath).FullName;

        if (!File.Exists(documentationFilePath))
        {
            throw new FileNotFoundException(string.Format("The documentation file '{0}' couldn't be found.", documentationFilePath), documentationFilePath);
        }

        return File.ReadAllText(documentationFilePath);
    }
}