using System;
using System.Diagnostics;
using System.IO;
using GitVersion;
using GitVersion.Helpers;
using GitVersionCore.Tests;
using GitVersion.VersionAssemblyInfoResources;
using NSubstitute;
using NUnit.Framework;

[TestFixture]
public class AssemblyInfoFileUpdateTests
{
    [SetUp]
    public void SetLoggers()
    {
        Logger.SetLoggers(m => Debug.WriteLine(m), m => Debug.WriteLine(m), m => Debug.WriteLine(m));
    }

    private VersionVariables GetTestVariables(string version = "1.0.0")
    {
        return GetTestVariables(SemanticVersion.Parse(version, "v"));
    }

    private VersionVariables GetTestVariables(SemanticVersion version)
    {
        return VariableProvider.GetVariablesFor(version, new TestEffectiveConfiguration(), false);
    }

    [Test]
    public void ShouldCreateCSharpAssemblyInfoFileWhenNotExistsAndEnsureAssemblyInfo()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        const string workingDir = "C:\\Testing";
        const string assemblyInfoFile = "VersionAssemblyInfo.cs";
        var fullPath = Path.Combine(workingDir, assemblyInfoFile);

        var variables = GetTestVariables();
        using (new AssemblyInfoFileUpdate(new Arguments { EnsureAssemblyInfo = true, UpdateAssemblyInfo = true, UpdateAssemblyInfoFileName = assemblyInfoFile}, workingDir, variables, fileSystem))
        {
            var source = AssemblyVersionInfoTemplates.GetAssemblyInfoTemplateFor(fullPath);
            fileSystem.Received(1).WriteAllText(fullPath, source);
        }
    }

    [Test]
    public void ShouldNotCreateCSharpAssemblyInfoFileWhenNotExistsAndNotEnsureAssemblyInfo()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        const string workingDir = "C:\\Testing";
        const string assemblyInfoFile = "VersionAssemblyInfo.cs";
        var fullPath = Path.Combine(workingDir, assemblyInfoFile);

        var variables = GetTestVariables();
        using (new AssemblyInfoFileUpdate(new Arguments { UpdateAssemblyInfo = true, UpdateAssemblyInfoFileName = assemblyInfoFile }, workingDir, variables, fileSystem))
        {
            var source = AssemblyVersionInfoTemplates.GetAssemblyInfoTemplateFor(fullPath);
            fileSystem.Received(0).WriteAllText(fullPath, source);
        }
    }

    [Test]
    public void ShouldNotCreateFSharpAssemblyInfoFileWhenNotExistsAndNotEnsureAssemblyInfo()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        const string workingDir = "C:\\Testing";
        const string assemblyInfoFile = "VersionAssemblyInfo.fs";
        var fullPath = Path.Combine(workingDir, assemblyInfoFile);

        var variables = GetTestVariables();
        using (new AssemblyInfoFileUpdate(new Arguments { UpdateAssemblyInfo = true, UpdateAssemblyInfoFileName = assemblyInfoFile }, workingDir, variables, fileSystem))
        {
            var source = AssemblyVersionInfoTemplates.GetAssemblyInfoTemplateFor(fullPath);
            fileSystem.Received(0).WriteAllText(fullPath, source);
        }
    }

    [Test]
    public void ShouldNotCreateVisualBasicAssemblyInfoFileWhenNotExistsAndNotEnsureAssemblyInfo()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        const string workingDir = "C:\\Testing";
        const string assemblyInfoFile = "VersionAssemblyInfo.vb";
        var fullPath = Path.Combine(workingDir, assemblyInfoFile);

        var variables = GetTestVariables();
        using (new AssemblyInfoFileUpdate(new Arguments { UpdateAssemblyInfo = true, UpdateAssemblyInfoFileName = assemblyInfoFile }, workingDir, variables, fileSystem))
        {
            var source = AssemblyVersionInfoTemplates.GetAssemblyInfoTemplateFor(fullPath);
            fileSystem.Received(0).WriteAllText(fullPath, source);
        }
    }

    [Test]
    public void ShouldCreateVisualBasicAssemblyInfoFileWhenNotExistsAndEnsureAssemblyInfo()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        const string workingDir = "C:\\Testing";
        const string assemblyInfoFile = "VersionAssemblyInfo.vb";
        var fullPath = Path.Combine(workingDir, assemblyInfoFile);

        var variables = GetTestVariables();
        using (new AssemblyInfoFileUpdate(new Arguments { EnsureAssemblyInfo = true, UpdateAssemblyInfo = true, UpdateAssemblyInfoFileName = assemblyInfoFile }, workingDir, variables, fileSystem))
        {
            var source = AssemblyVersionInfoTemplates.GetAssemblyInfoTemplateFor(fullPath);
            fileSystem.Received().WriteAllText(fullPath, source);
        }
    }

    [Test]
    public void ShouldCreateFSharpAssemblyInfoFileWhenNotExistsAndEnsureAssemblyInfo()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        const string workingDir = "C:\\Testing";
        const string assemblyInfoFile = "VersionAssemblyInfo.fs";
        var fullPath = Path.Combine(workingDir, assemblyInfoFile);

        var variables = GetTestVariables();
        using (new AssemblyInfoFileUpdate(new Arguments { EnsureAssemblyInfo = true, UpdateAssemblyInfo = true, UpdateAssemblyInfoFileName = assemblyInfoFile }, workingDir, variables, fileSystem))
        {
            var source = AssemblyVersionInfoTemplates.GetAssemblyInfoTemplateFor(fullPath);
            fileSystem.Received().WriteAllText(fullPath, source);
        }
    }

    [Test]
    public void ShouldNotCreateAssemblyInfoFileForUnknownSourceCodeAndEnsureAssemblyInfo()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        const string workingDir = "C:\\Testing";
        const string assemblyInfoFile = "VersionAssemblyInfo.js";
        var fullPath = Path.Combine(workingDir, assemblyInfoFile);

        var variables = GetTestVariables();
        using (new AssemblyInfoFileUpdate(new Arguments { EnsureAssemblyInfo = true, UpdateAssemblyInfo = true, UpdateAssemblyInfoFileName = assemblyInfoFile }, workingDir, variables, fileSystem))
        {
            fileSystem.Received(0).WriteAllText(fullPath, Arg.Any<string>());
        }
    }

    [Test]
    public void ShouldStartSearchFromWorkingDirectory()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        const string workingDir = "C:\\Testing";

        var variables = GetTestVariables();
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


    [Test]
    public void ShouldReplaceAssemblyVersionWhenCreatingCSharpAssemblyVersionFileAndEnsureAssemblyInfo()
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

        fileSystem.Exists("C:\\Testing\\AssemblyInfo.cs").Returns(false);
        fileSystem.ReadAllText("C:\\Testing\\AssemblyInfo.cs").Returns(assemblyInfoFile);
        var variable = GetTestVariables(version);
        var args = new Arguments
        {
            EnsureAssemblyInfo = true,
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = "AssemblyInfo.cs"
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            var source = AssemblyVersionInfoTemplates.GetAssemblyInfoTemplateFor(args.UpdateAssemblyInfoFileName);

            const string expected = @"AssemblyVersion(""2.3.1.0"");
AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"");
AssemblyFileVersion(""2.3.1.0"");";
            fileSystem.Received(1).WriteAllText("C:\\Testing\\AssemblyInfo.cs", source);
            fileSystem.Received(1).WriteAllText("C:\\Testing\\AssemblyInfo.cs", expected);
        }
    }

    [Test]
    public void ShouldReplaceAssemblyVersionWhenCreatingFSharpAssemblyVersionFileAndEnsureAssemblyInfo()
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

        fileSystem.Exists("C:\\Testing\\AssemblyInfo.fs").Returns(false);
        fileSystem.ReadAllText("C:\\Testing\\AssemblyInfo.fs").Returns(assemblyInfoFile);
        var variable = GetTestVariables(version);
        var args = new Arguments
        {
            EnsureAssemblyInfo = true,
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = "AssemblyInfo.fs"
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            var source = AssemblyVersionInfoTemplates.GetAssemblyInfoTemplateFor(args.UpdateAssemblyInfoFileName);

            const string expected = @"AssemblyVersion(""2.3.1.0"");
AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"");
AssemblyFileVersion(""2.3.1.0"");";
            fileSystem.Received(1).WriteAllText("C:\\Testing\\AssemblyInfo.fs", source);
            fileSystem.Received(1).WriteAllText("C:\\Testing\\AssemblyInfo.fs", expected);
        }
    }

    [Test]
    public void ShouldReplaceAssemblyVersionWhenCreatingVisualBasicAssemblyVersionFileAndEnsureAssemblyInfo()
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

        fileSystem.Exists("C:\\Testing\\AssemblyInfo.fs").Returns(false);
        fileSystem.ReadAllText("C:\\Testing\\AssemblyInfo.vb").Returns(assemblyInfoFile);
        var variable = GetTestVariables(version);
        var args = new Arguments
        {
            EnsureAssemblyInfo = true,
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = "AssemblyInfo.vb"
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            var source = AssemblyVersionInfoTemplates.GetAssemblyInfoTemplateFor(args.UpdateAssemblyInfoFileName);

            const string expected = @"AssemblyVersion(""2.3.1.0"");
AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"");
AssemblyFileVersion(""2.3.1.0"");";
            fileSystem.Received(1).WriteAllText("C:\\Testing\\AssemblyInfo.vb", source);
            fileSystem.Received(1).WriteAllText("C:\\Testing\\AssemblyInfo.vb", expected);
        }
    }
}
