using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.Output.AssemblyInfo;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Core.Tests;

[TestFixture]
[Parallelizable(ParallelScope.None)]
public class AssemblyInfoFileUpdaterTests : TestBase
{
    private IVariableProvider variableProvider;
    private ILog log;
    private IFileSystem fileSystem;
    private string workingDir;

    [OneTimeSetUp]
    public void OneTimeSetUp() => workingDir = PathHelper.Combine(PathHelper.GetTempPath(), "AssemblyInfoFileUpdaterTests");

    [OneTimeTearDown]
    public void OneTimeTearDown() => DirectoryHelper.DeleteDirectory(workingDir);

    [SetUp]
    public void Setup()
    {
        ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestCaseAttribute>();

        var sp = ConfigureServices();

        this.log = sp.GetRequiredService<ILog>();
        this.fileSystem = sp.GetRequiredService<IFileSystem>();
        this.variableProvider = sp.GetRequiredService<IVariableProvider>();
    }

    [TestCase("cs")]
    [TestCase("fs")]
    [TestCase("vb")]
    public void ShouldCreateAssemblyInfoFileWhenNotExistsAndEnsureAssemblyInfo(string fileExtension)
    {
        var assemblyInfoFile = "VersionAssemblyInfo." + fileExtension;
        var fullPath = PathHelper.Combine(workingDir, assemblyInfoFile);

        var variables = this.variableProvider.GetVariablesFor(
            SemanticVersion.Parse("1.0.0", RegexPatterns.Configuration.DefaultTagPrefixPattern), EmptyConfigurationBuilder.New.Build(), 0);

        using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(this.log, this.fileSystem);
        assemblyInfoFileUpdater.Execute(variables, new(workingDir, true, assemblyInfoFile));

        this.fileSystem.ReadAllText(fullPath).ShouldMatchApproved(c => c.SubFolder(PathHelper.Combine("Approved", fileExtension)));
    }

    [TestCase("cs")]
    [TestCase("fs")]
    [TestCase("vb")]
    public void ShouldCreateAssemblyInfoFileAtPathWhenNotExistsAndEnsureAssemblyInfo(string fileExtension)
    {
        var assemblyInfoFile = PathHelper.Combine("src", "Project", "Properties", $"VersionAssemblyInfo.{fileExtension}");
        var fullPath = PathHelper.Combine(workingDir, assemblyInfoFile);
        var variables = this.variableProvider.GetVariablesFor(
            SemanticVersion.Parse("1.0.0", RegexPatterns.Configuration.DefaultTagPrefixPattern), EmptyConfigurationBuilder.New.Build(), 0
        );

        using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(this.log, this.fileSystem);
        assemblyInfoFileUpdater.Execute(variables, new(workingDir, true, assemblyInfoFile));

        this.fileSystem.ReadAllText(fullPath).ShouldMatchApproved(c => c.SubFolder(PathHelper.Combine("Approved", fileExtension)));
    }

    [TestCase("cs")]
    [TestCase("fs")]
    [TestCase("vb")]
    public void ShouldCreateAssemblyInfoFilesAtPathWhenNotExistsAndEnsureAssemblyInfo(string fileExtension)
    {
        var assemblyInfoFiles = new HashSet<string> { "AssemblyInfo." + fileExtension, PathHelper.Combine("src", "Project", "Properties", "VersionAssemblyInfo." + fileExtension) };
        var variables = this.variableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", RegexPatterns.Configuration.DefaultTagPrefixPattern), EmptyConfigurationBuilder.New.Build(), 0);

        using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(this.log, this.fileSystem);
        assemblyInfoFileUpdater.Execute(variables, new(workingDir, true, [.. assemblyInfoFiles]));

        foreach (var item in assemblyInfoFiles)
        {
            var fullPath = PathHelper.Combine(workingDir, item);
            this.fileSystem.ReadAllText(fullPath).ShouldMatchApproved(c => c.SubFolder(PathHelper.Combine("Approved", fileExtension)));
        }
    }

