using System;
using System.IO;
using System.Linq;

using GitVersion;

using NUnit.Framework;

using Shouldly;

[TestFixture]
public class DocumentationTests
{
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
        var currentDirectory = new DirectoryInfo(Environment.CurrentDirectory);
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