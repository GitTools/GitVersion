using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GitVersion;
using GitVersion.Helpers;
using GitVersionCore.Tests;
using GitVersion.VersionAssemblyInfoResources;
using NSubstitute;
using NUnit.Framework;
using System;
using System.IO;
using Shouldly;

[TestFixture]
public class AssemblyInfoFileUpdateTests
{
    [SetUp]
    public void SetLoggers()
    {
        Logger.SetLoggers(m => Debug.WriteLine(m), m => Debug.WriteLine(m), m => Debug.WriteLine(m));
    }
    
    [Test]
    public void ShouldCreateCSharpAssemblyInfoFileWhenNotExistsAndEnsureAssemblyInfo()
    {
        var fileSystem = new TestFileSystem();
        const string workingDir = "C:\\Testing";
        ISet<string> assemblyInfoFile = new HashSet<string> { "VersionAssemblyInfo.cs" };
        var fullPath = Path.Combine(workingDir, assemblyInfoFile.First());

        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);
        using (new AssemblyInfoFileUpdate(new Arguments { EnsureAssemblyInfo = true, UpdateAssemblyInfo = true, UpdateAssemblyInfoFileName = assemblyInfoFile}, workingDir, variables, fileSystem))
        {
            fileSystem.ReadAllText(fullPath).ShouldMatchApproved();
        }
    }

    [Test]
    public void ShouldCreateCSharpAssemblyInfoFileAtPathWhenNotExistsAndEnsureAssemblyInfo()
    {
        var fileSystem = new TestFileSystem();
        const string workingDir = "C:\\Testing";
        ISet<string> assemblyInfoFile = new HashSet<string> { @"src\Project\Properties\VersionAssemblyInfo.cs" };
        var fullPath = Path.Combine(workingDir, assemblyInfoFile.First());

        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);
        using (new AssemblyInfoFileUpdate(new Arguments { EnsureAssemblyInfo = true, UpdateAssemblyInfo = true, UpdateAssemblyInfoFileName = assemblyInfoFile }, workingDir, variables, fileSystem))
        {
            fileSystem.ReadAllText(fullPath).ShouldMatchApproved();
        }
    }

    [Test]
    public void ShouldCreateCSharpAssemblyInfoFilesAtPathWhenNotExistsAndEnsureAssemblyInfo()
    {
        var fileSystem = new TestFileSystem();
        const string workingDir = "C:\\Testing";
        ISet<string> assemblyInfoFile = new HashSet<string> { "AssemblyInfo.cs", @"src\Project\Properties\VersionAssemblyInfo.cs" };
        
        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);
        using (new AssemblyInfoFileUpdate(new Arguments { EnsureAssemblyInfo = true, UpdateAssemblyInfo = true, UpdateAssemblyInfoFileName = assemblyInfoFile }, workingDir, variables, fileSystem))
        {
            foreach (var item in assemblyInfoFile)
            {
                var fullPath = Path.Combine(workingDir, item);
                // ReSharper disable once AccessToForEachVariableInClosure
                fileSystem.ReadAllText(fullPath).ShouldMatchApproved(c => c.WithDescriminator(item.Replace("\\", string.Empty).Replace(".", string.Empty)));
            }
        }
    }

    [Test]
    public void ShouldNotCreateCSharpAssemblyInfoFileWhenNotExistsAndNotEnsureAssemblyInfo()
    {
        var fileSystem = new TestFileSystem();
        const string workingDir = "C:\\Testing";
        ISet<string> assemblyInfoFile = new HashSet<string> { "VersionAssemblyInfo.cs" };
        var fullPath = Path.Combine(workingDir, assemblyInfoFile.First());

        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);
        using (new AssemblyInfoFileUpdate(new Arguments { UpdateAssemblyInfo = true, UpdateAssemblyInfoFileName = assemblyInfoFile }, workingDir, variables, fileSystem))
        {
            fileSystem.Exists(fullPath).ShouldBeFalse();
        }
    }

    [Test]
    public void ShouldNotCreateFSharpAssemblyInfoFileWhenNotExistsAndNotEnsureAssemblyInfo()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        const string workingDir = "C:\\Testing";
        ISet<string> assemblyInfoFile = new HashSet<string> { "VersionAssemblyInfo.fs" };
        var fullPath = Path.Combine(workingDir, assemblyInfoFile.First());

        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);
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
        ISet<string> assemblyInfoFile = new HashSet<string> { "VersionAssemblyInfo.vb" };
        var fullPath = Path.Combine(workingDir, assemblyInfoFile.First());


        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);
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
        ISet<string> assemblyInfoFile = new HashSet<string> { "VersionAssemblyInfo.vb" };
        var fullPath = Path.Combine(workingDir, assemblyInfoFile.First());
        
        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);
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
        ISet<string> assemblyInfoFile = new HashSet<string> { "VersionAssemblyInfo.fs" };
        var fullPath = Path.Combine(workingDir, assemblyInfoFile.First());

        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);
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
        ISet<string> assemblyInfoFile = new HashSet<string> { "VersionAssemblyInfo.js" };
        var fullPath = Path.Combine(workingDir, assemblyInfoFile.First());

        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);
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
            UpdateAssemblyInfoFileName = new HashSet<string> { "AssemblyInfo.cs" }
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
    public void ShouldReplaceAssemblyVersionInRelativePath()
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

        fileSystem.Exists("C:\\Testing\\Project\\src\\Properties\\AssemblyInfo.cs").Returns(true);
        fileSystem.ReadAllText("C:\\Testing\\Project\\src\\Properties\\AssemblyInfo.cs").Returns(assemblyInfoFile);

        var config = new TestEffectiveConfiguration(assemblyVersioningScheme: AssemblyVersioningScheme.MajorMinor);

        var variable = VariableProvider.GetVariablesFor(version, config, false);
        var args = new Arguments
        {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = new HashSet<string> { @"Project\src\Properties\AssemblyInfo.cs" }
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            const string expected = @"AssemblyVersion(""2.3.0.0"");
AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"");
AssemblyFileVersion(""2.3.1.0"");";
            fileSystem.Received().WriteAllText("C:\\Testing\\Project\\src\\Properties\\AssemblyInfo.cs", expected);
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
            UpdateAssemblyInfoFileName = new HashSet<string> { "AssemblyInfo.cs" }
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
    public void ShouldAddAssemblyVersionIfMissingFromInfoFile()
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
        const string assemblyInfoFileContent = @"";

        fileSystem.Exists("C:\\Testing\\AssemblyInfo.cs").Returns(true);
        fileSystem.ReadAllText("C:\\Testing\\AssemblyInfo.cs").Returns(assemblyInfoFileContent);

        var config = new TestEffectiveConfiguration(assemblyVersioningScheme: AssemblyVersioningScheme.MajorMinor);

        var variable = VariableProvider.GetVariablesFor(version, config, false);
        var args = new Arguments
        {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = new HashSet<string> { "AssemblyInfo.cs" }
        };

        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            const string expected = @"