    [TestCase("cs")]
    [TestCase("fs")]
    [TestCase("vb")]
    public void ShouldNotCreateAssemblyInfoFileWhenNotExistsAndNotEnsureAssemblyInfo(string fileExtension)
    {
        var assemblyInfoFile = "VersionAssemblyInfo." + fileExtension;
        var fullPath = PathHelper.Combine(workingDir, assemblyInfoFile);
        var variables = this.variableProvider.GetVariablesFor(
            SemanticVersion.Parse("1.0.0", RegexPatterns.Configuration.DefaultTagPrefixPattern), EmptyConfigurationBuilder.New.Build(), 0
        );

        using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(this.log, this.fileSystem);
        assemblyInfoFileUpdater.Execute(variables, new(workingDir, false, assemblyInfoFile));

        this.fileSystem.Exists(fullPath).ShouldBeFalse();
    }

    [Test]
    public void ShouldNotCreateAssemblyInfoFileForUnknownSourceCodeAndEnsureAssemblyInfo()
    {
        this.fileSystem = Substitute.For<IFileSystem>();

        const string assemblyInfoFile = "VersionAssemblyInfo.js";
        var fullPath = PathHelper.Combine(workingDir, assemblyInfoFile);
        var variables = this.variableProvider.GetVariablesFor(
            SemanticVersion.Parse("1.0.0", RegexPatterns.Configuration.DefaultTagPrefixPattern), EmptyConfigurationBuilder.New.Build(), 0
        );

        using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(this.log, this.fileSystem);
        assemblyInfoFileUpdater.Execute(variables, new(workingDir, true, assemblyInfoFile));

        this.fileSystem.Received(0).WriteAllText(fullPath, Arg.Any<string>());
    }

    [Test]
    public void ShouldStartSearchFromWorkingDirectory()
    {
        this.fileSystem = Substitute.For<IFileSystem>();

        string[] assemblyInfoFiles = [];
        var variables = this.variableProvider.GetVariablesFor(
            SemanticVersion.Parse("1.0.0", RegexPatterns.Configuration.DefaultTagPrefixPattern), EmptyConfigurationBuilder.New.Build(), 0
        );

        using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(this.log, this.fileSystem);
        assemblyInfoFileUpdater.Execute(variables, new(workingDir, false, [.. assemblyInfoFiles]));

        this.fileSystem.Received().DirectoryEnumerateFiles(Arg.Is(workingDir), Arg.Any<string>(), Arg.Any<SearchOption>());
    }

