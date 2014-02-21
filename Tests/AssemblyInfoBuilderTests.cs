using System;
using System.IO;
using System.Linq;
using ApprovalTests;
using GitFlowVersion;
using GitFlowVersionTask;
using NUnit.Framework;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

[TestFixture]
public class AssemblyInfoBuilderTests
{

    [Test]
    public void VerifyCreatedCode()
    {
        var semanticVersion = new VersionAndBranch
        {
            BranchType = BranchType.Feature,
            BranchName = "feature1",
            Sha = "a682956dc1a2752aa24597a0f5cd939f93614509",
            Version = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 3,
                Tag = "unstable4",
                Suffix = "a682956d",
            }
        };
        var assemblyInfoBuilder = new AssemblyInfoBuilder
            {
                VersionAndBranch = semanticVersion,
                AssemblyName = "MyAssembly"
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