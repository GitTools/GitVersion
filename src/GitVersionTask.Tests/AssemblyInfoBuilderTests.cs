using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using GitVersion;
using GitVersionCore.Tests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Shouldly;

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
                "feature1", "commitSha", DateTimeOffset.Parse("2014-03-06 23:59:59Z"))
        };
        var assemblyInfoBuilder = new AssemblyInfoBuilder();

        var config = new TestEffectiveConfiguration();

        var versionVariables = VariableProvider.GetVariablesFor(semanticVersion, config, false);
        var assemblyInfoText = assemblyInfoBuilder.GetAssemblyInfoText(versionVariables, "Fake");
        assemblyInfoText.ShouldMatchApproved();

        var compilation = CSharpCompilation.Create("Fake.dll")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(assemblyInfoText));

        using (var stream = new MemoryStream())
        {
            var emitResult = compilation.Emit(stream);

            Assert.IsTrue(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics.Select(x => x.Descriptor)));

            stream.Seek(0, SeekOrigin.Begin);
            var assembly = Assembly.Load(stream.ToArray());
            VerifyGitVersionInformationAttribute(assembly, versionVariables);
        }
    }

    [Test]
    public void VerifyCreatedCode_NoNamespaceConflict()
    {
        var semanticVersion = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = "unstable4",
            BuildMetaData = new SemanticVersionBuildMetaData(5,
                "feature1", "commitSha", DateTimeOffset.Parse("2014-03-06 23:59:59Z"))
        };
        var assemblyInfoBuilder = new AssemblyInfoBuilder();

        var config = new TestEffectiveConfiguration();

        var versionVariables = VariableProvider.GetVariablesFor(semanticVersion, config, false);
        var assemblyInfoText = assemblyInfoBuilder.GetAssemblyInfoText(versionVariables, "Fake.System");
        assemblyInfoText.ShouldMatchApproved();

        var compilation = CSharpCompilation.Create("Fake.System.dll")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(assemblyInfoText));

        var emitResult = compilation.Emit(new MemoryStream());
        Assert.IsTrue(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics.Select(x => x.Descriptor)));
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
    public void VerifyAssemblyVersion_MajorMinorPatchTag()
    {
        VerifyAssemblyVersion(AssemblyVersioningScheme.MajorMinorPatchTag);
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    [ExpectedException(typeof(WarningException))]
    public void VerifyAssemblyVersion_Major_InvalidInformationalValue()
    {
        VerifyAssemblyVersion(AssemblyVersioningScheme.Major, "{ThisVariableDoesntExist}");
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void VerifyAssemblyVersion_Major_NugetAssemblyInfo()
    {
        VerifyAssemblyVersion(AssemblyVersioningScheme.Major, "{NugetVersion}");
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void VerifyAssemblyVersion_MajorMinor_NugetAssemblyInfoWithMultipleVariables()
    {
        VerifyAssemblyVersion(AssemblyVersioningScheme.MajorMinor, "{BranchName}-{Major}.{Minor}.{Patch}-{Sha}");
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void VerifyAssemblyVersion_MajorMinor_NugetAssemblyInfo()
    {
        VerifyAssemblyVersion(AssemblyVersioningScheme.MajorMinor, "{NugetVersion}");
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void VerifyAssemblyVersion_MajorMinorPatch_NugetAssemblyInfo()
    {
        VerifyAssemblyVersion(AssemblyVersioningScheme.MajorMinorPatch, "{NugetVersion}");
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void VerifyAssemblyVersion_MajorMinorPatchTag_NugetAssemblyInfo()
    {
        VerifyAssemblyVersion(AssemblyVersioningScheme.MajorMinorPatchTag, "{NugetVersion}");
    }

    static void VerifyAssemblyVersion(AssemblyVersioningScheme avs, string assemblyInformationalFormat = null)
    {
        var semanticVersion = new SemanticVersion
        {
            Major = 2,
            Minor = 3,
            Patch = 4,
            PreReleaseTag = "beta.5",
            BuildMetaData = new SemanticVersionBuildMetaData(6,
                "master", "commitSha", DateTimeOffset.Parse("2014-03-06 23:59:59Z")),
        };
        var assemblyInfoBuilder = new AssemblyInfoBuilder();


        var config = new TestEffectiveConfiguration(assemblyVersioningScheme: avs, assemblyInformationalFormat: assemblyInformationalFormat);

        var versionVariables = VariableProvider.GetVariablesFor(semanticVersion, config, false);
        var assemblyInfoText = assemblyInfoBuilder.GetAssemblyInfoText(versionVariables, "Fake");
        assemblyInfoText.ShouldMatchApproved(c => c.UseCallerLocation());

        var compilation = CSharpCompilation.Create("Fake.dll")
            .WithOptions(new CSharpCompilationOptions(OutputKind.NetModule))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(assemblyInfoText));

        var emitResult = compilation.Emit(new MemoryStream());
        Assert.IsTrue(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics.Select(x => x.Descriptor)));
    }

    static void VerifyGitVersionInformationAttribute(Assembly assembly, VersionVariables versionVariables)
    {
        var gitVersionInformation = assembly.GetType("Fake.GitVersionInformation");
        var fields = gitVersionInformation.GetFields(BindingFlags.Static | BindingFlags.Public);

        foreach (var variable in versionVariables)
        {
            Assert.IsNotNull(variable.Value);

            var field = fields.FirstOrDefault(p => p.Name == variable.Key);
            Assert.IsNotNull(field);

            var value = field.GetValue(null);
            Assert.AreEqual(variable.Value, value, "{0} had an invalid value.", field.Name);
        }
    }
}