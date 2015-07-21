using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ApprovalTests;
using GitVersion;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

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
        var versionVariables = VariableProvider.GetVariablesFor(semanticVersion, AssemblyVersioningScheme.MajorMinorPatch, VersioningMode.ContinuousDelivery, "ci", false);
        var assemblyInfoText = assemblyInfoBuilder.GetAssemblyInfoText(versionVariables, "Fake");
        Approvals.Verify(assemblyInfoText);

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

    static void VerifyAssemblyVersion(AssemblyVersioningScheme avs)
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
        var versionVariables = VariableProvider.GetVariablesFor(semanticVersion, avs, VersioningMode.ContinuousDelivery, "ci", false);
        var assemblyInfoText = assemblyInfoBuilder.GetAssemblyInfoText(versionVariables, "Fake");
        Approvals.Verify(assemblyInfoText);

        var compilation = CSharpCompilation.Create("Fake.dll")
    .WithOptions(new CSharpCompilationOptions(OutputKind.NetModule))
    .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
    .AddSyntaxTrees(CSharpSyntaxTree.ParseText(assemblyInfoText));

        var emitResult = compilation.Emit(new MemoryStream());
        Assert.IsTrue(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics.Select(x => x.Descriptor)));
    }

    static void VerifyGitVersionInformationAttribute(Assembly assembly, VersionVariables versionVariables)
    {
        var gitVersionInformationAttributeData = assembly.CustomAttributes
            .FirstOrDefault(a => a.AttributeType.Name == "GitVersionInformationAttribute");

        Assert.IsNotNull(gitVersionInformationAttributeData);

        var gitVersionInformationAttributeType = gitVersionInformationAttributeData.AttributeType;
        var gitVersionInformationAttribute = assembly
            .GetCustomAttributes(gitVersionInformationAttributeType)
            .FirstOrDefault();

        Assert.IsNotNull(gitVersionInformationAttribute);

        var properties = gitVersionInformationAttributeType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var variable in versionVariables)
        {
            Assert.IsNotNull(variable.Value);

            var property = properties.FirstOrDefault(p => p.Name == variable.Key);
            Assert.IsNotNull(property);

            var propertyValue = property.GetValue(gitVersionInformationAttribute, null);
            Assert.AreEqual(variable.Value, propertyValue, "{0} had an invalid value.", property.Name);
        }
    }
}