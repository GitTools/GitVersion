using System;
using System.IO;
using GitVersion;
using GitVersion.Helpers;
using GitVersionCore.Tests;
using NSubstitute;
using NUnit.Framework;

[TestFixture]
public class AssemblyInfoFileUpdateTests
{
    [Test]
    public void ShouldStartSearchFromWorkingDirectory()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        const string workingDir = "C:\\Testing";

        var config = new TestEffectiveConfiguration();
        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), config, false);
        using (new AssemblyInfoFileUpdate(new Arguments { UpdateAssemblyInfo = true }, workingDir, variables, fileSystem))
        {
            fileSystem.Received().DirectoryGetFiles(Arg.Is(workingDir), Arg.Any<string>(), Arg.Any<SearchOption>());
        }
    }

    [Test]
    public void ShouldReplaceAssemblyVersion()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        var version = new SemanticVersion
        {
            BuildMetaData = new SemanticVersionBuildMetaData(3, "foo", "hash", DateTimeOffset.Now),
            Major = 2,
            Minor = 3,
            Patch = 1
        };

        const string workingDir = "C:\\Testing";
        const string assemblyInfoFile = @"AssemblyVersion(""1.0.0.0"");
AssemblyInformationalVersion(""1.0.0.0"");
AssemblyFileVersion(""1.0.0.0"");";

        fileSystem.Exists("C:\\Testing\\AssemblyInfo.cs").Returns(true);
        fileSystem.ReadAllText("C:\\Testing\\AssemblyInfo.cs").Returns(assemblyInfoFile);

        var config = new TestEffectiveConfiguration(assemblyVersioningScheme: AssemblyVersioningScheme.MajorMinor);

        var variable = VariableProvider.GetVariablesFor(version, config, false);
        var args = new Arguments
        {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = "AssemblyInfo.cs"
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            const string expected = @"AssemblyVersion(""2.3.0.0"");
AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"");
AssemblyFileVersion(""2.3.1.0"");";
            fileSystem.Received().WriteAllText("C:\\Testing\\AssemblyInfo.cs", expected);
        }
    }

    [Test]
    public void ShouldReplaceAssemblyVersionWithStar()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        var version = new SemanticVersion
        {
            BuildMetaData = new SemanticVersionBuildMetaData(3, "foo", "hash", DateTimeOffset.Now),
            Major = 2,
            Minor = 3,
            Patch = 1
        };

        const string workingDir = "C:\\Testing";
        const string assemblyInfoFile = @"AssemblyVersion(""1.0.0.*"");
AssemblyInformationalVersion(""1.0.0.*"");
AssemblyFileVersion(""1.0.0.*"");";

        fileSystem.Exists("C:\\Testing\\AssemblyInfo.cs").Returns(true);
        fileSystem.ReadAllText("C:\\Testing\\AssemblyInfo.cs").Returns(assemblyInfoFile);

        var config = new TestEffectiveConfiguration(assemblyVersioningScheme: AssemblyVersioningScheme.MajorMinor);
        var variable = VariableProvider.GetVariablesFor(version, config, false);
        var args = new Arguments
        {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = "AssemblyInfo.cs"
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            const string expected = @"AssemblyVersion(""2.3.0.0"");
AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"");
AssemblyFileVersion(""2.3.1.0"");";
            fileSystem.Received().WriteAllText("C:\\Testing\\AssemblyInfo.cs", expected);
        }
    }

    [Test] public void ShouldReplaceAlreadySubstitutedValues()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        var version = new SemanticVersion
        {
            BuildMetaData = new SemanticVersionBuildMetaData(3, "foo", "hash", DateTimeOffset.Now),
            Major = 2,
            Minor = 3,
            Patch = 1
        };

        const string workingDir = "C:\\Testing";
        const string assemblyInfoFile = @"AssemblyVersion(""2.2.0.0"");
AssemblyInformationalVersion(""2.2.0+5.Branch.foo.Sha.hash"");
AssemblyFileVersion(""2.2.0.0"");";

        fileSystem.Exists("C:\\Testing\\AssemblyInfo.cs").Returns(true);
        fileSystem.ReadAllText("C:\\Testing\\AssemblyInfo.cs").Returns(assemblyInfoFile);

        var config = new TestEffectiveConfiguration(assemblyVersioningScheme: AssemblyVersioningScheme.MajorMinor);
        var variable = VariableProvider.GetVariablesFor(version, config, false);
        var args = new Arguments
        {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = "AssemblyInfo.cs"
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            const string expected = @"AssemblyVersion(""2.3.0.0"");
AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"");
AssemblyFileVersion(""2.3.1.0"");";
            fileSystem.Received().WriteAllText("C:\\Testing\\AssemblyInfo.cs", expected);
        }
    }
}