    [TestCase("cs", "[assembly: AssemblyVersion(\"1.0.0.0\")]\r\n[assembly: AssemblyInformationalVersion(\"1.0.0.0\")]\r\n[assembly: AssemblyFileVersion(\"1.0.0.0\")]")]
    [TestCase("fs", "[<assembly: AssemblyVersion(\"1.0.0.0\")>]\r\n[<assembly: AssemblyInformationalVersion(\"1.0.0.0\")>]\r\n[<assembly: AssemblyFileVersion(\"1.0.0.0\")>]")]
    [TestCase("vb", "<Assembly: AssemblyVersion(\"1.0.0.0\")>\r\n<Assembly: AssemblyInformationalVersion(\"1.0.0.0\")>\r\n<Assembly: AssemblyFileVersion(\"1.0.0.0\")>")]
    public void ShouldReplaceAssemblyVersion(string fileExtension, string assemblyFileContent)
    {
        var assemblyInfoFile = "AssemblyInfo." + fileExtension;
        var fileName = PathHelper.Combine(workingDir, assemblyInfoFile);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, AssemblyVersioningScheme.MajorMinor, (fs, variables) =>
        {
            using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(this.log, fs);
            assemblyInfoFileUpdater.Execute(variables, new(workingDir, false, assemblyInfoFile));

            fs.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                s.Contains(@"AssemblyVersion(""2.3.0.0"")") &&
                s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
        });
    }

    [TestCase("cs", "[assembly: AssemblyFileVersion(\"1.0.0.0\")]")]
    [TestCase("fs", "[<assembly: AssemblyFileVersion(\"1.0.0.0\")>]")]
    [TestCase("vb", "<Assembly: AssemblyFileVersion(\"1.0.0.0\")>")]
    public void ShouldNotReplaceAssemblyVersionWhenVersionSchemeIsNone(string fileExtension, string assemblyFileContent)
    {
        var assemblyInfoFile = "AssemblyInfo." + fileExtension;
        var fileName = PathHelper.Combine(workingDir, assemblyInfoFile);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, AssemblyVersioningScheme.None, (fs, variables) =>
        {
            using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(this.log, fs);
            assemblyInfoFileUpdater.Execute(variables, new(workingDir, false, assemblyInfoFile));

            assemblyFileContent = fs.ReadAllText(fileName);
            assemblyFileContent.ShouldMatchApproved(c => c.SubFolder(PathHelper.Combine("Approved", fileExtension)));
        });
    }

    [TestCase("cs", "[assembly: AssemblyVersion(\"1.0.0.0\")]\r\n[assembly: AssemblyInformationalVersion(\"1.0.0.0\")]\r\n[assembly: AssemblyFileVersion(\"1.0.0.0\")]")]
    [TestCase("fs", "[<assembly: AssemblyVersion(\"1.0.0.0\")>]\r\n[<assembly: AssemblyInformationalVersion(\"1.0.0.0\")>]\r\n[<assembly: AssemblyFileVersion(\"1.0.0.0\")>]")]
    [TestCase("vb", "<Assembly: AssemblyVersion(\"1.0.0.0\")>\r\n<Assembly: AssemblyInformationalVersion(\"1.0.0.0\")>\r\n<Assembly: AssemblyFileVersion(\"1.0.0.0\")>")]
    public void ShouldReplaceAssemblyVersionInRelativePath(string fileExtension, string assemblyFileContent)
    {
        var assemblyInfoFile = PathHelper.Combine("Project", "src", "Properties", "AssemblyInfo." + fileExtension);
        var fileName = PathHelper.Combine(workingDir, assemblyInfoFile);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, AssemblyVersioningScheme.MajorMinor, (fs, variables) =>
        {
            using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(this.log, fs);
            assemblyInfoFileUpdater.Execute(variables, new(workingDir, false, assemblyInfoFile));

            fs.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                s.Contains(@"AssemblyVersion(""2.3.0.0"")") &&
                s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
        });
    }

    [TestCase("cs", "[assembly: AssemblyVersion ( \"1.0.0.0\") ]\r\n[assembly: AssemblyInformationalVersion\t(\t\"1.0.0.0\"\t)]\r\n[assembly: AssemblyFileVersion\r\n(\r\n\"1.0.0.0\"\r\n)]")]
    [TestCase("fs", "[<assembly: AssemblyVersion ( \"1.0.0.0\" )>]\r\n[<assembly: AssemblyInformationalVersion\t(\t\"1.0.0.0\"\t)>]\r\n[<assembly: AssemblyFileVersion\r\n(\r\n\"1.0.0.0\"\r\n)>]")]
    [TestCase("vb", "<Assembly: AssemblyVersion ( \"1.0.0.0\" )>\r\n<Assembly: AssemblyInformationalVersion\t(\t\"1.0.0.0\"\t)>\r\n<Assembly: AssemblyFileVersion\r\n(\r\n\"1.0.0.0\"\r\n)>")]
    public void ShouldReplaceAssemblyVersionInRelativePathWithWhiteSpace(string fileExtension, string assemblyFileContent)
    {
        var assemblyInfoFile = PathHelper.Combine("Project", "src", "Properties", "AssemblyInfo." + fileExtension);
        var fileName = PathHelper.Combine(workingDir, assemblyInfoFile);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, AssemblyVersioningScheme.MajorMinor, (fs, variables) =>
        {
            using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(this.log, fs);
            assemblyInfoFileUpdater.Execute(variables, new(workingDir, false, assemblyInfoFile));

            fs.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                s.Contains(@"AssemblyVersion(""2.3.0.0"")") &&
                s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
        });
    }

    [TestCase("cs", "[assembly: AssemblyVersion(\"1.0.0.*\")]\r\n[assembly: AssemblyInformationalVersion(\"1.0.0.*\")]\r\n[assembly: AssemblyFileVersion(\"1.0.0.*\")]")]
    [TestCase("fs", "[<assembly: AssemblyVersion(\"1.0.0.*\")>]\r\n[<assembly: AssemblyInformationalVersion(\"1.0.0.*\")>]\r\n[<assembly: AssemblyFileVersion(\"1.0.0.*\")>]")]
    [TestCase("vb", "<Assembly: AssemblyVersion(\"1.0.0.*\")>\r\n<Assembly: AssemblyInformationalVersion(\"1.0.0.*\")>\r\n<Assembly: AssemblyFileVersion(\"1.0.0.*\")>")]
    public void ShouldReplaceAssemblyVersionWithStar(string fileExtension, string assemblyFileContent)
    {
        var assemblyInfoFile = "AssemblyInfo." + fileExtension;
        var fileName = PathHelper.Combine(workingDir, assemblyInfoFile);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, AssemblyVersioningScheme.MajorMinor, (fs, variables) =>
        {
            using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(this.log, fs);
            assemblyInfoFileUpdater.Execute(variables, new(workingDir, false, assemblyInfoFile));

            fs.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                s.Contains(@"AssemblyVersion(""2.3.0.0"")") &&
                s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
        });
    }

    [TestCase("cs", "[assembly: AssemblyVersionAttribute(\"1.0.0.0\")]\r\n[assembly: AssemblyInformationalVersionAttribute(\"1.0.0.0\")]\r\n[assembly: AssemblyFileVersionAttribute(\"1.0.0.0\")]")]
    [TestCase("fs", "[<assembly: AssemblyVersionAttribute(\"1.0.0.0\")>]\r\n[<assembly: AssemblyInformationalVersionAttribute(\"1.0.0.0\")>]\r\n[<assembly: AssemblyFileVersionAttribute(\"1.0.0.0\")>]")]
    [TestCase("vb", "<Assembly: AssemblyVersionAttribute(\"1.0.0.0\")>\r\n<Assembly: AssemblyInformationalVersionAttribute(\"1.0.0.0\")>\r\n<Assembly: AssemblyFileVersionAttribute(\"1.0.0.0\")>")]
    public void ShouldReplaceAssemblyVersionWithAttributeSuffix(string fileExtension, string assemblyFileContent)
    {
        var assemblyInfoFile = "AssemblyInfo." + fileExtension;
        var fileName = PathHelper.Combine(workingDir, assemblyInfoFile);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, verify: (fs, variables) =>
        {
            using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(this.log, fs);
            assemblyInfoFileUpdater.Execute(variables, new(workingDir, false, assemblyInfoFile));

            fs.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                !s.Contains(@"AssemblyVersionAttribute(""1.0.0.0"")") &&
                !s.Contains(@"AssemblyInformationalVersionAttribute(""1.0.0.0"")") &&
                !s.Contains(@"AssemblyFileVersionAttribute(""1.0.0.0"")") &&
                s.Contains(@"AssemblyVersion(""2.3.1.0"")") &&
                s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
        });
    }

    [TestCase("cs")]
    [TestCase("fs")]
    [TestCase("vb")]
    public void ShouldAddAssemblyVersionIfMissingFromInfoFile(string fileExtension)
    {
        var assemblyInfoFile = "AssemblyInfo." + fileExtension;
        var fileName = PathHelper.Combine(workingDir, assemblyInfoFile);

        VerifyAssemblyInfoFile("", fileName, AssemblyVersioningScheme.MajorMinor, (fs, variables) =>
        {
            using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(this.log, fs);
            assemblyInfoFileUpdater.Execute(variables, new(workingDir, false, assemblyInfoFile));

            fs.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                s.Contains(@"AssemblyVersion(""2.3.0.0"")") &&
                s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
        });
    }

    [TestCase("cs", "[assembly: AssemblyVersion(\"2.2.0.0\")]\r\n[assembly: AssemblyInformationalVersion(\"2.2.0+5.Branch.foo.Sha.hash\")]\r\n[assembly: AssemblyFileVersion(\"2.2.0.0\")]")]
    [TestCase("fs", "[<assembly: AssemblyVersion(\"2.2.0.0\")>]\r\n[<assembly: AssemblyInformationalVersion(\"2.2.0+5.Branch.foo.Sha.hash\")>]\r\n[<assembly: AssemblyFileVersion(\"2.2.0.0\")>]")]
    [TestCase("vb", "<Assembly: AssemblyVersion(\"2.2.0.0\")>\r\n<Assembly: AssemblyInformationalVersion(\"2.2.0+5.Branch.foo.Sha.hash\")>\r\n<Assembly: AssemblyFileVersion(\"2.2.0.0\")>")]
    public void ShouldReplaceAlreadySubstitutedValues(string fileExtension, string assemblyFileContent)
    {
        var assemblyInfoFile = "AssemblyInfo." + fileExtension;
        var fileName = PathHelper.Combine(workingDir, assemblyInfoFile);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, AssemblyVersioningScheme.MajorMinor, (fs, variables) =>
        {
            using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(this.log, fs);
            assemblyInfoFileUpdater.Execute(variables, new(workingDir, false, assemblyInfoFile));

            fs.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                s.Contains(@"AssemblyVersion(""2.3.0.0"")") &&
                s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
        });
    }

    [TestCase("cs", "[assembly: AssemblyVersion(\"1.0.0.0\")]\r\n[assembly: AssemblyInformationalVersion(\"1.0.0.0\")]\r\n[assembly: AssemblyFileVersion(\"1.0.0.0\")]")]
    [TestCase("fs", "[<assembly: AssemblyVersion(\"1.0.0.0\")>]\r\n[<assembly: AssemblyInformationalVersion(\"1.0.0.0\")>]\r\n[<assembly: AssemblyFileVersion(\"1.0.0.0\")>]")]
    [TestCase("vb", "<Assembly: AssemblyVersion(\"1.0.0.0\")>\r\n<Assembly: AssemblyInformationalVersion(\"1.0.0.0\")>\r\n<Assembly: AssemblyFileVersion(\"1.0.0.0\")>")]
    public void ShouldReplaceAssemblyVersionWhenCreatingAssemblyVersionFileAndEnsureAssemblyInfo(string fileExtension, string assemblyFileContent)
    {
        var assemblyInfoFile = "AssemblyInfo." + fileExtension;
        var fileName = PathHelper.Combine(workingDir, assemblyInfoFile);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, verify: (fs, variables) =>
        {
            using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(this.log, fs);
            assemblyInfoFileUpdater.Execute(variables, new(workingDir, false, assemblyInfoFile));

            fs.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                s.Contains(@"AssemblyVersion(""2.3.1.0"")") &&
                s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
        });
    }

    [TestCase("cs", "[assembly: AssemblyVersion (AssemblyInfo.Version) ]\r\n[assembly: AssemblyInformationalVersion(AssemblyInfo.InformationalVersion)]\r\n[assembly: AssemblyFileVersion(AssemblyInfo.FileVersion)]")]
    [TestCase("fs", "[<assembly: AssemblyVersion (AssemblyInfo.Version)>]\r\n[<assembly: AssemblyInformationalVersion(AssemblyInfo.InformationalVersion)>]\r\n[<assembly: AssemblyFileVersion(AssemblyInfo.FileVersion)>]")]
    [TestCase("vb", "<Assembly: AssemblyVersion (AssemblyInfo.Version)>\r\n<Assembly: AssemblyInformationalVersion(AssemblyInfo.InformationalVersion)>\r\n<Assembly: AssemblyFileVersion(AssemblyInfo.FileVersion)>")]
    public void ShouldReplaceAssemblyVersionInRelativePathWithVariables(string fileExtension, string assemblyFileContent)
    {
        var assemblyInfoFile = PathHelper.Combine("Project", "src", "Properties", "AssemblyInfo." + fileExtension);
        var fileName = PathHelper.Combine(workingDir, assemblyInfoFile);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, AssemblyVersioningScheme.MajorMinor, (fs, variables) =>
        {
            using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(this.log, fs);
            assemblyInfoFileUpdater.Execute(variables, new(workingDir, false, assemblyInfoFile));

            fs.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                s.Contains(@"AssemblyVersion(""2.3.0.0"")") &&
                s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
        });
    }

    [TestCase("cs", "[assembly: AssemblyVersion (  AssemblyInfo.VersionInfo  ) ]\r\n[assembly: AssemblyInformationalVersion\t(\tAssemblyInfo.InformationalVersion\t)]\r\n[assembly: AssemblyFileVersion\r\n(\r\nAssemblyInfo.FileVersion\r\n)]")]
    [TestCase("fs", "[<assembly: AssemblyVersion ( AssemblyInfo.VersionInfo )>]\r\n[<assembly: AssemblyInformationalVersion\t(\tAssemblyInfo.InformationalVersion\t)>]\r\n[<assembly: AssemblyFileVersion\r\n(\r\nAssemblyInfo.FileVersion\r\n)>]")]
    [TestCase("vb", "<Assembly: AssemblyVersion ( AssemblyInfo.VersionInfo )>\r\n<Assembly: AssemblyInformationalVersion\t(\tAssemblyInfo.InformationalVersion\t)>\r\n<Assembly: AssemblyFileVersion\r\n(\r\nAssemblyInfo.FileVersion\r\n)>")]
    public void ShouldReplaceAssemblyVersionInRelativePathWithVariablesAndWhiteSpace(string fileExtension, string assemblyFileContent)
    {
        var assemblyInfoFile = PathHelper.Combine("Project", "src", "Properties", "AssemblyInfo." + fileExtension);
        var fileName = PathHelper.Combine(workingDir, assemblyInfoFile);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, AssemblyVersioningScheme.MajorMinor, (fs, variables) =>
        {
            using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(this.log, fs);
            assemblyInfoFileUpdater.Execute(variables, new(workingDir, false, assemblyInfoFile));

            fs.Received().WriteAllText(fileName, Arg.Is<string>(s =>
                s.Contains(@"AssemblyVersion(""2.3.0.0"")") &&
                s.Contains(@"AssemblyInformationalVersion(""2.3.1+3.Branch.foo.Sha.hash"")") &&
                s.Contains(@"AssemblyFileVersion(""2.3.1.0"")")));
        });
    }

    [TestCase("cs", "[assembly: AssemblyVersion(\"1.0.0.0\")]\r\n[assembly: AssemblyFileVersion(\"1.0.0.0\")]")]
    [TestCase("fs", "[<assembly: AssemblyVersion(\"1.0.0.0\")>]\r\n[<assembly: AssemblyFileVersion(\"1.0.0.0\")>]")]
    [TestCase("vb", "<Assembly: AssemblyVersion(\"1.0.0.0\")>\r\n<Assembly: AssemblyFileVersion(\"1.0.0.0\")>")]
    public void ShouldAddAssemblyInformationalVersionWhenUpdatingAssemblyVersionFile(string fileExtension, string assemblyFileContent)
    {
        var assemblyInfoFile = "AssemblyInfo." + fileExtension;
        var fileName = PathHelper.Combine(workingDir, assemblyInfoFile);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, verify: (fs, variables) =>
        {
            using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(this.log, fs);
            assemblyInfoFileUpdater.Execute(variables, new(workingDir, false, assemblyInfoFile));

            assemblyFileContent = fs.ReadAllText(fileName);
            assemblyFileContent.ShouldMatchApproved(c => c.SubFolder(PathHelper.Combine("Approved", fileExtension)));
        });
    }

    [TestCase("cs", "[assembly: AssemblyVersion(\"1.0.0.0\")]\r\n[assembly: AssemblyFileVersion(\"1.0.0.0\")]\r\n// comment\r\n")]
    [TestCase("fs", "[<assembly: AssemblyVersion(\"1.0.0.0\")>]\r\n[<assembly: AssemblyFileVersion(\"1.0.0.0\")>]\r\ndo\r\n()\r\n")]
    [TestCase("vb", "<Assembly: AssemblyVersion(\"1.0.0.0\")>\r\n<Assembly: AssemblyFileVersion(\"1.0.0.0\")>\r\n' comment\r\n")]
    public void Issue1183ShouldAddFSharpAssemblyInformationalVersionBesideOtherAttributes(string fileExtension, string assemblyFileContent)
    {
        var assemblyInfoFile = "AssemblyInfo." + fileExtension;
        var fileName = PathHelper.Combine(workingDir, assemblyInfoFile);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, verify: (fs, variables) =>
        {
            using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(this.log, fs);
            assemblyInfoFileUpdater.Execute(variables, new(workingDir, false, assemblyInfoFile));

            assemblyFileContent = fs.ReadAllText(fileName);
            assemblyFileContent.ShouldMatchApproved(c => c.SubFolder(PathHelper.Combine("Approved", fileExtension)));
        });
    }

    [TestCase("cs", "[assembly: AssemblyFileVersion(\"1.0.0.0\")]")]
    [TestCase("fs", "[<assembly: AssemblyFileVersion(\"1.0.0.0\")>]")]
    [TestCase("vb", "<Assembly: AssemblyFileVersion(\"1.0.0.0\")>")]
    public void ShouldNotAddAssemblyInformationalVersionWhenVersionSchemeIsNone(string fileExtension, string assemblyFileContent)
    {
        var assemblyInfoFile = "AssemblyInfo." + fileExtension;
        var fileName = PathHelper.Combine(workingDir, assemblyInfoFile);

        VerifyAssemblyInfoFile(assemblyFileContent, fileName, AssemblyVersioningScheme.None, (fs, variables) =>
        {
            using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(this.log, fs);
            assemblyInfoFileUpdater.Execute(variables, new(workingDir, false, assemblyInfoFile));

            assemblyFileContent = fs.ReadAllText(fileName);
            assemblyFileContent.ShouldMatchApproved(c => c.SubFolder(PathHelper.Combine("Approved", fileExtension)));
        });
    }

    private void VerifyAssemblyInfoFile(
        string assemblyFileContent,
        string fileName,
        AssemblyVersioningScheme versioningScheme = AssemblyVersioningScheme.MajorMinorPatch,
        Action<IFileSystem, GitVersionVariables>? verify = null)
    {
        this.fileSystem = Substitute.For<IFileSystem>();
        var version = new SemanticVersion { BuildMetaData = new("versionSourceHash", 3, "foo", "hash", "shortHash", DateTimeOffset.Now, 0), Major = 2, Minor = 3, Patch = 1 };

        this.fileSystem.Exists(fileName).Returns(true);
        this.fileSystem.ReadAllText(fileName).Returns(assemblyFileContent);
        this.fileSystem.When(f => f.WriteAllText(fileName, Arg.Any<string>())).Do(c =>
        {
            assemblyFileContent = c.ArgAt<string>(1);
            this.fileSystem.ReadAllText(fileName).Returns(assemblyFileContent);
        });

        var configuration = EmptyConfigurationBuilder.New.WithAssemblyVersioningScheme(versioningScheme).Build();
        var variables = this.variableProvider.GetVariablesFor(version, configuration, 0);

        verify?.Invoke(this.fileSystem, variables);
    }
}
