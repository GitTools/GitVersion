using System;
using System.IO;
using System.Linq;
using ApprovalTests;
using GitVersion;
using GitVersionTask;
using NUnit.Framework;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

[TestFixture]
public class AssemblyInfoBuilderTests
{

    [Test]
    public void VerifyCreatedCode()
    {
        var semanticVersion = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = "unstable4",
            BuildMetaData = new SemanticVersionBuildMetaData(5,
                "feature1",
                "a682956dc1a2752aa24597a0f5cd939f93614509",
                DateTimeOffset.Parse("2014-03-01 00:00:01Z"),
                DateTimeOffset.Parse("2014-03-06 23:59:59Z")),
        };
        var assemblyInfoBuilder = new AssemblyInfoBuilder
            {
                SemanticVersion = semanticVersion
            };
        var assemblyInfoText = assemblyInfoBuilder.GetAssemblyInfoText();
        Approvals.Verify(assemblyInfoText);
        var syntaxTree = SyntaxTree.ParseText(assemblyInfoText);
        var references = new[] {new MetadataFileReference(typeof(object).Assembly.Location), };
        var compilation = Compilation.Create("Greeter.dll", new CompilationOptions(OutputKind.NetModule), new[] { syntaxTree }, references);
        var emitResult = compilation.Emit(new MemoryStream());
        Assert.IsTrue(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics.Select(x => x.Info)));
    }
}