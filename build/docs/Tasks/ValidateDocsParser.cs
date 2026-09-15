using System.Xml.Linq;
using Docs.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Path = System.IO.Path;

namespace Docs.Tasks;

[TaskName(nameof(ValidateDocsParser))]
[TaskDescription("Verify C# 14 and C# 15 API symbols and XML comments using the docs Roslyn parser")]
public sealed class ValidateDocsParser : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        const string summaryElement = "summary";
        const string source = """
            using System;
            using System.Collections.Generic;

            namespace GitVersion.ParserFixture;
            /// <summary>A primary-constructor type.</summary>
            public class Example(string initial)
            {
                /// <summary>A field-backed property.</summary>
                public string Message { get => field ??= initial; }
            }
            /// <summary>A C# 15 collection-arguments type.</summary>
            public class ReservedPaths
            {
                /// <summary>Case-insensitive paths initialized with collection arguments.</summary>
                public HashSet<string> Paths { get; } = [with(StringComparer.OrdinalIgnoreCase), "/docs", "/api"];
            }
            /// <summary>Extension members.</summary>
            public static class Extensions
            {
                extension(Example example)
                {
                    /// <summary>Read the message.</summary>
                    public string ReadMessage() => example.Message;
                }
            }
            """;
        var root = Path.Combine(Path.GetTempPath(), "gitversion-docs-parser-" + Guid.NewGuid().ToString("N"));
        try
        {
            const string name = "GitVersion.ParserFixture";
            var project = Path.Combine(root, "src", name);
            var assemblies = Path.Combine(root, "assemblies");
            Directory.CreateDirectory(project);
            Directory.CreateDirectory(assemblies);
            var sourcePath = Path.Combine(project, "Example.cs");
            File.WriteAllText(sourcePath, source);
            var formats = Path.Combine(project, "GitVersionInfo", "AddFormats");
            Directory.CreateDirectory(formats);
            File.WriteAllText(Path.Combine(formats, "GitVersionInformation.cs"), "public const string {0} = \"{1}\";");
            var tree = DocsApi.ParseSource(source, sourcePath);
            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Select(p => MetadataReference.CreateFromFile(p));
            var compilation = CSharpCompilation.Create(name, [tree], references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var emitted = compilation.Emit(Path.Combine(assemblies, name + ".dll"));
            Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));

            var previousLanguageTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14));
            Assert.Contains(compilation.ReplaceSyntaxTree(tree, previousLanguageTree).GetDiagnostics(),
                d => d.Severity == DiagnosticSeverity.Error);

            DocsApi.Prepare(root, assemblies);

            Assert.Contains("T:GitVersion.ParserFixture.Example", File.ReadAllLines(Path.Combine(assemblies, "public-types.txt")));
            Assert.Contains("T:GitVersion.ParserFixture.ReservedPaths", File.ReadAllLines(Path.Combine(assemblies, "public-types.txt")));
            var members = XDocument.Load(Path.Combine(assemblies, name + ".xml")).Descendants("member").ToArray();
            Assert.Contains(members, m => (string?)m.Attribute("name") == "T:GitVersion.ParserFixture.ReservedPaths"
                && m.Element(summaryElement)?.Value.Trim() == "A C# 15 collection-arguments type.");
            Assert.Contains(members, m => (string?)m.Attribute("name") == "P:GitVersion.ParserFixture.ReservedPaths.Paths"
                && m.Element(summaryElement)?.Value.Trim() == "Case-insensitive paths initialized with collection arguments.");
            Assert.Contains(members, m => (string?)m.Attribute("name") == "P:GitVersion.ParserFixture.Example.Message"
                && m.Element(summaryElement)?.Value.Trim() == "A field-backed property.");
            Assert.Contains(members, m => m.Element(summaryElement)?.Value.Trim() == "Read the message.");
            Assert.Throws<InvalidOperationException>(() => DocsApi.ParseSource("public class Broken {", "Broken.cs"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }
}
