using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GitVersion;
using GitVersion.Helpers;
using GitVersionCore.Tests;
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
    
    [TestCase("cs")]
    [TestCase("fs")]
    [TestCase("vb")]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ShouldCreateAssemblyInfoFileWhenNotExistsAndEnsureAssemblyInfo(string fileExtension)
    {
        var fileSystem = new TestFileSystem();
        var workingDir = Path.GetTempPath();
        ISet<string> assemblyInfoFile = new HashSet<string> { "VersionAssemblyInfo." + fileExtension };
        var fullPath = Path.Combine(workingDir, assemblyInfoFile.First());

        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);
        using (new AssemblyInfoFileUpdate(new Arguments { EnsureAssemblyInfo = true, UpdateAssemblyInfo = true, UpdateAssemblyInfoFileName = assemblyInfoFile}, workingDir, variables, fileSystem))
        {
            fileSystem.ReadAllText(fullPath).ShouldMatchApproved(c => c.SubFolder(Path.Combine("Approved", fileExtension)));
        }
    }

    [TestCase("cs")]
    [TestCase("fs")]
    [TestCase("vb")]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ShouldCreateAssemblyInfoFileAtPathWhenNotExistsAndEnsureAssemblyInfo(string fileExtension)
    {
        var fileSystem = new TestFileSystem();
        var workingDir = Path.GetTempPath();
        ISet<string> assemblyInfoFile = new HashSet<string> { Path.Combine("src", "Project", "Properties", "VersionAssemblyInfo." + fileExtension) };
        var fullPath = Path.Combine(workingDir, assemblyInfoFile.First());

        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);
        using (new AssemblyInfoFileUpdate(new Arguments { EnsureAssemblyInfo = true, UpdateAssemblyInfo = true, UpdateAssemblyInfoFileName = assemblyInfoFile }, workingDir, variables, fileSystem))
        {
            fileSystem.ReadAllText(fullPath).ShouldMatchApproved(c => c.SubFolder(Path.Combine("Approved", fileExtension)));
        }
    }

    [TestCase("cs")]
    [TestCase("fs")]
    [TestCase("vb")]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ShouldCreateAssemblyInfoFilesAtPathWhenNotExistsAndEnsureAssemblyInfo(string fileExtension)
    {
        var fileSystem = new TestFileSystem();
        var workingDir = Path.GetTempPath();
        ISet<string> assemblyInfoFile = new HashSet<string>
        {
            "AssemblyInfo." + fileExtension,
            Path.Combine("src", "Project", "Properties", "VersionAssemblyInfo." + fileExtension)
        };
        
        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);
        using (new AssemblyInfoFileUpdate(new Arguments { EnsureAssemblyInfo = true, UpdateAssemblyInfo = true, UpdateAssemblyInfoFileName = assemblyInfoFile }, workingDir, variables, fileSystem))
        {
            foreach (var item in assemblyInfoFile)
            {
                var fullPath = Path.Combine(workingDir, item);
                fileSystem.ReadAllText(fullPath).ShouldMatchApproved(c => c.SubFolder(Path.Combine("Approved", fileExtension)));
            }
        }
    }

    [TestCase("cs")]
    [TestCase("fs")]
    [TestCase("vb")]
    public void ShouldNotCreateAssemblyInfoFileWhenNotExistsAndNotEnsureAssemblyInfo(string fileExtension)
    {
        var fileSystem = new TestFileSystem();
        var workingDir = Path.GetTempPath();
        ISet<string> assemblyInfoFile = new HashSet<string> { "VersionAssemblyInfo." + fileExtension };
        var fullPath = Path.Combine(workingDir, assemblyInfoFile.First());

        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);
        using (new AssemblyInfoFileUpdate(new Arguments { UpdateAssemblyInfo = true, UpdateAssemblyInfoFileName = assemblyInfoFile }, workingDir, variables, fileSystem))
        {
            fileSystem.Exists(fullPath).ShouldBeFalse();
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

    [TestCase("cs", "[assembly: AssemblyVersion(\"1.0.0.0\")]\r\n[assembly: AssemblyInformationalVersion(\"1.0.0.0\")]\r\n[assembly: AssemblyFileVersion(\"1.0.0.0\")]")]
    [TestCase("fs", "[<assembly: AssemblyVersion(\"1.0.0.0\")>]\r\n[<assembly: AssemblyInformationalVersion(\"1.0.0.0\")>]\r\n[<assembly: AssemblyFileVersion(\"1.0.0.0\")>]")]
    [TestCase("vb", "<Assembly: AssemblyVersion(\"1.0.0.0\")>\r\n<Assembly: AssemblyInformationalVersion(\"1.0.0.0\")>\r\n<Assembly: AssemblyFileVersion(\"1.0.0.0\")>")]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ShouldReplaceAssemblyVersion(string fileExtension, string assemblyFileContent)
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
        var fileName = Path.Combine(workingDir, "AssemblyInfo." + fileExtension);
        fileSystem.Exists(fileName).Returns(true);
        fileSystem.ReadAllText(fileName).Returns(assemblyFileContent);

        var config = new TestEffectiveConfiguration(assemblyVersioningScheme: AssemblyVersioningScheme.MajorMinor);

        var variable = VariableProvider.GetVariablesFor(version, config, false);
        var args = new Arguments
        {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = new HashSet<string> { "AssemblyInfo." + fileExtension }
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            fileSystem.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                s.Contains(@"AssemblyVersion(""2.3.0.0"")") &&
                s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
        }
    }

    [TestCase("cs", "[assembly: AssemblyVersion(\"1.0.0.0\")]\r\n[assembly: AssemblyInformationalVersion(\"1.0.0.0\")]\r\n[assembly: AssemblyFileVersion(\"1.0.0.0\")]")]
    [TestCase("fs", "[<assembly: AssemblyVersion(\"1.0.0.0\")>]\r\n[<assembly: AssemblyInformationalVersion(\"1.0.0.0\")>]\r\n[<assembly: AssemblyFileVersion(\"1.0.0.0\")>]")]
    [TestCase("vb", "<Assembly: AssemblyVersion(\"1.0.0.0\")>\r\n<Assembly: AssemblyInformationalVersion(\"1.0.0.0\")>\r\n<Assembly: AssemblyFileVersion(\"1.0.0.0\")>")]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ShouldReplaceAssemblyVersionInRelativePath(string fileExtension, string assemblyFileContent)
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
        var fileName = Path.Combine(workingDir, "Project", "src", "Properties", "AssemblyInfo." + fileExtension);
        fileSystem.Exists(fileName).Returns(true);
        fileSystem.ReadAllText(fileName).Returns(assemblyFileContent);

        var config = new TestEffectiveConfiguration(assemblyVersioningScheme: AssemblyVersioningScheme.MajorMinor);

        var variable = VariableProvider.GetVariablesFor(version, config, false);
        var args = new Arguments
        {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = new HashSet<string> { Path.Combine("Project", "src", "Properties", "AssemblyInfo." + fileExtension) }
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            fileSystem.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                s.Contains(@"AssemblyVersion(""2.3.0.0"")") &&
                s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
        }
    }

    [TestCase("cs", "[assembly: AssemblyVersion(\"1.0.0.*\")]\r\n[assembly: AssemblyInformationalVersion(\"1.0.0.*\")]\r\n[assembly: AssemblyFileVersion(\"1.0.0.*\")]")]
    [TestCase("fs", "[<assembly: AssemblyVersion(\"1.0.0.*\")>]\r\n[<assembly: AssemblyInformationalVersion(\"1.0.0.*\")>]\r\n[<assembly: AssemblyFileVersion(\"1.0.0.*\")>]")]
    [TestCase("vb", "<Assembly: AssemblyVersion(\"1.0.0.*\")>\r\n<Assembly: AssemblyInformationalVersion(\"1.0.0.*\")>\r\n<Assembly: AssemblyFileVersion(\"1.0.0.*\")>")]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ShouldReplaceAssemblyVersionWithStar(string fileExtension, string assemblyFileContent)
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
        var fileName = Path.Combine(workingDir, "AssemblyInfo." + fileExtension);
        fileSystem.Exists(fileName).Returns(true);
        fileSystem.ReadAllText(fileName).Returns(assemblyFileContent);

        var config = new TestEffectiveConfiguration(assemblyVersioningScheme: AssemblyVersioningScheme.MajorMinor);
        var variable = VariableProvider.GetVariablesFor(version, config, false);
        var args = new Arguments
        {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = new HashSet<string> { "AssemblyInfo." + fileExtension }
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            fileSystem.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                s.Contains(@"AssemblyVersion(""2.3.0.0"")") &&
                s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
        }
    }

    [TestCase("cs")]
    [TestCase("fs")]
    [TestCase("vb")]
    public void ShouldAddAssemblyVersionIfMissingFromInfoFile(string fileExtension)
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

        var fileName = Path.Combine(workingDir, "AssemblyInfo." + fileExtension);
        fileSystem.Exists(fileName).Returns(true);
        fileSystem.ReadAllText(fileName).Returns(assemblyInfoFileContent);

        var config = new TestEffectiveConfiguration(assemblyVersioningScheme: AssemblyVersioningScheme.MajorMinor);

        var variable = VariableProvider.GetVariablesFor(version, config, false);
        var args = new Arguments
        {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = new HashSet<string> { "AssemblyInfo." + fileExtension }
        };

        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            fileSystem.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                s.Contains(@"AssemblyVersion(""2.3.0.0"")") &&
                s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
        }
    }

    [TestCase("cs", "[assembly: AssemblyVersion(\"2.2.0.0\")]\r\n[assembly: AssemblyInformationalVersion(\"2.2.0+5.Branch.foo.Sha.hash\")]\r\n[assembly: AssemblyFileVersion(\"2.2.0.0\")]")]
    [TestCase("fs", "[<assembly: AssemblyVersion(\"2.2.0.0\")>]\r\n[<assembly: AssemblyInformationalVersion(\"2.2.0+5.Branch.foo.Sha.hash\")>]\r\n[<assembly: AssemblyFileVersion(\"2.2.0.0\")>]")]
    [TestCase("vb", "<Assembly: AssemblyVersion(\"2.2.0.0\")>\r\n<Assembly: AssemblyInformationalVersion(\"2.2.0+5.Branch.foo.Sha.hash\")>\r\n<Assembly: AssemblyFileVersion(\"2.2.0.0\")>")]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ShouldReplaceAlreadySubstitutedValues(string fileExtension, string assemblyFileContent)
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
        var fileName = Path.Combine(workingDir, "AssemblyInfo." + fileExtension);
        fileSystem.Exists(fileName).Returns(true);
        fileSystem.ReadAllText(fileName).Returns(assemblyFileContent);

        var config = new TestEffectiveConfiguration(assemblyVersioningScheme: AssemblyVersioningScheme.MajorMinor);
        var variable = VariableProvider.GetVariablesFor(version, config, false);
        var args = new Arguments
        {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = new HashSet<string> { "AssemblyInfo." + fileExtension }
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            fileSystem.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                s.Contains(@"AssemblyVersion(""2.3.0.0"")") &&
                s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
        }
    }


    [TestCase("cs", "[assembly: AssemblyVersion(\"1.0.0.0\")]\r\n[assembly: AssemblyInformationalVersion(\"1.0.0.0\")]\r\n[assembly: AssemblyFileVersion(\"1.0.0.0\")]")]
    [TestCase("fs", "[<assembly: AssemblyVersion(\"1.0.0.0\")>]\r\n[<assembly: AssemblyInformationalVersion(\"1.0.0.0\")>]\r\n[<assembly: AssemblyFileVersion(\"1.0.0.0\")>]")]
    [TestCase("vb", "<Assembly: AssemblyVersion(\"1.0.0.0\")>\r\n<Assembly: AssemblyInformationalVersion(\"1.0.0.0\")>\r\n<Assembly: AssemblyFileVersion(\"1.0.0.0\")>")]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ShouldReplaceAssemblyVersionWhenCreatingAssemblyVersionFileAndEnsureAssemblyInfo(string fileExtension, string assemblyFileContent)
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
        var fileName = Path.Combine(workingDir, "AssemblyInfo." + fileExtension);
        fileSystem.Exists(fileName).Returns(true);
        fileSystem.ReadAllText(fileName).Returns(assemblyFileContent);
        fileSystem.WriteAllText(fileName, assemblyFileContent);
        var variable = VariableProvider.GetVariablesFor(version, new TestEffectiveConfiguration(), false);
        var args = new Arguments
        {
            EnsureAssemblyInfo = true,
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = new HashSet<string> { "AssemblyInfo." + fileExtension }
        };
        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            fileSystem.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                s.Contains(@"AssemblyVersion(""2.3.1.0"")") &&
                s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
        }
    }
    

    [TestCase("cs", "[assembly: AssemblyVersion(\"1.0.0.0\")]\r\n[assembly: AssemblyFileVersion(\"1.0.0.0\")]")]
    [TestCase("fs", "[<assembly: AssemblyVersion(\"1.0.0.0\")>]\r\n[<assembly: AssemblyFileVersion(\"1.0.0.0\")>]")]
    [TestCase("vb", "<Assembly: AssemblyVersion(\"1.0.0.0\")>\r\n<Assembly: AssemblyFileVersion(\"1.0.0.0\")>")]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ShouldAddAssemblyInformationalVersionWhenUpdatingAssemblyVersionFile(string fileExtension, string assemblyFileContent)
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
        var fileName = Path.Combine(workingDir, "AssemblyInfo." + fileExtension);
        fileSystem.WriteAllText(fileName, assemblyFileContent);
        var variable = VariableProvider.GetVariablesFor(version, new TestEffectiveConfiguration(), false);

        var args = new Arguments
        {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = new HashSet<string> { "AssemblyInfo." + fileExtension }
        };

        using (new AssemblyInfoFileUpdate(args, workingDir, variable, fileSystem))
        {
            fileSystem.ReadAllText(fileName).ShouldMatchApproved(c => c.SubFolder(Path.Combine("Approved", fileExtension)));
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
