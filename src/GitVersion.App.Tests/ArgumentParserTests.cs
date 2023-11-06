using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.App.Tests;

[TestFixture]
public class ArgumentParserTests : TestBase
{
    private IEnvironment environment;
    private IArgumentParser argumentParser;

    [SetUp]
    public void SetUp()
    {
        var sp = ConfigureServices(services =>
        {
            services.AddSingleton<IArgumentParser, ArgumentParser>();
            services.AddSingleton<IGlobbingResolver, GlobbingResolver>();
        });
        this.environment = sp.GetRequiredService<IEnvironment>();
        this.argumentParser = sp.GetRequiredService<IArgumentParser>();
    }

    [Test]
    public void EmptyMeansUseCurrentDirectory()
    {
        var arguments = this.argumentParser.ParseArguments("");
        arguments.TargetPath.ShouldBe(SysEnv.CurrentDirectory);
        arguments.LogFilePath.ShouldBe(null);
        arguments.IsHelp.ShouldBe(false);
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
        var arguments = this.argumentParser.ParseArguments("-version");
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
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath -l logFilePath");
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
        var exception = Assert.Throws<WarningException>(() => this.argumentParser.ParseArguments("targetDirectoryPath -output invalid_value"));
        exception.ShouldNotBeNull();
        exception.Message.ShouldBe("Value 'invalid_value' cannot be parsed as output type, please use 'json', 'file' or 'buildserver'");
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
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath -output json");
        arguments.Output.ShouldContain(OutputType.Json);
        arguments.Output.ShouldNotContain(OutputType.BuildServer);
        arguments.Output.ShouldNotContain(OutputType.File);
    }

