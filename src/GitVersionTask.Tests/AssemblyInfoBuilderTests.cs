using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using GitVersion;
using GitVersionCore.Tests;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class AssemblyInfoBuilderTests
{
    public interface ICompiler
    {
        Compilation Compile(string assemblyInfoText);

        AssemblyInfoBuilder Builder { get; }

        string ApprovedSubFolder { get; }
    }

    private class CSharpCompiler : ICompiler
    {
        public Compilation Compile(string assemblyInfoText)
        {
            return CSharpCompilation.Create("Fake.dll")
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(assemblyInfoText));
        }

        public AssemblyInfoBuilder Builder { get { return new CSharpAssemblyInfoBuilder(); } }

        public string ApprovedSubFolder { get { return Path.Combine("Approved", "CSharp"); } }
    }

    private class VisualBasicCompiler : ICompiler
    {
        public Compilation Compile(string assemblyInfoText)
        {
            return VisualBasicCompilation.Create("Fake.dll")
                .WithOptions(new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, rootNamespace: "Fake"))
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddSyntaxTrees(VisualBasicSyntaxTree.ParseText(assemblyInfoText));
        }

        public AssemblyInfoBuilder Builder { get { return new VisualBasicAssemblyInfoBuilder(); } }

        public string ApprovedSubFolder { get { return Path.Combine("Approved", "VisualBasic"); } }
    }

    private static readonly ICompiler[] compilers = new ICompiler[]
    {
        new CSharpCompiler(),
        new VisualBasicCompiler()
    };

    [Test]
    [NUnit.Framework.Category("NoMono")]
    [NUnit.Framework.Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void VerifyCreatedCode([ValueSource("compilers")]ICompiler compiler)
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

        var config = new TestEffectiveConfiguration();

        var versionVariables = VariableProvider.GetVariablesFor(semanticVersion, config, false);
        var assemblyInfoText = compiler.Builder.GetAssemblyInfoText(versionVariables, "Fake");
        assemblyInfoText.ShouldMatchApproved(c => c.SubFolder(compiler.ApprovedSubFolder));

        var compilation = compiler.Compile(assemblyInfoText);

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
    [NUnit.Framework.Category("NoMono")]
    [NUnit.Framework.Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void VerifyCreatedCode_NoNamespaceConflict([ValueSource("compilers")]ICompiler compiler)
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

        var config = new TestEffectiveConfiguration();

        var versionVariables = VariableProvider.GetVariablesFor(semanticVersion, config, false);
        var assemblyInfoText = compiler.Builder.GetAssemblyInfoText(versionVariables, "Fake.System");
        assemblyInfoText.ShouldMatchApproved(c => c.SubFolder(compiler.ApprovedSubFolder));

        var compilation = compiler.Compile(assemblyInfoText);

        var emitResult = compilation.Emit(new MemoryStream());
        Assert.IsTrue(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics.Select(x => x.Descriptor)));
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    [NUnit.Framework.Category("NoMono")]
    [NUnit.Framework.Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void VerifyAssemblyVersion_Major([ValueSource("compilers")]ICompiler compiler)
    {
        VerifyAssemblyVersion(compiler, AssemblyVersioningScheme.Major);
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    [NUnit.Framework.Category("NoMono")]
    [NUnit.Framework.Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void VerifyAssemblyVersion_MajorMinor([ValueSource("compilers")]ICompiler compiler)
    {
        VerifyAssemblyVersion(compiler, AssemblyVersioningScheme.MajorMinor);
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    [NUnit.Framework.Category("NoMono")]
    [NUnit.Framework.Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void VerifyAssemblyVersion_MajorMinorPatch([ValueSource("compilers")]ICompiler compiler)
    {
        VerifyAssemblyVersion(compiler, AssemblyVersioningScheme.MajorMinorPatch);
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    [NUnit.Framework.Category("NoMono")]
    [NUnit.Framework.Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void VerifyAssemblyVersion_MajorMinorPatchTag([ValueSource("compilers")]ICompiler compiler)
    {
        VerifyAssemblyVersion(compiler, AssemblyVersioningScheme.MajorMinorPatchTag);
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    [NUnit.Framework.Category("NoMono")]
    [NUnit.Framework.Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void VerifyAssemblyVersion_Major_InvalidInformationalValue([ValueSource("compilers")]ICompiler compiler)
    {
        var exception = Assert.Throws<WarningException>(() => VerifyAssemblyVersion(compiler, AssemblyVersioningScheme.Major, "{ThisVariableDoesntExist}"));
        Assert.That(exception.Message, Does.Contain("ThisVariableDoesntExist"));
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    [NUnit.Framework.Category("NoMono")]
    [NUnit.Framework.Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void VerifyAssemblyVersion_Major_NugetAssemblyInfo([ValueSource("compilers")]ICompiler compiler)
    {
        VerifyAssemblyVersion(compiler, AssemblyVersioningScheme.Major, "{NugetVersion}");
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    [NUnit.Framework.Category("NoMono")]
    public void VerifyAssemblyVersion_MajorMinor_NugetAssemblyInfoWithMultipleVariables([ValueSource("compilers")]ICompiler compiler)
    {
        VerifyAssemblyVersion(compiler, AssemblyVersioningScheme.MajorMinor, "{BranchName}-{Major}.{Minor}.{Patch}-{Sha}");
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    [NUnit.Framework.Category("NoMono")]
    [NUnit.Framework.Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void VerifyAssemblyVersion_MajorMinor_NugetAssemblyInfo([ValueSource("compilers")]ICompiler compiler)
    {
        VerifyAssemblyVersion(compiler, AssemblyVersioningScheme.MajorMinor, "{NugetVersion}");
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    [NUnit.Framework.Category("NoMono")]
    [NUnit.Framework.Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void VerifyAssemblyVersion_MajorMinorPatch_NugetAssemblyInfo([ValueSource("compilers")]ICompiler compiler)
    {
        VerifyAssemblyVersion(compiler, AssemblyVersioningScheme.MajorMinorPatch, "{NugetVersion}");
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    [NUnit.Framework.Category("NoMono")]
    [NUnit.Framework.Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void VerifyAssemblyVersion_MajorMinorPatchTag_NugetAssemblyInfo([ValueSource("compilers")]ICompiler compiler)
    {
        VerifyAssemblyVersion(compiler, AssemblyVersioningScheme.MajorMinorPatchTag, "{NugetVersion}");
    }

    [Test]
    public void GetAssemblyInfoBuilder_Empty_ThrowsWarningException()
    {
        var taskItems = Substitute.For<IEnumerable<ITaskItem>>();
        var exception = Assert.Throws<GitTools.WarningException>(() => AssemblyInfoBuilder.GetAssemblyInfoBuilder(taskItems));
        exception.Message.ShouldBe("Unable to determine which AssemblyBuilder required to generate GitVersion assembly information");
    }

    [Test]
    public void GetAssemblyInfoBuilder_Null_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => AssemblyInfoBuilder.GetAssemblyInfoBuilder(null));
        exception.ParamName.ShouldBe("compileFiles");
    }

    [TestCase("Class1.cs", typeof(CSharpAssemblyInfoBuilder))]
    [TestCase("Class1.vb", typeof(VisualBasicAssemblyInfoBuilder))]
    [TestCase("AssemblyInfo.cs", typeof(CSharpAssemblyInfoBuilder))]
    [TestCase("AssemblyInfo.vb", typeof(VisualBasicAssemblyInfoBuilder))]
    public void GetAssemblyInfoBuilder_ShouldReturnAppropriateAssemblyInfoBuilder(string fileName, Type assemblyInfoBuilderType)
    {
        var taskItem = Substitute.For<ITaskItem>();
        taskItem.ItemSpec.Returns(fileName);

        var assemblyInfoBuilder = AssemblyInfoBuilder.GetAssemblyInfoBuilder(new[] { taskItem });

        assemblyInfoBuilder.ShouldNotBeNull();
        assemblyInfoBuilder.ShouldBeOfType(assemblyInfoBuilderType);
    }

    static void VerifyAssemblyVersion(ICompiler compiler, AssemblyVersioningScheme avs, string assemblyInformationalFormat = null)
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

        var config = new TestEffectiveConfiguration(assemblyVersioningScheme: avs, assemblyInformationalFormat: assemblyInformationalFormat);

        var versionVariables = VariableProvider.GetVariablesFor(semanticVersion, config, false);
        var assemblyInfoText = compiler.Builder.GetAssemblyInfoText(versionVariables, "Fake");
        assemblyInfoText.ShouldMatchApproved(c => c.UseCallerLocation().SubFolder(compiler.ApprovedSubFolder));

        var compilation = compiler.Compile(assemblyInfoText);

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