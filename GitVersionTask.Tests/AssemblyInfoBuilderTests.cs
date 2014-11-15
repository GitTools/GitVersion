using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ApprovalTests;
using GitVersion;
using GitVersion.Configuration;
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
                "feature1","commitSha",DateTimeOffset.Parse("2014-03-06 23:59:59Z"))
        };
        var assemblyInfoBuilder = new AssemblyInfoBuilder
            {
                CachedVersion = new CachedVersion
                {
                    SemanticVersion = semanticVersion,
                    MasterReleaseDate = DateTimeOffset.Parse("2014-03-01 00:00:01Z"),
                }
            };
        var assemblyInfoText = assemblyInfoBuilder.GetAssemblyInfoText(new Config());
        Approvals.Verify(assemblyInfoText);
        var syntaxTree = SyntaxTree.ParseText(assemblyInfoText);
        var references = new[] {new MetadataFileReference(typeof(object).Assembly.Location)};
        var compilation = Compilation.Create("Greeter.dll", new CompilationOptions(OutputKind.NetModule), new[] { syntaxTree }, references);
        var emitResult = compilation.Emit(new MemoryStream());
        Assert.IsTrue(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics.Select(x => x.Info)));
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void VerifyAssemblyVersion_Major()
    {
        VerifyAssemblyVersion(AssemblyVersioningScheme.Major);
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void VerifyAssemblyVersion_MajorMinor()
    {
        VerifyAssemblyVersion(AssemblyVersioningScheme.MajorMinor);
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void VerifyAssemblyVersion_MajorMinorPatch()
    {
        VerifyAssemblyVersion(AssemblyVersioningScheme.MajorMinorPatch);
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void VerifyAssemblyVersion_MajorMinorPatchMetadata()
    {
        VerifyAssemblyVersion(AssemblyVersioningScheme.MajorMinorPatchMetadata);
    }

    static void VerifyAssemblyVersion(AssemblyVersioningScheme avs)
    {
        var semanticVersion = new SemanticVersion
        {
            Major = 2,
            Minor = 3,
            Patch = 4,
            BuildMetaData = new SemanticVersionBuildMetaData(5,
                "master","commitSha",DateTimeOffset.Parse("2014-03-06 23:59:59Z")),
        };
        var assemblyInfoBuilder = new AssemblyInfoBuilder
        {
            CachedVersion = new CachedVersion
            {
                SemanticVersion = semanticVersion,
                MasterReleaseDate = DateTimeOffset.Parse("2014-03-01 00:00:01Z")
            },
        };

        var assemblyInfoText = assemblyInfoBuilder.GetAssemblyInfoText(new Config { AssemblyVersioningScheme = avs });
        Approvals.Verify(assemblyInfoText);
        var syntaxTree = SyntaxTree.ParseText(assemblyInfoText);
        var references = new[] { new MetadataFileReference(typeof(object).Assembly.Location)};
        var compilation = Compilation.Create("Greeter.dll", new CompilationOptions(OutputKind.NetModule), new[] { syntaxTree }, references);
        var emitResult = compilation.Emit(new MemoryStream());
        Assert.IsTrue(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics.Select(x => x.Info)));
    }
}
