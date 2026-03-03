using System.IO.Abstractions;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.VersionCalculation;

namespace GitVersion.App.Tests;

[TestFixture]
public class ArgumentParserTests : TestBase
{
    private IEnvironment environment;
    private IArgumentParser argumentParser;
    private IFileSystem fileSystem;

    [SetUp]
    public void SetUp()
    {
        var sp = ConfigureServices(services => services.AddModule(new GitVersionAppModule()));
        this.environment = sp.GetRequiredService<IEnvironment>();
        this.argumentParser = sp.GetRequiredService<IArgumentParser>();
        this.fileSystem = sp.GetRequiredService<IFileSystem>();
    }

    [Test]
    public void EmptyMeansUseCurrentDirectory()
    {
        var arguments = this.argumentParser.ParseArguments("");
        arguments.TargetPath.ShouldBe(SysEnv.CurrentDirectory);
        arguments.LogFilePath.ShouldBe(null);
        arguments.IsHelp.ShouldBe(false);
        arguments.NoFetch.ShouldBe(false);
    }

    [Test]
    public void SingleMeansUseAsTargetDirectory()
    {
        var arguments = this.argumentParser.ParseArguments("path");
        arguments.TargetPath.ShouldBe("path");
        arguments.LogFilePath.ShouldBe(null);
        arguments.IsHelp.ShouldBe(false);
    }

    [Test]
    public void NoPathAndLogfileShouldUseCurrentDirectoryTargetDirectory()
    {
        var arguments = this.argumentParser.ParseArguments("-l logFilePath");
        arguments.TargetPath.ShouldBe(SysEnv.CurrentDirectory);
        arguments.LogFilePath.ShouldBe("logFilePath");
        arguments.IsHelp.ShouldBe(false);
    }

    [Test]
    public void NoPathAndLogfileLongFormShouldUseCurrentDirectoryTargetDirectory()
    {
        var arguments = this.argumentParser.ParseArguments("--log-file logFilePath");
        arguments.TargetPath.ShouldBe(SysEnv.CurrentDirectory);
        arguments.LogFilePath.ShouldBe("logFilePath");
        arguments.IsHelp.ShouldBe(false);
    }

    [Test]
    public void HelpSwitchTest()
    {
        var arguments = this.argumentParser.ParseArguments("-h");
        Assert.Multiple(() =>
        {
            Assert.That(arguments.TargetPath, Is.Null);
            Assert.That(arguments.LogFilePath, Is.Null);
        });
        arguments.IsHelp.ShouldBe(true);
    }

    [Test]
    public void VersionSwitchTest()
    {
        var arguments = this.argumentParser.ParseArguments("--version");
        Assert.Multiple(() =>
        {
            Assert.That(arguments.TargetPath, Is.Null);
            Assert.That(arguments.LogFilePath, Is.Null);
        });
        arguments.IsVersion.ShouldBe(true);
    }

