using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitVersion;
using GitVersion.Helpers;
using GitVersionCore.Tests;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class AssemblyInfoFileUpdateTests
{
    [SetUp]
    public void SetLoggers()
    {
        ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestCaseAttribute>();
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
        var assemblyInfoFile = new HashSet<string>
        {
            "VersionAssemblyInfo." + fileExtension
        };
        var fullPath = Path.Combine(workingDir, assemblyInfoFile.First());
        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);
        var arguments = new Arguments
        {
            EnsureAssemblyInfo = true,
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = assemblyInfoFile
        };

        using (new AssemblyInfoFileUpdate(arguments, workingDir, variables, fileSystem))
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
        var assemblyInfoFile = new HashSet<string>
        {
            Path.Combine("src", "Project", "Properties", "VersionAssemblyInfo." + fileExtension)
        };
        var fullPath = Path.Combine(workingDir, assemblyInfoFile.First());
        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);
        var arguments = new Arguments
        {
            EnsureAssemblyInfo = true,
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = assemblyInfoFile
        };

        using (new AssemblyInfoFileUpdate(arguments, workingDir, variables, fileSystem))
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
        var assemblyInfoFile = new HashSet<string>
        {
            "AssemblyInfo." + fileExtension,
            Path.Combine("src", "Project", "Properties", "VersionAssemblyInfo." + fileExtension)
        };
        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);
        var arguments = new Arguments
        {
            EnsureAssemblyInfo = true,
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = assemblyInfoFile
        };

        using (new AssemblyInfoFileUpdate(arguments, workingDir, variables, fileSystem))
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
        var assemblyInfoFile = new HashSet<string>
        {
            "VersionAssemblyInfo." + fileExtension
        };
        var fullPath = Path.Combine(workingDir, assemblyInfoFile.First());
        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);
        var arguments = new Arguments
        {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = assemblyInfoFile
        };

        using (new AssemblyInfoFileUpdate(arguments, workingDir, variables, fileSystem))
        {
            fileSystem.Exists(fullPath).ShouldBeFalse();
        }
    }


    [Test]
    public void ShouldNotCreateAssemblyInfoFileForUnknownSourceCodeAndEnsureAssemblyInfo()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        var workingDir = Path.GetTempPath();
        var assemblyInfoFile = new HashSet<string>
        {
            "VersionAssemblyInfo.js"
        };
        var fullPath = Path.Combine(workingDir, assemblyInfoFile.First());
        var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);
        var arguments = new Arguments
        {
            EnsureAssemblyInfo = true,
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFileName = assemblyInfoFile
        };

        using (new AssemblyInfoFileUpdate(arguments, workingDir, variables, fileSystem))
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
        var arguments = new Arguments
        {
            UpdateAssemblyInfo = true
        };
        using (new AssemblyInfoFileUpdate(arguments, workingDir, variables, fileSystem))
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
        var workingDir = Path.GetTempPath();
        var fileName = Path.Combine(workingDir, "AssemblyInfo." + fileExtension);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, AssemblyVersioningScheme.MajorMinor, (fileSystem, variables) =>
        {
            var args = new Arguments
            {
                UpdateAssemblyInfo = true,
                UpdateAssemblyInfoFileName = new HashSet<string>
                {
                    "AssemblyInfo." + fileExtension
                }
            };
            using (new AssemblyInfoFileUpdate(args, workingDir, variables, fileSystem))
            {
                fileSystem.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                    s.Contains(@"AssemblyVersion(""2.3.0.0"")") &&
                    s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                    s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
            }
        });
    }


    [TestCase("cs", "[assembly: AssemblyFileVersion(\"1.0.0.0\")]")]
    [TestCase("fs", "[<assembly: AssemblyFileVersion(\"1.0.0.0\")>]")]
    [TestCase("vb", "<Assembly: AssemblyFileVersion(\"1.0.0.0\")>")]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ShouldNotReplaceAssemblyVersionWhenVersionSchemeIsNone(string fileExtension, string assemblyFileContent)
    {
        var workingDir = Path.GetTempPath();
        var fileName = Path.Combine(workingDir, "AssemblyInfo." + fileExtension);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, AssemblyVersioningScheme.None, verify: (fileSystem, variables) =>
        {
            var args = new Arguments
            {
                UpdateAssemblyInfo = true,
                UpdateAssemblyInfoFileName = new HashSet<string>
                {
                    "AssemblyInfo." + fileExtension
                }
            };

            using (new AssemblyInfoFileUpdate(args, workingDir, variables, fileSystem))
            {
                assemblyFileContent = fileSystem.ReadAllText(fileName);
                assemblyFileContent.ShouldMatchApproved(c => c.SubFolder(Path.Combine("Approved", fileExtension)));
            }
        });
    }

    [TestCase("cs", "[assembly: AssemblyVersion(\"1.0.0.0\")]\r\n[assembly: AssemblyInformationalVersion(\"1.0.0.0\")]\r\n[assembly: AssemblyFileVersion(\"1.0.0.0\")]")]
    [TestCase("fs", "[<assembly: AssemblyVersion(\"1.0.0.0\")>]\r\n[<assembly: AssemblyInformationalVersion(\"1.0.0.0\")>]\r\n[<assembly: AssemblyFileVersion(\"1.0.0.0\")>]")]
    [TestCase("vb", "<Assembly: AssemblyVersion(\"1.0.0.0\")>\r\n<Assembly: AssemblyInformationalVersion(\"1.0.0.0\")>\r\n<Assembly: AssemblyFileVersion(\"1.0.0.0\")>")]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ShouldReplaceAssemblyVersionInRelativePath(string fileExtension, string assemblyFileContent)
    {
        var workingDir = Path.GetTempPath();
        var fileName = Path.Combine(workingDir, "Project", "src", "Properties", "AssemblyInfo." + fileExtension);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, AssemblyVersioningScheme.MajorMinor, (fileSystem, variables) =>
        {
            var args = new Arguments
            {
                UpdateAssemblyInfo = true,
                UpdateAssemblyInfoFileName = new HashSet<string>
                {
                    Path.Combine("Project", "src", "Properties", "AssemblyInfo." + fileExtension)
                }
            };
            using (new AssemblyInfoFileUpdate(args, workingDir, variables, fileSystem))
            {
                fileSystem.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                    s.Contains(@"AssemblyVersion(""2.3.0.0"")") &&
                    s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                    s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
            }
        });
    }

    [TestCase("cs", "[assembly: AssemblyVersion ( \"1.0.0.0\") ]\r\n[assembly: AssemblyInformationalVersion\t(\t\"1.0.0.0\"\t)]\r\n[assembly: AssemblyFileVersion\r\n(\r\n\"1.0.0.0\"\r\n)]")]
    [TestCase("fs", "[<assembly: AssemblyVersion ( \"1.0.0.0\" )>]\r\n[<assembly: AssemblyInformationalVersion\t(\t\"1.0.0.0\"\t)>]\r\n[<assembly: AssemblyFileVersion\r\n(\r\n\"1.0.0.0\"\r\n)>]")]
    [TestCase("vb", "<Assembly: AssemblyVersion ( \"1.0.0.0\" )>\r\n<Assembly: AssemblyInformationalVersion\t(\t\"1.0.0.0\"\t)>\r\n<Assembly: AssemblyFileVersion\r\n(\r\n\"1.0.0.0\"\r\n)>")]
    public void ShouldReplaceAssemblyVersionInRelativePathWithWhiteSpace(string fileExtension, string assemblyFileContent)
    {
        var workingDir = Path.GetTempPath();
        var fileName = Path.Combine(workingDir, "Project", "src", "Properties", "AssemblyInfo." + fileExtension);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, AssemblyVersioningScheme.MajorMinor, (fileSystem, variables) =>
        {
            var args = new Arguments
            {
                UpdateAssemblyInfo = true,
                UpdateAssemblyInfoFileName = new HashSet<string>
                {
                    Path.Combine("Project", "src", "Properties", "AssemblyInfo." + fileExtension)
                }
            };
            using (new AssemblyInfoFileUpdate(args, workingDir, variables, fileSystem))
            {
                fileSystem.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                    s.Contains(@"AssemblyVersion(""2.3.0.0"")") &&
                    s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                    s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
            }
        });
    }

    [TestCase("cs", "[assembly: AssemblyVersion(\"1.0.0.*\")]\r\n[assembly: AssemblyInformationalVersion(\"1.0.0.*\")]\r\n[assembly: AssemblyFileVersion(\"1.0.0.*\")]")]
    [TestCase("fs", "[<assembly: AssemblyVersion(\"1.0.0.*\")>]\r\n[<assembly: AssemblyInformationalVersion(\"1.0.0.*\")>]\r\n[<assembly: AssemblyFileVersion(\"1.0.0.*\")>]")]
    [TestCase("vb", "<Assembly: AssemblyVersion(\"1.0.0.*\")>\r\n<Assembly: AssemblyInformationalVersion(\"1.0.0.*\")>\r\n<Assembly: AssemblyFileVersion(\"1.0.0.*\")>")]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ShouldReplaceAssemblyVersionWithStar(string fileExtension, string assemblyFileContent)
    {
        var workingDir = Path.GetTempPath();
        var fileName = Path.Combine(workingDir, "AssemblyInfo." + fileExtension);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, AssemblyVersioningScheme.MajorMinor, (fileSystem, variables) =>
        {
            var args = new Arguments
            {
                UpdateAssemblyInfo = true,
                UpdateAssemblyInfoFileName = new HashSet<string>
                {
                    "AssemblyInfo." + fileExtension
                }
            };
            using (new AssemblyInfoFileUpdate(args, workingDir, variables, fileSystem))
            {
                fileSystem.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                    s.Contains(@"AssemblyVersion(""2.3.0.0"")") &&
                    s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                    s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
            }
        });
    }

    [TestCase("cs")]
    [TestCase("fs")]
    [TestCase("vb")]
    public void ShouldAddAssemblyVersionIfMissingFromInfoFile(string fileExtension)
    {
        var workingDir = Path.GetTempPath();
        var fileName = Path.Combine(workingDir, "AssemblyInfo." + fileExtension);

        VerifyAssemblyInfoFile("", fileName, AssemblyVersioningScheme.MajorMinor, (fileSystem, variables) =>
        {
            var args = new Arguments
            {
                UpdateAssemblyInfo = true,
                UpdateAssemblyInfoFileName = new HashSet<string>
                {
                    "AssemblyInfo." + fileExtension
                }
            };
            using (new AssemblyInfoFileUpdate(args, workingDir, variables, fileSystem))
            {
                fileSystem.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                    s.Contains(@"AssemblyVersion(""2.3.0.0"")") &&
                    s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                    s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
            }
        });
    }

    [TestCase("cs", "[assembly: AssemblyVersion(\"2.2.0.0\")]\r\n[assembly: AssemblyInformationalVersion(\"2.2.0+5.Branch.foo.Sha.hash\")]\r\n[assembly: AssemblyFileVersion(\"2.2.0.0\")]")]
    [TestCase("fs", "[<assembly: AssemblyVersion(\"2.2.0.0\")>]\r\n[<assembly: AssemblyInformationalVersion(\"2.2.0+5.Branch.foo.Sha.hash\")>]\r\n[<assembly: AssemblyFileVersion(\"2.2.0.0\")>]")]
    [TestCase("vb", "<Assembly: AssemblyVersion(\"2.2.0.0\")>\r\n<Assembly: AssemblyInformationalVersion(\"2.2.0+5.Branch.foo.Sha.hash\")>\r\n<Assembly: AssemblyFileVersion(\"2.2.0.0\")>")]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ShouldReplaceAlreadySubstitutedValues(string fileExtension, string assemblyFileContent)
    {
        var workingDir = Path.GetTempPath();
        var fileName = Path.Combine(workingDir, "AssemblyInfo." + fileExtension);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, AssemblyVersioningScheme.MajorMinor, (fileSystem, variables) =>
        {
            var args = new Arguments
            {
                UpdateAssemblyInfo = true,
                UpdateAssemblyInfoFileName = new HashSet<string>
                {
                    "AssemblyInfo." + fileExtension
                }
            };
            using (new AssemblyInfoFileUpdate(args, workingDir, variables, fileSystem))
            {
                fileSystem.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                    s.Contains(@"AssemblyVersion(""2.3.0.0"")") &&
                    s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                    s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
            }
        });
    }


    [TestCase("cs", "[assembly: AssemblyVersion(\"1.0.0.0\")]\r\n[assembly: AssemblyInformationalVersion(\"1.0.0.0\")]\r\n[assembly: AssemblyFileVersion(\"1.0.0.0\")]")]
    [TestCase("fs", "[<assembly: AssemblyVersion(\"1.0.0.0\")>]\r\n[<assembly: AssemblyInformationalVersion(\"1.0.0.0\")>]\r\n[<assembly: AssemblyFileVersion(\"1.0.0.0\")>]")]
    [TestCase("vb", "<Assembly: AssemblyVersion(\"1.0.0.0\")>\r\n<Assembly: AssemblyInformationalVersion(\"1.0.0.0\")>\r\n<Assembly: AssemblyFileVersion(\"1.0.0.0\")>")]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ShouldReplaceAssemblyVersionWhenCreatingAssemblyVersionFileAndEnsureAssemblyInfo(string fileExtension, string assemblyFileContent)
    {
        var workingDir = Path.GetTempPath();
        var fileName = Path.Combine(workingDir, "AssemblyInfo." + fileExtension);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, verify: (fileSystem, variables) =>
        {
            var args = new Arguments
            {
                EnsureAssemblyInfo = true,
                UpdateAssemblyInfo = true,
                UpdateAssemblyInfoFileName = new HashSet<string>
                {
                    "AssemblyInfo." + fileExtension
                }
            };
            using (new AssemblyInfoFileUpdate(args, workingDir, variables, fileSystem))
            {
                fileSystem.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                    s.Contains(@"AssemblyVersion(""2.3.1.0"")") &&
                    s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                    s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
            }
        });
    }


    [TestCase("cs", "[assembly: AssemblyVersion(\"1.0.0.0\")]\r\n[assembly: AssemblyFileVersion(\"1.0.0.0\")]")]
    [TestCase("fs", "[<assembly: AssemblyVersion(\"1.0.0.0\")>]\r\n[<assembly: AssemblyFileVersion(\"1.0.0.0\")>]")]
    [TestCase("vb", "<Assembly: AssemblyVersion(\"1.0.0.0\")>\r\n<Assembly: AssemblyFileVersion(\"1.0.0.0\")>")]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ShouldAddAssemblyInformationalVersionWhenUpdatingAssemblyVersionFile(string fileExtension, string assemblyFileContent)
    {
        var workingDir = Path.GetTempPath();
        var fileName = Path.Combine(workingDir, "AssemblyInfo." + fileExtension);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, verify: (fileSystem, variables) =>
        {
            var args = new Arguments
            {
                UpdateAssemblyInfo = true,
                UpdateAssemblyInfoFileName = new HashSet<string>
                {
                    "AssemblyInfo." + fileExtension
                }
            };

            using (new AssemblyInfoFileUpdate(args, workingDir, variables, fileSystem))
            {
                assemblyFileContent = fileSystem.ReadAllText(fileName);
                assemblyFileContent.ShouldMatchApproved(c => c.SubFolder(Path.Combine("Approved", fileExtension)));
            }
        });
    }


    [TestCase("cs", "[assembly: AssemblyFileVersion(\"1.0.0.0\")]")]
    [TestCase("fs", "[<assembly: AssemblyFileVersion(\"1.0.0.0\")>]")]
    [TestCase("vb", "<Assembly: AssemblyFileVersion(\"1.0.0.0\")>")]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ShouldNotAddAssemblyInformationalVersionWhenUpdatingAssemblyVersionFileWhenVersionSchemeIsNone(string fileExtension, string assemblyFileContent)
    {
        var workingDir = Path.GetTempPath();
        var fileName = Path.Combine(workingDir, "AssemblyInfo." + fileExtension);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, AssemblyVersioningScheme.None, verify: (fileSystem, variables) =>
        {
            var args = new Arguments
            {
                UpdateAssemblyInfo = true,
                UpdateAssemblyInfoFileName = new HashSet<string>
                {
                    "AssemblyInfo." + fileExtension
                }
            };

            using (new AssemblyInfoFileUpdate(args, workingDir, variables, fileSystem))
            {
                assemblyFileContent = fileSystem.ReadAllText(fileName);
                assemblyFileContent.ShouldMatchApproved(c => c.SubFolder(Path.Combine("Approved", fileExtension)));
            }
        });
    }

    private static void VerifyAssemblyInfoFile(
        string assemblyFileContent,
        string fileName,
        AssemblyVersioningScheme versioningScheme = AssemblyVersioningScheme.MajorMinorPatch,
        Action<IFileSystem, VersionVariables> verify = null)
    {
        var fileSystem = Substitute.For<IFileSystem>();
        var version = new SemanticVersion
        {
            BuildMetaData = new SemanticVersionBuildMetaData(3, "foo", "hash", DateTimeOffset.Now),
            Major = 2,
            Minor = 3,
            Patch = 1
        };

        fileSystem.Exists(fileName).Returns(true);
        fileSystem.ReadAllText(fileName).Returns(assemblyFileContent);
        fileSystem.When(f => f.WriteAllText(fileName, Arg.Any<string>())).Do(c =>
        {
            assemblyFileContent = c.ArgAt<string>(1);
            fileSystem.ReadAllText(fileName).Returns(assemblyFileContent);
        });

        var config = new TestEffectiveConfiguration(assemblyVersioningScheme: versioningScheme);
        var variables = VariableProvider.GetVariablesFor(version, config, false);

        verify(fileSystem, variables);
    }
}