[assembly: AssemblyVersion(""2.3.0.0"")]
[assembly: AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")]
[assembly: AssemblyFileVersion(""2.3.1.0"")]";
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
            UpdateAssemblyInfoFileName = new HashSet<string> { "AssemblyInfo.cs" }
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
        var fileSystem = new TestFileSystem();
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

        fileSystem.WriteAllText("C:\\Testing\\AssemblyInfo.cs", assemblyInfoFile);
        var variable = VariableProvider.GetVariablesFor(version, new TestEffectiveConfiguration(), false);
        var args = new Arguments
        {
            EnsureAssemblyInfo = true,
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = new HashSet<string> { "AssemblyInfo.cs" }
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            const string expected = @"AssemblyVersion(""2.3.1.0"");
AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"");
AssemblyFileVersion(""2.3.1.0"");";
            fileSystem.ReadAllText("C:\\Testing\\AssemblyInfo.cs").ShouldBe(expected);
        }
    }

    [Test]
    public void ShouldReplaceAssemblyVersionWhenCreatingFSharpAssemblyVersionFileAndEnsureAssemblyInfo()
    {
        var fileSystem = new TestFileSystem();
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

        fileSystem.WriteAllText("C:\\Testing\\AssemblyInfo.fs", assemblyInfoFile);
        var variable = VariableProvider.GetVariablesFor(version, new TestEffectiveConfiguration(), false);
        var args = new Arguments
        {
            EnsureAssemblyInfo = true,
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = new HashSet<string> { "AssemblyInfo.fs" }
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            const string expected = @"AssemblyVersion(""2.3.1.0"");
AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"");
AssemblyFileVersion(""2.3.1.0"");";
            fileSystem.ReadAllText("C:\\Testing\\AssemblyInfo.fs").ShouldBe(expected);
        }
    }

    [Test]
    public void ShouldReplaceAssemblyVersionWhenCreatingVisualBasicAssemblyVersionFileAndEnsureAssemblyInfo()
    {
        var fileSystem = new TestFileSystem();
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
        
        fileSystem.WriteAllText("C:\\Testing\\AssemblyInfo.vb", assemblyInfoFile);
        var variable = VariableProvider.GetVariablesFor(version, new TestEffectiveConfiguration(), false);
        var args = new Arguments
        {
            EnsureAssemblyInfo = true,
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = new HashSet<string> { "AssemblyInfo.vb" }
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            const string expected = @"AssemblyVersion(""2.3.1.0"");
AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"");
AssemblyFileVersion(""2.3.1.0"");";
            fileSystem.ReadAllText("C:\\Testing\\AssemblyInfo.vb").ShouldBe(expected);
        }
    }
}