    [Test]
    public void TargetDirectoryAndLogFilePathCanBeParsed()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath --log-file logFilePath");
        arguments.TargetPath.ShouldBe("targetDirectoryPath");
        arguments.LogFilePath.ShouldBe("logFilePath");
        arguments.IsHelp.ShouldBe(false);
    }

    [Test]
    public void UsernameAndPasswordCanBeParsed()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath -u [username] -p [password]");
        arguments.TargetPath.ShouldBe("targetDirectoryPath");
        arguments.Authentication.Username.ShouldBe("[username]");
        arguments.Authentication.Password.ShouldBe("[password]");
        arguments.IsHelp.ShouldBe(false);
    }

    [Test]
    public void UnknownOutputShouldThrow()
    {
        var exception = Assert.Throws<WarningException>(() => this.argumentParser.ParseArguments("targetDirectoryPath --output invalid_value"));
        exception.ShouldNotBeNull();
        exception.Message.ShouldContain("invalid_value");
    }

    [Test]
    public void OutputDefaultsToJson()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath");
        arguments.Output.ShouldContain(OutputType.Json);
        arguments.Output.ShouldNotContain(OutputType.BuildServer);
        arguments.Output.ShouldNotContain(OutputType.File);
    }

    [Test]
    public void OutputJsonCanBeParsed()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath --output json");
        arguments.Output.ShouldContain(OutputType.Json);
        arguments.Output.ShouldNotContain(OutputType.BuildServer);
        arguments.Output.ShouldNotContain(OutputType.File);
    }

    [Test]
    public void MultipleOutputJsonCanBeParsed()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath --output json --output json");
        arguments.Output.ShouldContain(OutputType.Json);
        arguments.Output.ShouldNotContain(OutputType.BuildServer);
        arguments.Output.ShouldNotContain(OutputType.File);
    }

    [Test]
    public void OutputBuildserverCanBeParsed()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath --output buildserver");
        arguments.Output.ShouldContain(OutputType.BuildServer);
        arguments.Output.ShouldNotContain(OutputType.Json);
        arguments.Output.ShouldNotContain(OutputType.File);
    }

    [Test]
    public void MultipleOutputBuildserverCanBeParsed()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath --output buildserver --output buildserver");
        arguments.Output.ShouldContain(OutputType.BuildServer);
        arguments.Output.ShouldNotContain(OutputType.Json);
        arguments.Output.ShouldNotContain(OutputType.File);
    }

    [Test]
    public void OutputFileCanBeParsed()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath --output file");
        arguments.Output.ShouldContain(OutputType.File);
        arguments.Output.ShouldNotContain(OutputType.BuildServer);
        arguments.Output.ShouldNotContain(OutputType.Json);
    }

    [Test]
    public void MultipleOutputFileCanBeParsed()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath --output file --output file");
        arguments.Output.ShouldContain(OutputType.File);
        arguments.Output.ShouldNotContain(OutputType.BuildServer);
        arguments.Output.ShouldNotContain(OutputType.Json);
    }

    [Test]
    public void OutputBuildserverAndJsonCanBeParsed()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath --output buildserver --output json");
        arguments.Output.ShouldContain(OutputType.BuildServer);
        arguments.Output.ShouldContain(OutputType.Json);
        arguments.Output.ShouldNotContain(OutputType.File);
    }

    [Test]
    public void OutputBuildserverAndJsonAndFileCanBeParsed()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath --output buildserver --output json --output file");
        arguments.Output.ShouldContain(OutputType.BuildServer);
        arguments.Output.ShouldContain(OutputType.Json);
        arguments.Output.ShouldContain(OutputType.File);
    }

    [Test]
    public void MultipleArgsAndFlag()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath --output buildserver --update-assembly-info");
        arguments.Output.ShouldContain(OutputType.BuildServer);
    }

    [TestCase("--output file", "GitVersion.json")]
    [TestCase("--output file --output-file version.json", "version.json")]
    public void OutputFileArgumentCanBeParsed(string args, string outputFile)
    {
        var arguments = this.argumentParser.ParseArguments(args);

        arguments.Output.ShouldContain(OutputType.File);
        arguments.OutputFile.ShouldBe(outputFile);
    }

    [Test]
    public void UrlAndBranchNameCanBeParsed()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath --url https://github.com/Particular/GitVersion.git --branch someBranch");
        arguments.TargetPath.ShouldBe("targetDirectoryPath");
        arguments.TargetUrl.ShouldBe("https://github.com/Particular/GitVersion.git");
        arguments.TargetBranch.ShouldBe("someBranch");
        arguments.IsHelp.ShouldBe(false);
    }

    [Test]
    public void WrongNumberOfArgumentsShouldThrow()
    {
        var exception = Assert.Throws<WarningException>(() => this.argumentParser.ParseArguments("targetDirectoryPath --log-file logFilePath extraArg"));
        exception.ShouldNotBeNull();
        exception.Message.ShouldBe("Could not parse command line parameter 'extraArg'.");
    }

    [TestCase("targetDirectoryPath -x logFilePath")]
    [TestCase("--invalid-argument")]
    public void UnknownArgumentsShouldThrow(string arguments)
    {
        var exception = Assert.Throws<WarningException>(() => this.argumentParser.ParseArguments(arguments));
        exception.ShouldNotBeNull();
        exception.Message.ShouldStartWith("Could not parse command line parameter");
    }

    [TestCase("--update-assembly-info true")]
    [TestCase("--update-assembly-info 1")]
    [TestCase("--update-assembly-info")]
    [TestCase("--update-assembly-info assemblyInfo.cs")]
    [TestCase("--update-assembly-info assemblyInfo.cs --ensure-assembly-info")]
    [TestCase("--update-assembly-info assemblyInfo.cs otherAssemblyInfo.cs")]
    [TestCase("--update-assembly-info Assembly.cs Assembly.cs --ensure-assembly-info")]
    public void UpdateAssemblyInfoTrue(string command)
    {
        var arguments = this.argumentParser.ParseArguments(command);
        arguments.UpdateAssemblyInfo.ShouldBe(true);
    }

    [TestCase("--update-project-files assemblyInfo.csproj")]
    [TestCase("--update-project-files assemblyInfo.csproj")]
    [TestCase("--update-project-files assemblyInfo.csproj otherAssemblyInfo.fsproj")]
    [TestCase("--update-project-files")]
    public void UpdateProjectTrue(string command)
    {
        var arguments = this.argumentParser.ParseArguments(command);
        arguments.UpdateProjectFiles.ShouldBe(true);
    }

    [TestCase("--update-assembly-info false")]
    [TestCase("--update-assembly-info 0")]
    public void UpdateAssemblyInfoFalse(string command)
    {
        var arguments = this.argumentParser.ParseArguments(command);
        arguments.UpdateAssemblyInfo.ShouldBe(false);
    }

    [TestCase("--update-assembly-info Assembly.cs Assembly1.cs --ensure-assembly-info")]
    public void CreateMultipleAssemblyInfoProtected(string command)
    {
        var exception = Assert.Throws<WarningException>(() => this.argumentParser.ParseArguments(command));
        exception.ShouldNotBeNull();
        exception.Message.ShouldBe("Can't specify multiple assembly info files when using --ensure-assembly-info, either use a single assembly info file or do not specify --ensure-assembly-info and create assembly info files manually");
    }

    [TestCase("--update-project-files Assembly.csproj --ensure-assembly-info")]
    public void UpdateProjectInfoWithEnsureAssemblyInfoProtected(string command)
    {
        var exception = Assert.Throws<WarningException>(() => this.argumentParser.ParseArguments(command));
        exception.ShouldNotBeNull();
        exception.Message.ShouldBe("Cannot specify --ensure-assembly-info with --update-project-files: please ensure your project file exists before attempting to update it");
    }

    [Test]
    public void UpdateAssemblyInfoWithFilename()
    {
        using var repo = new EmptyRepositoryFixture();

        var assemblyFile = FileSystemHelper.Path.Combine(repo.RepositoryPath, "CommonAssemblyInfo.cs");
        using var file = this.fileSystem.File.Create(assemblyFile);

        var arguments = this.argumentParser.ParseArguments($"--target-path {repo.RepositoryPath} --update-assembly-info CommonAssemblyInfo.cs");
        arguments.UpdateAssemblyInfo.ShouldBe(true);
        arguments.UpdateAssemblyInfoFileName.Count.ShouldBe(1);
        arguments.UpdateAssemblyInfoFileName.ShouldContain(x => FileSystemHelper.Path.GetFileName(x).Equals("CommonAssemblyInfo.cs"));
    }

    [Test]
    public void UpdateAssemblyInfoWithMultipleFilenames()
    {
        using var repo = new EmptyRepositoryFixture();

        var assemblyFile1 = FileSystemHelper.Path.Combine(repo.RepositoryPath, "CommonAssemblyInfo.cs");
        using var file = this.fileSystem.File.Create(assemblyFile1);

        var assemblyFile2 = FileSystemHelper.Path.Combine(repo.RepositoryPath, "VersionAssemblyInfo.cs");
        using var file2 = this.fileSystem.File.Create(assemblyFile2);

        var arguments = this.argumentParser.ParseArguments($"--target-path {repo.RepositoryPath} --update-assembly-info CommonAssemblyInfo.cs VersionAssemblyInfo.cs");
        arguments.UpdateAssemblyInfo.ShouldBe(true);
        arguments.UpdateAssemblyInfoFileName.Count.ShouldBe(2);
        arguments.UpdateAssemblyInfoFileName.ShouldContain(x => FileSystemHelper.Path.GetFileName(x).Equals("CommonAssemblyInfo.cs"));
        arguments.UpdateAssemblyInfoFileName.ShouldContain(x => FileSystemHelper.Path.GetFileName(x).Equals("VersionAssemblyInfo.cs"));
    }

    [Test]
    public void UpdateProjectFilesWithMultipleFilenames()
    {
        using var repo = new EmptyRepositoryFixture();

        var assemblyFile1 = FileSystemHelper.Path.Combine(repo.RepositoryPath, "CommonAssemblyInfo.csproj");
        using var file = this.fileSystem.File.Create(assemblyFile1);

        var assemblyFile2 = FileSystemHelper.Path.Combine(repo.RepositoryPath, "VersionAssemblyInfo.csproj");
        using var file2 = this.fileSystem.File.Create(assemblyFile2);

        var arguments = this.argumentParser.ParseArguments($"--target-path {repo.RepositoryPath} --update-project-files CommonAssemblyInfo.csproj VersionAssemblyInfo.csproj");
        arguments.UpdateProjectFiles.ShouldBe(true);
        arguments.UpdateAssemblyInfoFileName.Count.ShouldBe(2);
        arguments.UpdateAssemblyInfoFileName.ShouldContain(x => FileSystemHelper.Path.GetFileName(x).Equals("CommonAssemblyInfo.csproj"));
        arguments.UpdateAssemblyInfoFileName.ShouldContain(x => FileSystemHelper.Path.GetFileName(x).Equals("VersionAssemblyInfo.csproj"));
    }

    [Test]
    public void UpdateAssemblyInfoWithMultipleFilenamesMatchingGlobbing()
    {
        using var repo = new EmptyRepositoryFixture();

        var assemblyFile1 = FileSystemHelper.Path.Combine(repo.RepositoryPath, "CommonAssemblyInfo.cs");
        using var file = this.fileSystem.File.Create(assemblyFile1);

        var assemblyFile2 = FileSystemHelper.Path.Combine(repo.RepositoryPath, "VersionAssemblyInfo.cs");
        using var file2 = this.fileSystem.File.Create(assemblyFile2);

        var subdir = FileSystemHelper.Path.Combine(repo.RepositoryPath, "subdir");

        this.fileSystem.Directory.CreateDirectory(subdir);
        var assemblyFile3 = FileSystemHelper.Path.Combine(subdir, "LocalAssemblyInfo.cs");
        using var file3 = this.fileSystem.File.Create(assemblyFile3);

        var arguments = this.argumentParser.ParseArguments($"--target-path {repo.RepositoryPath} --update-assembly-info **/*AssemblyInfo.cs");
        arguments.UpdateAssemblyInfo.ShouldBe(true);
        arguments.UpdateAssemblyInfoFileName.Count.ShouldBe(3);
        arguments.UpdateAssemblyInfoFileName.ShouldContain(x => FileSystemHelper.Path.GetFileName(x).Equals("CommonAssemblyInfo.cs"));
        arguments.UpdateAssemblyInfoFileName.ShouldContain(x => FileSystemHelper.Path.GetFileName(x).Equals("VersionAssemblyInfo.cs"));
        arguments.UpdateAssemblyInfoFileName.ShouldContain(x => FileSystemHelper.Path.GetFileName(x).Equals("LocalAssemblyInfo.cs"));
    }

    [Test]
    public void UpdateAssemblyInfoWithRelativeFilename()
    {
        using var repo = new EmptyRepositoryFixture();

        var assemblyFile = FileSystemHelper.Path.Combine(repo.RepositoryPath, "CommonAssemblyInfo.cs");
        using var file = this.fileSystem.File.Create(assemblyFile);

        var targetPath = FileSystemHelper.Path.Combine(repo.RepositoryPath, "subdir1", "subdir2");
        this.fileSystem.Directory.CreateDirectory(targetPath);

        var arguments = this.argumentParser.ParseArguments($@"--target-path {targetPath} --update-assembly-info ..\..\CommonAssemblyInfo.cs");
        arguments.UpdateAssemblyInfo.ShouldBe(true);
        arguments.UpdateAssemblyInfoFileName.Count.ShouldBe(1);
        arguments.UpdateAssemblyInfoFileName.ShouldContain(x => FileSystemHelper.Path.GetFileName(x).Equals("CommonAssemblyInfo.cs"));
    }

    [Test]
    public void OverrideconfigWithNoOptions()
    {
        var arguments = this.argumentParser.ParseArguments("--override-config");
        arguments.OverrideConfiguration.ShouldBeNull();
    }

    [TestCaseSource(nameof(OverrideconfigWithInvalidOptionTestData))]
    public string OverrideconfigWithInvalidOption(string options)
    {
        var exception = Assert.Throws<WarningException>(() => this.argumentParser.ParseArguments($"--override-config {options}"));
        exception.ShouldNotBeNull();
        return exception.Message;
    }

    private static IEnumerable<TestCaseData> OverrideconfigWithInvalidOptionTestData()
    {
        yield return new TestCaseData("tag-prefix=sample=asdf")
        {
            ExpectedResult = "Could not parse --override-config option: tag-prefix=sample=asdf. Ensure it is in format 'key=value'."
        };
        yield return new TestCaseData("unknown-option=25")
        {
            ExpectedResult = "Could not parse --override-config option: unknown-option=25. Unsupported key 'unknown-option'."
        };
    }

    [TestCaseSource(nameof(OverrideConfigWithSingleOptionTestData))]
    public void OverrideConfigWithSingleOptions(string options, IGitVersionConfiguration expected)
    {
        var arguments = this.argumentParser.ParseArguments($"--override-config {options}");

        ConfigurationHelper configurationHelper = new(arguments.OverrideConfiguration);
        configurationHelper.Configuration.ShouldBeEquivalentTo(expected);
    }

    private static IEnumerable<TestCaseData> OverrideConfigWithSingleOptionTestData()
    {
        yield return new TestCaseData(
            "assembly-versioning-scheme=MajorMinor",
            new GitVersionConfiguration
            {
                AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinor
            }
        );
        yield return new TestCaseData(
            "assembly-file-versioning-scheme=\"MajorMinorPatch\"",
            new GitVersionConfiguration
            {
                AssemblyFileVersioningScheme = AssemblyFileVersioningScheme.MajorMinorPatch
            }
        );
        yield return new TestCaseData(
            "assembly-informational-format=\"{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}\"",
            new GitVersionConfiguration
            {
                AssemblyInformationalFormat = "{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}"
            }
        );
        yield return new TestCaseData(
            "assembly-versioning-format=\"{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}\"",
            new GitVersionConfiguration
            {
                AssemblyVersioningFormat = "{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}"
            }
        );
        yield return new TestCaseData(
            "assembly-file-versioning-format=\"{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}\"",
            new GitVersionConfiguration
            {
                AssemblyFileVersioningFormat = "{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}"
            }
        );
        yield return new TestCaseData(
            "mode=ContinuousDelivery",
            new GitVersionConfiguration
            {
                DeploymentMode = DeploymentMode.ContinuousDelivery
            }
        );
        yield return new TestCaseData(
            "tag-prefix=sample",
            new GitVersionConfiguration
            {
                TagPrefixPattern = "sample"
            }
        );
        yield return new TestCaseData(
            "label=cd-label",
            new GitVersionConfiguration
            {
                Label = "cd-label"
            }
        );
        yield return new TestCaseData(
            "next-version=1",
            new GitVersionConfiguration
            {
                NextVersion = "1"
            }
        );
        yield return new TestCaseData(
            "major-version-bump-message=\"This is major version bump message.\"",
            new GitVersionConfiguration
            {
                MajorVersionBumpMessage = "This is major version bump message."
            }
        );
        yield return new TestCaseData(
            "minor-version-bump-message=\"This is minor version bump message.\"",
            new GitVersionConfiguration
            {
                MinorVersionBumpMessage = "This is minor version bump message."
            }
        );
        yield return new TestCaseData(
            "patch-version-bump-message=\"This is patch version bump message.\"",
            new GitVersionConfiguration
            {
                PatchVersionBumpMessage = "This is patch version bump message."
            }
        );
        yield return new TestCaseData(
            "no-bump-message=\"This is no bump message.\"",
            new GitVersionConfiguration
            {
                NoBumpMessage = "This is no bump message."
            }
        );
        yield return new TestCaseData(
            "tag-pre-release-weight=2",
            new GitVersionConfiguration
            {
                TagPreReleaseWeight = 2
            }
        );
        yield return new TestCaseData(
            "commit-message-incrementing=MergeMessageOnly",
            new GitVersionConfiguration
            {
                CommitMessageIncrementing = CommitMessageIncrementMode.MergeMessageOnly
            }
        );
        yield return new TestCaseData(
            "increment=Minor",
            new GitVersionConfiguration
            {
                Increment = IncrementStrategy.Minor
            }
        );
        yield return new TestCaseData(
            "commit-date-format=\"MM/dd/yyyy h:mm tt\"",
            new GitVersionConfiguration
            {
                CommitDateFormat = "MM/dd/yyyy h:mm tt"
            }
        );
        yield return new TestCaseData(
            "update-build-number=true",
            new GitVersionConfiguration
            {
                UpdateBuildNumber = true
            }
        );
        yield return new TestCaseData(
            "strategies=[\"None\",\"Mainline\"]",
            new GitVersionConfiguration
            {
                VersionStrategies = [VersionStrategies.None, VersionStrategies.Mainline]
            }
        );
    }

    [TestCaseSource(nameof(OverrideConfigWithMultipleOptionsTestData))]
    public void OverrideConfigWithMultipleOptions(string options, IGitVersionConfiguration expected)
    {
        var arguments = this.argumentParser.ParseArguments(options);
        ConfigurationHelper configurationHelper = new(arguments.OverrideConfiguration);
        configurationHelper.Configuration.ShouldBeEquivalentTo(expected);
    }

    private static IEnumerable<TestCaseData> OverrideConfigWithMultipleOptionsTestData()
    {
        yield return new TestCaseData(
            "--override-config tag-prefix=sample --override-config assembly-versioning-scheme=MajorMinor",
            new GitVersionConfiguration
            {
                TagPrefixPattern = "sample",
                AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinor
            }
        );
        yield return new TestCaseData(
            "--override-config tag-prefix=sample --override-config assembly-versioning-format=\"{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}\"",
            new GitVersionConfiguration
            {
                TagPrefixPattern = "sample",
                AssemblyVersioningFormat = "{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}"
            }
        );
        yield return new TestCaseData(
            "--override-config tag-prefix=sample --override-config assembly-versioning-format=\"{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}\" --override-config update-build-number=true --override-config assembly-versioning-scheme=MajorMinorPatchTag --override-config mode=ContinuousDelivery --override-config tag-pre-release-weight=4",
            new GitVersionConfiguration
            {
                TagPrefixPattern = "sample",
                AssemblyVersioningFormat = "{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}",
                UpdateBuildNumber = true,
                AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatchTag,
                DeploymentMode = DeploymentMode.ContinuousDelivery,
                TagPreReleaseWeight = 4
            }
        );
    }

    [Test]
    public void EnsureAssemblyInfoTrueWhenFound()
    {
        var arguments = this.argumentParser.ParseArguments("--ensure-assembly-info");
        arguments.EnsureAssemblyInfo.ShouldBe(true);
    }

    [Test]
    public void EnsureAssemblyInfoTrue()
    {
        var arguments = this.argumentParser.ParseArguments("--ensure-assembly-info true");
        arguments.EnsureAssemblyInfo.ShouldBe(true);
    }

    [Test]
    public void EnsureAssemblyInfoFalse()
    {
        var arguments = this.argumentParser.ParseArguments("--ensure-assembly-info false");
        arguments.EnsureAssemblyInfo.ShouldBe(false);
    }

    [Test]
    public void DynamicRepoLocation()
    {
        var arguments = this.argumentParser.ParseArguments("--dynamic-repo-location /tmp/foo");
        arguments.ClonePath.ShouldBe("/tmp/foo");
    }

    [Test]
    public void CanLogToConsole()
    {
        var arguments = this.argumentParser.ParseArguments("--log-file console");
        arguments.LogFilePath.ShouldBe("console");
    }

    [Test]
    public void NofetchTrueWhenDefined()
    {
        var arguments = this.argumentParser.ParseArguments("--no-fetch");
        arguments.NoFetch.ShouldBe(true);
    }

    [Test]
    public void NoNormalizeTrueWhenDefined()
    {
        var arguments = this.argumentParser.ParseArguments("--no-normalize");
        arguments.NoNormalize.ShouldBe(true);
    }

    [Test]
    public void AllowshallowTrueWhenDefined()
    {
        var arguments = this.argumentParser.ParseArguments("--allow-shallow");
        arguments.AllowShallow.ShouldBe(true);
    }

    [Test]
    public void DiagTrueWhenDefined()
    {
        var arguments = this.argumentParser.ParseArguments("--diagnose");
        arguments.Diag.ShouldBe(true);
    }

    [Test]
    public void DiagAndLogToConsoleIsNotIgnored()
    {
        var arguments = this.argumentParser.ParseArguments("--diagnose --log-file console");
        arguments.Diag.ShouldBe(true);
        arguments.LogFilePath.ShouldBe("console");
    }

    [Test]
    public void OtherArgumentsCanBeParsedBeforeNofetch()
    {
        var arguments = this.argumentParser.ParseArguments("targetpath --no-fetch");
        arguments.TargetPath.ShouldBe("targetpath");
        arguments.NoFetch.ShouldBe(true);
    }

    [Test]
    public void OtherArgumentsCanBeParsedBeforeNonormalize()
    {
        var arguments = this.argumentParser.ParseArguments("targetpath --no-normalize");
        arguments.TargetPath.ShouldBe("targetpath");
        arguments.NoNormalize.ShouldBe(true);
    }

    [Test]
    public void OtherArgumentsCanBeParsedBeforeNocache()
    {
        var arguments = this.argumentParser.ParseArguments("targetpath --no-cache");
        arguments.TargetPath.ShouldBe("targetpath");
        arguments.NoCache.ShouldBe(true);
    }

    [Test]
    public void OtherArgumentsCanBeParsedBeforeAllowshallow()
    {
        var arguments = this.argumentParser.ParseArguments("targetpath --allow-shallow");
        arguments.TargetPath.ShouldBe("targetpath");
        arguments.AllowShallow.ShouldBe(true);
    }

    [TestCase("--no-fetch --no-normalize --no-cache --allow-shallow")]
    [TestCase("--no-fetch --no-normalize --allow-shallow --no-cache")]
    [TestCase("--no-fetch --no-cache --no-normalize --allow-shallow")]
    [TestCase("--no-fetch --no-cache --allow-shallow --no-normalize")]
    [TestCase("--no-fetch --allow-shallow --no-normalize --no-cache")]
    [TestCase("--no-fetch --allow-shallow --no-cache --no-normalize")]
    [TestCase("--no-normalize --no-fetch --no-cache --allow-shallow")]
    [TestCase("--no-normalize --no-fetch --allow-shallow --no-cache")]
    [TestCase("--no-normalize --no-cache --no-fetch --allow-shallow")]
    [TestCase("--no-normalize --no-cache --allow-shallow --no-fetch")]
    [TestCase("--no-normalize --allow-shallow --no-fetch --no-cache")]
    [TestCase("--no-normalize --allow-shallow --no-cache --no-fetch")]
    [TestCase("--no-cache --no-fetch --no-normalize --allow-shallow")]
    [TestCase("--no-cache --no-fetch --allow-shallow --no-normalize")]
    [TestCase("--no-cache --no-normalize --no-fetch --allow-shallow")]
    [TestCase("--no-cache --no-normalize --allow-shallow --no-fetch")]
    [TestCase("--no-cache --allow-shallow --no-fetch --no-normalize")]
    [TestCase("--no-cache --allow-shallow --no-normalize --no-fetch")]
    [TestCase("--allow-shallow --no-fetch --no-normalize --no-cache")]
    [TestCase("--allow-shallow --no-fetch --no-cache --no-normalize")]
    [TestCase("--allow-shallow --no-normalize --no-fetch --no-cache")]
    [TestCase("--allow-shallow --no-normalize --no-cache --no-fetch")]
    [TestCase("--allow-shallow --no-cache --no-fetch --no-normalize")]
    [TestCase("--allow-shallow --no-cache --no-normalize --no-fetch")]
    public void SeveralSwitchesCanBeParsed(string commandLineArgs)
    {
        var arguments = this.argumentParser.ParseArguments(commandLineArgs);
        arguments.NoCache.ShouldBe(true);
        arguments.NoNormalize.ShouldBe(true);
        arguments.NoFetch.ShouldBe(true);
        arguments.AllowShallow.ShouldBe(true);
    }

    [Test]
    public void LogPathCanContainForwardSlash()
    {
        var arguments = this.argumentParser.ParseArguments("-l /some/path");
        arguments.LogFilePath.ShouldBe("/some/path");
    }

    [Test]
    public void BooleanArgumentHandling()
    {
        var arguments = this.argumentParser.ParseArguments("--no-fetch --update-assembly-info true");
        arguments.NoFetch.ShouldBe(true);
        arguments.UpdateAssemblyInfo.ShouldBe(true);
    }

    [Test]
    public void NocacheTrueWhenDefined()
    {
        var arguments = this.argumentParser.ParseArguments("--no-cache");
        arguments.NoCache.ShouldBe(true);
    }

    [TestCase("x", true, Verbosity.Normal)]
    [TestCase("diagnostic", false, Verbosity.Diagnostic)]
    [TestCase("Minimal", false, Verbosity.Minimal)]
    [TestCase("NORMAL", false, Verbosity.Normal)]
    [TestCase("quiet", false, Verbosity.Quiet)]
    [TestCase("Verbose", false, Verbosity.Verbose)]
    public void CheckVerbosityParsing(string command, bool shouldThrow, Verbosity expectedVerbosity)
    {
        if (shouldThrow)
        {
            Assert.Throws<WarningException>(() => ArgumentParser.ParseVerbosity(command));
        }
        else
        {
            var verbosity = ArgumentParser.ParseVerbosity(command);
            verbosity.ShouldBe(expectedVerbosity);
        }
    }

    [Test]
    public void EmptyArgumentsRemoteUsernameDefinedSetsUsername()
    {
        this.environment.SetEnvironmentVariable("GITVERSION_REMOTE_USERNAME", "value");
        var arguments = this.argumentParser.ParseArguments(string.Empty);
        arguments.Authentication.Username.ShouldBe("value");
    }

    [Test]
    public void EmptyArgumentsRemotePasswordDefinedSetsPassword()
    {
        this.environment.SetEnvironmentVariable("GITVERSION_REMOTE_PASSWORD", "value");
        var arguments = this.argumentParser.ParseArguments(string.Empty);
        arguments.Authentication.Password.ShouldBe("value");
    }

    [Test]
    public void ArbitraryArgumentsRemoteUsernameDefinedSetsUsername()
    {
        this.environment.SetEnvironmentVariable("GITVERSION_REMOTE_USERNAME", "value");
        var arguments = this.argumentParser.ParseArguments("--no-cache");
        arguments.Authentication.Username.ShouldBe("value");
    }

    [Test]
    public void ArbitraryArgumentsRemotePasswordDefinedSetsPassword()
    {
        this.environment.SetEnvironmentVariable("GITVERSION_REMOTE_PASSWORD", "value");
        var arguments = this.argumentParser.ParseArguments("--no-cache");
        arguments.Authentication.Password.ShouldBe("value");
    }

    [Test]
    public void EnsureShowVariableIsSet()
    {
        var arguments = this.argumentParser.ParseArguments("--show-variable SemVer");
        arguments.ShowVariable.ShouldBe("SemVer");
    }

    [Test]
    public void EnsureFormatIsSet()
    {
        var arguments = this.argumentParser.ParseArguments("--format {Major}.{Minor}.{Patch}");
        arguments.Format.ShouldBe("{Major}.{Minor}.{Patch}");
    }

    [TestCase("custom-config.yaml")]
    [TestCase("/tmp/custom-config.yaml")]
    public void ThrowIfConfigurationFileDoesNotExist(string configFile) =>
        Should.Throw<WarningException>(() => _ = this.argumentParser.ParseArguments($"--config {configFile}"));

    [Test]
    public void EnsureConfigurationFileIsSet()
    {
        var configFile = FileSystemHelper.Path.GetTempPath() + Guid.NewGuid() + ".yaml";
        this.fileSystem.File.WriteAllText(configFile, "next-version: 1.0.0");
        var arguments = this.argumentParser.ParseArguments($"--config {configFile}");
        arguments.ConfigurationFile.ShouldBe(configFile);
        this.fileSystem.File.Delete(configFile);
    }

    [Test]
    public void ShortAliasCIsConfigNotCommit()
    {
        // -c is aliased to --config, NOT --commit (breaking change from v6)
        var configFile = FileSystemHelper.Path.GetTempPath() + Guid.NewGuid() + ".yaml";
        this.fileSystem.File.WriteAllText(configFile, "next-version: 1.0.0");

        var arguments = this.argumentParser.ParseArguments($"-c {configFile}");

        arguments.ConfigurationFile.ShouldBe(configFile);
        arguments.CommitId.ShouldBeNullOrEmpty();

        this.fileSystem.File.Delete(configFile);
    }

    [Test]
    public void CommitLongFormSetsCommitId()
    {
        var arguments = this.argumentParser.ParseArguments("--url https://github.com/GitTools/GitVersion.git --branch main --commit abc1234");
        arguments.CommitId.ShouldBe("abc1234");
        arguments.ConfigurationFile.ShouldBeNull();
    }
}
