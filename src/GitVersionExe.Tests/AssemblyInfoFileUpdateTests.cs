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
using System.Text;

[TestFixture]
public class AssemblyInfoFileUpdateTests
{
    [SetUp]
    public void SetLoggers()
    {
        Logger.SetLoggers(m => Debug.WriteLine(m), m => Debug.WriteLine(m), m => Debug.WriteLine(m));
    }
    
    [Test]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ShouldCreateCSharpAssemblyInfoFileWhenNotExistsAndEnsureAssemblyInfo()
    {
        var fileSystem = new TestFileSystem();
        var workingDir = Path.GetTempPath();
        ISet<string> assemblyInfoFile = new HashSet<string> { "VersionAssemblyInfo.cs" };
        var fullPath = Path.Combine(workingDir, assemblyInfoFile.First());

        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);
        using (new AssemblyInfoFileUpdate(new Arguments { EnsureAssemblyInfo = true, UpdateAssemblyInfo = true, UpdateAssemblyInfoFileName = assemblyInfoFile}, workingDir, variables, fileSystem))
        {
            fileSystem.ReadAllText(fullPath).ShouldMatchApproved();
        }
    }

    [Test]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ShouldCreateCSharpAssemblyInfoFileAtPathWhenNotExistsAndEnsureAssemblyInfo()
    {
        var fileSystem = new TestFileSystem();
        var workingDir = Path.GetTempPath();
        ISet<string> assemblyInfoFile = new HashSet<string> { Path.Combine("src", "Project", "Properties", "VersionAssemblyInfo.cs") };
        var fullPath = Path.Combine(workingDir, assemblyInfoFile.First());

        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);
        using (new AssemblyInfoFileUpdate(new Arguments { EnsureAssemblyInfo = true, UpdateAssemblyInfo = true, UpdateAssemblyInfoFileName = assemblyInfoFile }, workingDir, variables, fileSystem))
        {
            fileSystem.ReadAllText(fullPath).ShouldMatchApproved();
        }
    }

    [Test]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ShouldCreateCSharpAssemblyInfoFilesAtPathWhenNotExistsAndEnsureAssemblyInfo()
    {
        var fileSystem = new TestFileSystem();
        var workingDir = Path.GetTempPath();
        ISet<string> assemblyInfoFile = new HashSet<string> { "AssemblyInfo.cs", Path.Combine("src", "Project", "Properties", "VersionAssemblyInfo.cs") };
        
        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);
        using (new AssemblyInfoFileUpdate(new Arguments { EnsureAssemblyInfo = true, UpdateAssemblyInfo = true, UpdateAssemblyInfoFileName = assemblyInfoFile }, workingDir, variables, fileSystem))
        {
            foreach (var item in assemblyInfoFile)
            {
                var fullPath = Path.Combine(workingDir, item);
                var fileDescriminator = item.Replace(Path.DirectorySeparatorChar.ToString(), string.Empty).Replace(".", string.Empty);
                fileSystem.ReadAllText(fullPath).ShouldMatchApproved(c => c.WithDescriminator(fileDescriminator));
            }
        }
    }

    [Test]
    public void ShouldNotCreateCSharpAssemblyInfoFileWhenNotExistsAndNotEnsureAssemblyInfo()
    {
        var fileSystem = new TestFileSystem();
        var workingDir = Path.GetTempPath();
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
        var workingDir = Path.GetTempPath();
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
        var workingDir = Path.GetTempPath();
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
        var workingDir = Path.GetTempPath();
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
        var workingDir = Path.GetTempPath();
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
        var workingDir = Path.GetTempPath();
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
        var workingDir = Path.GetTempPath();

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

        var workingDir = Path.GetTempPath();
        string assemblyInfoFile = Join(@"AssemblyVersion(""1.0.0.0"");",
            @"AssemblyInformationalVersion(""1.0.0.0"");",
            @"AssemblyFileVersion(""1.0.0.0"");");

        var fileName = Path.Combine(workingDir, "AssemblyInfo.cs");
        fileSystem.Exists(fileName).Returns(true);
        fileSystem.ReadAllText(fileName).Returns(assemblyInfoFile);

        var config = new TestEffectiveConfiguration(assemblyVersioningScheme: AssemblyVersioningScheme.MajorMinor);

        var variable = VariableProvider.GetVariablesFor(version, config, false);
        var args = new Arguments
        {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = new HashSet<string> { "AssemblyInfo.cs" }
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            string expected = Join(@"AssemblyVersion(""2.3.0.0"");",
                @"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"");",
                @"AssemblyFileVersion(""2.3.1.0"");");
            fileSystem.Received().WriteAllText(fileName, expected);
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

        var workingDir = Path.GetTempPath();
        string assemblyInfoFile = Join(@"AssemblyVersion(""1.0.0.0"");",
            @"AssemblyInformationalVersion(""1.0.0.0"");",
            @"AssemblyFileVersion(""1.0.0.0"");");

        var fileName = Path.Combine(workingDir, "Project", "src", "Properties", "AssemblyInfo.cs");
        fileSystem.Exists(fileName).Returns(true);
        fileSystem.ReadAllText(fileName).Returns(assemblyInfoFile);

        var config = new TestEffectiveConfiguration(assemblyVersioningScheme: AssemblyVersioningScheme.MajorMinor);

        var variable = VariableProvider.GetVariablesFor(version, config, false);
        var args = new Arguments
        {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = new HashSet<string> { Path.Combine("Project", "src", "Properties", "AssemblyInfo.cs") }
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            string expected = Join(@"AssemblyVersion(""2.3.0.0"");",
                @"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"");",
                @"AssemblyFileVersion(""2.3.1.0"");");
            fileSystem.Received().WriteAllText(fileName, expected);
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

        var workingDir = Path.GetTempPath();
        string assemblyInfoFile = Join(@"AssemblyVersion(""1.0.0.*"");",
            @"AssemblyInformationalVersion(""1.0.0.*"");",
            @"AssemblyFileVersion(""1.0.0.*"");");

        var fileName = Path.Combine(workingDir, "AssemblyInfo.cs");
        fileSystem.Exists(fileName).Returns(true);
        fileSystem.ReadAllText(fileName).Returns(assemblyInfoFile);

        var config = new TestEffectiveConfiguration(assemblyVersioningScheme: AssemblyVersioningScheme.MajorMinor);
        var variable = VariableProvider.GetVariablesFor(version, config, false);
        var args = new Arguments
        {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = new HashSet<string> { "AssemblyInfo.cs" }
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            string expected = Join(@"AssemblyVersion(""2.3.0.0"");",
                @"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"");",
                @"AssemblyFileVersion(""2.3.1.0"");");
            fileSystem.Received().WriteAllText(fileName, expected);
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

        var workingDir = Path.GetTempPath();
        const string assemblyInfoFileContent = "";

        var fileName = Path.Combine(workingDir, "AssemblyInfo.cs");
        fileSystem.Exists(fileName).Returns(true);
        fileSystem.ReadAllText(fileName).Returns(assemblyInfoFileContent);

        var config = new TestEffectiveConfiguration(assemblyVersioningScheme: AssemblyVersioningScheme.MajorMinor);

        var variable = VariableProvider.GetVariablesFor(version, config, false);
        var args = new Arguments
        {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = new HashSet<string> { "AssemblyInfo.cs" }
        };

        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            string expected = Join("", @"[assembly: AssemblyVersion(""2.3.0.0"")]",
                @"[assembly: AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")]",
                @"[assembly: AssemblyFileVersion(""2.3.1.0"")]");
            fileSystem.Received().WriteAllText(fileName, expected);
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

        var workingDir = Path.GetTempPath();
        string assemblyInfoFile = Join(@"AssemblyVersion(""2.2.0.0"");",
            @"AssemblyInformationalVersion(""2.2.0+5.Branch.foo.Sha.hash"");",
            @"AssemblyFileVersion(""2.2.0.0"");");

        var fileName = Path.Combine(workingDir, "AssemblyInfo.cs");
        fileSystem.Exists(fileName).Returns(true);
        fileSystem.ReadAllText(fileName).Returns(assemblyInfoFile);

        var config = new TestEffectiveConfiguration(assemblyVersioningScheme: AssemblyVersioningScheme.MajorMinor);
        var variable = VariableProvider.GetVariablesFor(version, config, false);
        var args = new Arguments
        {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = new HashSet<string> { "AssemblyInfo.cs" }
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            string expected = Join(@"AssemblyVersion(""2.3.0.0"");",
                @"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"");",
                @"AssemblyFileVersion(""2.3.1.0"");");
            fileSystem.Received().WriteAllText(fileName, expected);
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

        var workingDir = Path.GetTempPath();
        string assemblyInfoFile = Join(@"AssemblyVersion(""1.0.0.0"");",
            @"AssemblyInformationalVersion(""1.0.0.0"");",
            @"AssemblyFileVersion(""1.0.0.0"");");

        var fileName = Path.Combine(workingDir, "AssemblyInfo.cs");
        fileSystem.WriteAllText(fileName, assemblyInfoFile);
        var variable = VariableProvider.GetVariablesFor(version, new TestEffectiveConfiguration(), false);
        var args = new Arguments
        {
            EnsureAssemblyInfo = true,
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = new HashSet<string> { "AssemblyInfo.cs" }
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            string expected = Join(@"AssemblyVersion(""2.3.1.0"");",
                @"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"");",
                @"AssemblyFileVersion(""2.3.1.0"");");
            fileSystem.ReadAllText(fileName).ShouldBe(expected);
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

        var workingDir = Path.GetTempPath();
        string assemblyInfoFile = Join(@"AssemblyVersion(""1.0.0.0"");",
            @"AssemblyInformationalVersion(""1.0.0.0"");",
            @"AssemblyFileVersion(""1.0.0.0"");");

        var fileName = Path.Combine(workingDir, "AssemblyInfo.fs");
        fileSystem.WriteAllText(fileName, assemblyInfoFile);
        var variable = VariableProvider.GetVariablesFor(version, new TestEffectiveConfiguration(), false);
        var args = new Arguments
        {
            EnsureAssemblyInfo = true,
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = new HashSet<string> { "AssemblyInfo.fs" }
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            string expected = Join(@"AssemblyVersion(""2.3.1.0"");",
                @"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"");",
                @"AssemblyFileVersion(""2.3.1.0"");");
            fileSystem.ReadAllText(fileName).ShouldBe(expected);
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

        var workingDir = Path.GetTempPath();
        string assemblyInfoFile = Join(@"AssemblyVersion(""1.0.0.0"");",
            @"AssemblyInformationalVersion(""1.0.0.0"");",
            @"AssemblyFileVersion(""1.0.0.0"");");

        var fileName = Path.Combine(workingDir, "AssemblyInfo.vb");
        fileSystem.WriteAllText(fileName, assemblyInfoFile);
        var variable = VariableProvider.GetVariablesFor(version, new TestEffectiveConfiguration(), false);
        var args = new Arguments
        {
            EnsureAssemblyInfo = true,
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = new HashSet<string> { "AssemblyInfo.vb" }
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            string expected = Join(@"AssemblyVersion(""2.3.1.0"");",
                @"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"");",
                @"AssemblyFileVersion(""2.3.1.0"");");
            fileSystem.ReadAllText(fileName).ShouldBe(expected);
        }
    }

    [Test]
    public void ShouldAddAssemblyInformationalVersionWhenUpdatingVisualBasicAssemblyVersionFile()
    {
       var fileSystem = new TestFileSystem();
        var version = new SemanticVersion
        {
            BuildMetaData = new SemanticVersionBuildMetaData(3, "foo", "hash", DateTimeOffset.Now),
            Major = 2,
            Minor = 3,
            Patch = 1
        };

        var workingDir = Path.GetTempPath();
        var assemblyInfoFile = Join(
                   "<Assembly: AssemblyVersion(\"1.0.0.0\")>",
                   "<Assembly: AssemblyFileVersion(\"1.0.0.0\")> ");

        var fileName = Path.Combine(workingDir, "AssemblyInfo.vb");
        fileSystem.WriteAllText(fileName, assemblyInfoFile);
        var variable = VariableProvider.GetVariablesFor(version, new TestEffectiveConfiguration(), false);

        var args = new Arguments
        {
            // EnsureAssemblyInfo = true,
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = new HashSet<string> { "AssemblyInfo.vb" }
        };

        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            string expected = Join(@"<AssemblyVersion(""2.3.1.0"")>",
                @"<AssemblyFileVersion(""2.3.1.0"")>",
                @"<AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")>");
            fileSystem.ReadAllText(fileName).ShouldBe(expected);
        }
    }


    private static string Join(params string[] lines)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            sb.Append(line);

            var lastLine = i == lines.Length - 1;
            if (!lastLine)
                sb.AppendLine();
        }

        return sb.ToString();
    }
}