    [Test]
    public void MultipleOutputJsonCanBeParsed()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath -output json -output json");
        arguments.Output.ShouldContain(OutputType.Json);
        arguments.Output.ShouldNotContain(OutputType.BuildServer);
        arguments.Output.ShouldNotContain(OutputType.File);
    }

    [Test]
    public void OutputBuildserverCanBeParsed()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath -output buildserver");
        arguments.Output.ShouldContain(OutputType.BuildServer);
        arguments.Output.ShouldNotContain(OutputType.Json);
        arguments.Output.ShouldNotContain(OutputType.File);
    }

    [Test]
    public void MultipleOutputBuildserverCanBeParsed()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath -output buildserver -output buildserver");
        arguments.Output.ShouldContain(OutputType.BuildServer);
        arguments.Output.ShouldNotContain(OutputType.Json);
        arguments.Output.ShouldNotContain(OutputType.File);
    }

    [Test]
    public void OutputFileCanBeParsed()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath -output file");
        arguments.Output.ShouldContain(OutputType.File);
        arguments.Output.ShouldNotContain(OutputType.BuildServer);
        arguments.Output.ShouldNotContain(OutputType.Json);
    }

    [Test]
    public void MultipleOutputFileCanBeParsed()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath -output file -output file");
        arguments.Output.ShouldContain(OutputType.File);
        arguments.Output.ShouldNotContain(OutputType.BuildServer);
        arguments.Output.ShouldNotContain(OutputType.Json);
    }

    [Test]
    public void OutputBuildserverAndJsonCanBeParsed()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath -output buildserver -output json");
        arguments.Output.ShouldContain(OutputType.BuildServer);
        arguments.Output.ShouldContain(OutputType.Json);
        arguments.Output.ShouldNotContain(OutputType.File);
    }

    [Test]
    public void OutputBuildserverAndJsonAndFileCanBeParsed()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath -output buildserver -output json -output file");
        arguments.Output.ShouldContain(OutputType.BuildServer);
        arguments.Output.ShouldContain(OutputType.Json);
        arguments.Output.ShouldContain(OutputType.File);
    }

    [Test]
    public void MultipleArgsAndFlag()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath -output buildserver -updateAssemblyInfo");
        arguments.Output.ShouldContain(OutputType.BuildServer);
    }

    [TestCase("-output file", "GitVersion.json")]
    [TestCase("-output file -outputfile version.json", "version.json")]
    public void OutputFileArgumentCanBeParsed(string args, string outputFile)
    {
        var arguments = this.argumentParser.ParseArguments(args);

        arguments.Output.ShouldContain(OutputType.File);
        arguments.OutputFile.ShouldBe(outputFile);
    }

    [Test]
    public void UrlAndBranchNameCanBeParsed()
    {
        var arguments = this.argumentParser.ParseArguments("targetDirectoryPath -url https://github.com/Particular/GitVersion.git -b someBranch");
        arguments.TargetPath.ShouldBe("targetDirectoryPath");
        arguments.TargetUrl.ShouldBe("https://github.com/Particular/GitVersion.git");
        arguments.TargetBranch.ShouldBe("someBranch");
        arguments.IsHelp.ShouldBe(false);
    }

    [Test]
    public void WrongNumberOfArgumentsShouldThrow()
    {
        var exception = Assert.Throws<WarningException>(() => this.argumentParser.ParseArguments("targetDirectoryPath -l logFilePath extraArg"));
        exception.ShouldNotBeNull();
        exception.Message.ShouldBe("Could not parse command line parameter 'extraArg'.");
    }

    [TestCase("targetDirectoryPath -x logFilePath")]
    [TestCase("/invalid-argument")]
    public void UnknownArgumentsShouldThrow(string arguments)
    {
        var exception = Assert.Throws<WarningException>(() => this.argumentParser.ParseArguments(arguments));
        exception.ShouldNotBeNull();
        exception.Message.ShouldStartWith("Could not parse command line parameter");
    }

    [TestCase("-updateAssemblyInfo true")]
    [TestCase("-updateAssemblyInfo 1")]
    [TestCase("-updateAssemblyInfo")]
    [TestCase("-updateAssemblyInfo assemblyInfo.cs")]
    [TestCase("-updateAssemblyInfo assemblyInfo.cs -ensureassemblyinfo")]
    [TestCase("-updateAssemblyInfo assemblyInfo.cs otherAssemblyInfo.cs")]
    [TestCase("-updateAssemblyInfo Assembly.cs Assembly.cs -ensureassemblyinfo")]
    public void UpdateAssemblyInfoTrue(string command)
    {
        var arguments = this.argumentParser.ParseArguments(command);
        arguments.UpdateAssemblyInfo.ShouldBe(true);
    }

    [TestCase("-updateProjectFiles assemblyInfo.csproj")]
    [TestCase("-updateProjectFiles assemblyInfo.csproj")]
    [TestCase("-updateProjectFiles assemblyInfo.csproj otherAssemblyInfo.fsproj")]
    [TestCase("-updateProjectFiles")]
    public void UpdateProjectTrue(string command)
    {
        var arguments = this.argumentParser.ParseArguments(command);
        arguments.UpdateProjectFiles.ShouldBe(true);
    }

    [TestCase("-updateAssemblyInfo false")]
    [TestCase("-updateAssemblyInfo 0")]
    public void UpdateAssemblyInfoFalse(string command)
    {
        var arguments = this.argumentParser.ParseArguments(command);
        arguments.UpdateAssemblyInfo.ShouldBe(false);
    }

    [TestCase("-updateAssemblyInfo Assembly.cs Assembly1.cs -ensureassemblyinfo")]
    public void CreateMultipleAssemblyInfoProtected(string command)
    {
        var exception = Assert.Throws<WarningException>(() => this.argumentParser.ParseArguments(command));
        exception.ShouldNotBeNull();
        exception.Message.ShouldBe("Can't specify multiple assembly info files when using /ensureassemblyinfo switch, either use a single assembly info file or do not specify /ensureassemblyinfo and create assembly info files manually");
    }

    [TestCase("-updateProjectFiles Assembly.csproj -ensureassemblyinfo")]
    public void UpdateProjectInfoWithEnsureAssemblyInfoProtected(string command)
    {
        var exception = Assert.Throws<WarningException>(() => this.argumentParser.ParseArguments(command));
        exception.ShouldNotBeNull();
        exception.Message.ShouldBe("Cannot specify -ensureassemblyinfo with updateprojectfiles: please ensure your project file exists before attempting to update it");
    }

    [Test]
    public void UpdateAssemblyInfoWithFilename()
    {
        using var repo = new EmptyRepositoryFixture();

        var assemblyFile = PathHelper.Combine(repo.RepositoryPath, "CommonAssemblyInfo.cs");
        using var file = File.Create(assemblyFile);

        var arguments = this.argumentParser.ParseArguments($"-targetpath {repo.RepositoryPath} -updateAssemblyInfo CommonAssemblyInfo.cs");
        arguments.UpdateAssemblyInfo.ShouldBe(true);
        arguments.UpdateAssemblyInfoFileName.Count.ShouldBe(1);
        arguments.UpdateAssemblyInfoFileName.ShouldContain(x => Path.GetFileName(x).Equals("CommonAssemblyInfo.cs"));
    }

    [Test]
    public void UpdateAssemblyInfoWithMultipleFilenames()
    {
        using var repo = new EmptyRepositoryFixture();

        var assemblyFile1 = PathHelper.Combine(repo.RepositoryPath, "CommonAssemblyInfo.cs");
        using var file = File.Create(assemblyFile1);

        var assemblyFile2 = PathHelper.Combine(repo.RepositoryPath, "VersionAssemblyInfo.cs");
        using var file2 = File.Create(assemblyFile2);

        var arguments = this.argumentParser.ParseArguments($"-targetpath {repo.RepositoryPath} -updateAssemblyInfo CommonAssemblyInfo.cs VersionAssemblyInfo.cs");
        arguments.UpdateAssemblyInfo.ShouldBe(true);
        arguments.UpdateAssemblyInfoFileName.Count.ShouldBe(2);
        arguments.UpdateAssemblyInfoFileName.ShouldContain(x => Path.GetFileName(x).Equals("CommonAssemblyInfo.cs"));
        arguments.UpdateAssemblyInfoFileName.ShouldContain(x => Path.GetFileName(x).Equals("VersionAssemblyInfo.cs"));
    }

    [Test]
    public void UpdateProjectFilesWithMultipleFilenames()
    {
        using var repo = new EmptyRepositoryFixture();

        var assemblyFile1 = PathHelper.Combine(repo.RepositoryPath, "CommonAssemblyInfo.csproj");
        using var file = File.Create(assemblyFile1);

        var assemblyFile2 = PathHelper.Combine(repo.RepositoryPath, "VersionAssemblyInfo.csproj");
        using var file2 = File.Create(assemblyFile2);

        var arguments = this.argumentParser.ParseArguments($"-targetpath {repo.RepositoryPath} -updateProjectFiles CommonAssemblyInfo.csproj VersionAssemblyInfo.csproj");
        arguments.UpdateProjectFiles.ShouldBe(true);
        arguments.UpdateAssemblyInfoFileName.Count.ShouldBe(2);
        arguments.UpdateAssemblyInfoFileName.ShouldContain(x => Path.GetFileName(x).Equals("CommonAssemblyInfo.csproj"));
        arguments.UpdateAssemblyInfoFileName.ShouldContain(x => Path.GetFileName(x).Equals("VersionAssemblyInfo.csproj"));
    }

    [Test]
    public void UpdateAssemblyInfoWithMultipleFilenamesMatchingGlobbing()
    {
        using var repo = new EmptyRepositoryFixture();

        var assemblyFile1 = PathHelper.Combine(repo.RepositoryPath, "CommonAssemblyInfo.cs");
        using var file = File.Create(assemblyFile1);

        var assemblyFile2 = PathHelper.Combine(repo.RepositoryPath, "VersionAssemblyInfo.cs");
        using var file2 = File.Create(assemblyFile2);

        var subdir = PathHelper.Combine(repo.RepositoryPath, "subdir");
        Directory.CreateDirectory(subdir);
        var assemblyFile3 = PathHelper.Combine(subdir, "LocalAssemblyInfo.cs");
        using var file3 = File.Create(assemblyFile3);

        var arguments = this.argumentParser.ParseArguments($"-targetpath {repo.RepositoryPath} -updateAssemblyInfo **/*AssemblyInfo.cs");
        arguments.UpdateAssemblyInfo.ShouldBe(true);
        arguments.UpdateAssemblyInfoFileName.Count.ShouldBe(3);
        arguments.UpdateAssemblyInfoFileName.ShouldContain(x => Path.GetFileName(x).Equals("CommonAssemblyInfo.cs"));
        arguments.UpdateAssemblyInfoFileName.ShouldContain(x => Path.GetFileName(x).Equals("VersionAssemblyInfo.cs"));
        arguments.UpdateAssemblyInfoFileName.ShouldContain(x => Path.GetFileName(x).Equals("LocalAssemblyInfo.cs"));
    }

    [Test]
    public void UpdateAssemblyInfoWithRelativeFilename()
    {
        using var repo = new EmptyRepositoryFixture();

        var assemblyFile = PathHelper.Combine(repo.RepositoryPath, "CommonAssemblyInfo.cs");
        using var file = File.Create(assemblyFile);

        var targetPath = PathHelper.Combine(repo.RepositoryPath, "subdir1", "subdir2");
        Directory.CreateDirectory(targetPath);

        var arguments = this.argumentParser.ParseArguments($"-targetpath {targetPath} -updateAssemblyInfo ..\\..\\CommonAssemblyInfo.cs");
        arguments.UpdateAssemblyInfo.ShouldBe(true);
        arguments.UpdateAssemblyInfoFileName.Count.ShouldBe(1);
        arguments.UpdateAssemblyInfoFileName.ShouldContain(x => Path.GetFileName(x).Equals("CommonAssemblyInfo.cs"));
    }

    [Test]
    public void OverrideconfigWithNoOptions()
    {
        var arguments = this.argumentParser.ParseArguments("/overrideconfig");
        arguments.OverrideConfiguration.ShouldBeNull();
    }

    [TestCaseSource(nameof(OverrideconfigWithInvalidOptionTestData))]
    public string OverrideconfigWithInvalidOption(string options)
    {
        var exception = Assert.Throws<WarningException>(() => this.argumentParser.ParseArguments($"/overrideconfig {options}"));
        exception.ShouldNotBeNull();
        return exception.Message;
    }

    private static IEnumerable<TestCaseData> OverrideconfigWithInvalidOptionTestData()
    {
        yield return new TestCaseData("tag-prefix=sample=asdf")
        {
            ExpectedResult = "Could not parse /overrideconfig option: tag-prefix=sample=asdf. Ensure it is in format 'key=value'."
        };
        yield return new TestCaseData("unknown-option=25")
        {
            ExpectedResult = "Could not parse /overrideconfig option: unknown-option=25. Unsupported 'key'."
        };
    }

    [TestCaseSource(nameof(OverrideConfigWithSingleOptionTestData))]
    public void OverrideConfigWithSingleOptions(string options, IGitVersionConfiguration expected)
    {
        var arguments = this.argumentParser.ParseArguments($"/overrideconfig {options}");

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
                VersioningMode = VersioningMode.ContinuousDelivery
            }
        );
        yield return new TestCaseData(
            "tag-prefix=sample",
            new GitVersionConfiguration
            {
                TagPrefix = "sample"
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
            "/overrideconfig tag-prefix=sample /overrideconfig assembly-versioning-scheme=MajorMinor",
            new GitVersionConfiguration
            {
                TagPrefix = "sample",
                AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinor
            }
        );
        yield return new TestCaseData(
            "/overrideconfig tag-prefix=sample /overrideconfig assembly-versioning-format=\"{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}\"",
            new GitVersionConfiguration
            {
                TagPrefix = "sample",
                AssemblyVersioningFormat = "{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}"
            }
        );
        yield return new TestCaseData(
            "/overrideconfig tag-prefix=sample /overrideconfig assembly-versioning-format=\"{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}\" /overrideconfig update-build-number=true /overrideconfig assembly-versioning-scheme=MajorMinorPatchTag /overrideconfig mode=ContinuousDelivery /overrideconfig tag-pre-release-weight=4",
            new GitVersionConfiguration
            {
                TagPrefix = "sample",
                AssemblyVersioningFormat = "{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}",
                UpdateBuildNumber = true,
                AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatchTag,
                VersioningMode = VersioningMode.ContinuousDelivery,
                TagPreReleaseWeight = 4
            }
        );
    }

    [Test]
    public void EnsureAssemblyInfoTrueWhenFound()
    {
        var arguments = this.argumentParser.ParseArguments("-ensureAssemblyInfo");
        arguments.EnsureAssemblyInfo.ShouldBe(true);
    }

    [Test]
    public void EnsureAssemblyInfoTrue()
    {
        var arguments = this.argumentParser.ParseArguments("-ensureAssemblyInfo true");
        arguments.EnsureAssemblyInfo.ShouldBe(true);
    }

    [Test]
    public void EnsureAssemblyInfoFalse()
    {
        var arguments = this.argumentParser.ParseArguments("-ensureAssemblyInfo false");
        arguments.EnsureAssemblyInfo.ShouldBe(false);
    }

    [Test]
    public void DynamicRepoLocation()
    {
        var arguments = this.argumentParser.ParseArguments("-dynamicRepoLocation c:\\foo\\");
        arguments.ClonePath.ShouldBe("c:\\foo\\");
    }

    [Test]
    public void CanLogToConsole()
    {
        var arguments = this.argumentParser.ParseArguments("-l console");
        arguments.LogFilePath.ShouldBe("console");
    }

    [Test]
    public void NofetchTrueWhenDefined()
    {
        var arguments = this.argumentParser.ParseArguments("-nofetch");
        arguments.NoFetch.ShouldBe(true);
    }

    [Test]
    public void NoNormalizeTrueWhenDefined()
    {
        var arguments = this.argumentParser.ParseArguments("-nonormalize");
        arguments.NoNormalize.ShouldBe(true);
    }

    [Test]
    public void OtherArgumentsCanBeParsedBeforeNofetch()
    {
        var arguments = this.argumentParser.ParseArguments("targetpath -nofetch ");
        arguments.TargetPath.ShouldBe("targetpath");
        arguments.NoFetch.ShouldBe(true);
    }

    [Test]
    public void OtherArgumentsCanBeParsedBeforeNonormalize()
    {
        var arguments = this.argumentParser.ParseArguments("targetpath -nonormalize");
        arguments.TargetPath.ShouldBe("targetpath");
        arguments.NoNormalize.ShouldBe(true);
    }

    [Test]
    public void OtherArgumentsCanBeParsedBeforeNocache()
    {
        var arguments = this.argumentParser.ParseArguments("targetpath -nocache");
        arguments.TargetPath.ShouldBe("targetpath");
        arguments.NoCache.ShouldBe(true);
    }

    [TestCase("-nofetch -nonormalize -nocache")]
    [TestCase("-nofetch -nocache -nonormalize")]
    [TestCase("-nocache -nofetch -nonormalize")]
    [TestCase("-nocache -nonormalize -nofetch")]
    [TestCase("-nonormalize -nocache -nofetch")]
    [TestCase("-nonormalize -nofetch -nocache")]
    public void SeveralSwitchesCanBeParsed(string commandLineArgs)
    {
        var arguments = this.argumentParser.ParseArguments(commandLineArgs);
        arguments.NoCache.ShouldBe(true);
        arguments.NoNormalize.ShouldBe(true);
        arguments.NoFetch.ShouldBe(true);
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
        var arguments = this.argumentParser.ParseArguments("/nofetch /updateassemblyinfo true");
        arguments.NoFetch.ShouldBe(true);
        arguments.UpdateAssemblyInfo.ShouldBe(true);
    }

    [Test]
    public void NocacheTrueWhenDefined()
    {
        var arguments = this.argumentParser.ParseArguments("-nocache");
        arguments.NoCache.ShouldBe(true);
    }

    [TestCase("-verbosity x", true, Verbosity.Normal)]
    [TestCase("-verbosity diagnostic", false, Verbosity.Diagnostic)]
    [TestCase("-verbosity Minimal", false, Verbosity.Minimal)]
    [TestCase("-verbosity NORMAL", false, Verbosity.Normal)]
    [TestCase("-verbosity quiet", false, Verbosity.Quiet)]
    [TestCase("-verbosity Verbose", false, Verbosity.Verbose)]
    public void CheckVerbosityParsing(string command, bool shouldThrow, Verbosity expectedVerbosity)
    {
        if (shouldThrow)
        {
            Assert.Throws<WarningException>(() => this.argumentParser.ParseArguments(command));
        }
        else
        {
            var arguments = this.argumentParser.ParseArguments(command);
            arguments.Verbosity.ShouldBe(expectedVerbosity);
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
        var arguments = this.argumentParser.ParseArguments("-nocache");
        arguments.Authentication.Username.ShouldBe("value");
    }

    [Test]
    public void ArbitraryArgumentsRemotePasswordDefinedSetsPassword()
    {
        this.environment.SetEnvironmentVariable("GITVERSION_REMOTE_PASSWORD", "value");
        var arguments = this.argumentParser.ParseArguments("-nocache");
        arguments.Authentication.Password.ShouldBe("value");
    }

    [Test]
    public void EnsureShowVariableIsSet()
    {
        var arguments = this.argumentParser.ParseArguments("-showvariable SemVer");
        arguments.ShowVariable.ShouldBe("SemVer");
    }

    [Test]
    public void EnsureFormatIsSet()
    {
        var arguments = this.argumentParser.ParseArguments("-format {Major}.{Minor}.{Patch}");
        arguments.Format.ShouldBe("{Major}.{Minor}.{Patch}");
    }

    [TestCase("custom-config.yaml")]
    [TestCase(@"c:\custom-config.yaml")]
    public void ThrowIfConfigurationFileDoesNotExist(string configFile) =>
        Should.Throw<WarningException>(() => _ = this.argumentParser.ParseArguments($"-config {configFile}"));

    [Test]
    public void EnsureConfigurationFileIsSet()
    {
        var configFile = Path.GetTempPath() + Guid.NewGuid() + ".yaml";
        File.WriteAllText(configFile, "next-version: 1.0.0");
        var arguments = this.argumentParser.ParseArguments($"-config {configFile}");
        arguments.ConfigurationFile.ShouldBe(configFile);
        File.Delete(configFile);
    }
}
