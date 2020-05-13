using System.IO;
using GitTools.Testing;
using GitVersion;
using GitVersion.Logging;
using GitVersion.Model;
using GitVersionCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using Environment = System.Environment;

namespace GitVersionExe.Tests
{
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
            environment = sp.GetService<IEnvironment>();
            argumentParser = sp.GetService<IArgumentParser>();
        }

        [Test]
        public void EmptyMeansUseCurrentDirectory()
        {
            var arguments = argumentParser.ParseArguments("");
            arguments.TargetPath.ShouldBe(Environment.CurrentDirectory);
            arguments.LogFilePath.ShouldBe(null);
            arguments.IsHelp.ShouldBe(false);
        }

        [Test]
        public void SingleMeansUseAsTargetDirectory()
        {
            var arguments = argumentParser.ParseArguments("path");
            arguments.TargetPath.ShouldBe("path");
            arguments.LogFilePath.ShouldBe(null);
            arguments.IsHelp.ShouldBe(false);
        }

        [Test]
        public void NoPathAndLogfileShouldUseCurrentDirectoryTargetDirectory()
        {
            var arguments = argumentParser.ParseArguments("-l logFilePath");
            arguments.TargetPath.ShouldBe(Environment.CurrentDirectory);
            arguments.LogFilePath.ShouldBe("logFilePath");
            arguments.IsHelp.ShouldBe(false);
        }

        [Test]
        public void HelpSwitchTest()
        {
            var arguments = argumentParser.ParseArguments("-h");
            Assert.IsNull(arguments.TargetPath);
            Assert.IsNull(arguments.LogFilePath);
            arguments.IsHelp.ShouldBe(true);
        }

        [Test]
        public void VersionSwitchTest()
        {
            var arguments = argumentParser.ParseArguments("-version");
            Assert.IsNull(arguments.TargetPath);
            Assert.IsNull(arguments.LogFilePath);
            arguments.IsVersion.ShouldBe(true);
        }

        [Test]
        public void Exec()
        {
            var arguments = argumentParser.ParseArguments("-exec rake");
#pragma warning disable CS0612 // Type or member is obsolete
            arguments.Exec.ShouldBe("rake");
#pragma warning restore CS0612 // Type or member is obsolete
        }

        [Test]
        public void ExecWithArgs()
        {
            var arguments = argumentParser.ParseArguments(new[]
            {
                "-exec",
                "rake",
                "-execargs",
                "clean build"
            });
#pragma warning disable CS0612 // Type or member is obsolete
            arguments.Exec.ShouldBe("rake");
            arguments.ExecArgs.ShouldBe("clean build");
#pragma warning restore CS0612 // Type or member is obsolete
        }

        [Test]
        public void Msbuild()
        {
            var arguments = argumentParser.ParseArguments("-proj msbuild.proj");
#pragma warning disable CS0612 // Type or member is obsolete
            arguments.Proj.ShouldBe("msbuild.proj");
#pragma warning restore CS0612 // Type or member is obsolete
        }

        [Test]
        public void MsbuildWithArgs()
        {
            var arguments = argumentParser.ParseArguments(new[]
            {
                "-proj",
                "msbuild.proj",
                "-projargs",
                "/p:Configuration=Debug /p:Platform=AnyCPU"
            });
#pragma warning disable CS0612 // Type or member is obsolete
            arguments.Proj.ShouldBe("msbuild.proj");
            arguments.ProjArgs.ShouldBe("/p:Configuration=Debug /p:Platform=AnyCPU");
#pragma warning restore CS0612 // Type or member is obsolete
        }

        [Test]
        public void ExecwithTargetdirectory()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath -exec rake");
            arguments.TargetPath.ShouldBe("targetDirectoryPath");
#pragma warning disable CS0612 // Type or member is obsolete
            arguments.Exec.ShouldBe("rake");
#pragma warning restore CS0612 // Type or member is obsolete
        }

        [Test]
        public void TargetDirectoryAndLogFilePathCanBeParsed()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath -l logFilePath");
            arguments.TargetPath.ShouldBe("targetDirectoryPath");
            arguments.LogFilePath.ShouldBe("logFilePath");
            arguments.IsHelp.ShouldBe(false);
        }

        [Test]
        public void UsernameAndPasswordCanBeParsed()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath -u [username] -p [password]");
            arguments.TargetPath.ShouldBe("targetDirectoryPath");
            arguments.Authentication.Username.ShouldBe("[username]");
            arguments.Authentication.Password.ShouldBe("[password]");
            arguments.IsHelp.ShouldBe(false);
        }

        [Test]
        public void UnknownOutputShouldThrow()
        {
            var exception = Assert.Throws<WarningException>(() => argumentParser.ParseArguments("targetDirectoryPath -output invalid_value"));
            exception.Message.ShouldBe("Value 'invalid_value' cannot be parsed as output type, please use 'json', 'file' or 'buildserver'");
        }

        [Test]
        public void OutputDefaultsToJson()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath");
            arguments.Output.ShouldContain(OutputType.Json);
            arguments.Output.ShouldNotContain(OutputType.BuildServer);
            arguments.Output.ShouldNotContain(OutputType.File);
        }

        [Test]
        public void OutputJsonCanBeParsed()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath -output json");
            arguments.Output.ShouldContain(OutputType.Json);
            arguments.Output.ShouldNotContain(OutputType.BuildServer);
            arguments.Output.ShouldNotContain(OutputType.File);
        }

        [Test]
        public void MultipleOutputJsonCanBeParsed()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath -output json -output json");
            arguments.Output.ShouldContain(OutputType.Json);
            arguments.Output.ShouldNotContain(OutputType.BuildServer);
            arguments.Output.ShouldNotContain(OutputType.File);
        }

        [Test]
        public void OutputBuildserverCanBeParsed()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath -output buildserver");
            arguments.Output.ShouldContain(OutputType.BuildServer);
            arguments.Output.ShouldNotContain(OutputType.Json);
            arguments.Output.ShouldNotContain(OutputType.File);
        }

        [Test]
        public void MultipleOutputBuildserverCanBeParsed()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath -output buildserver -output buildserver");
            arguments.Output.ShouldContain(OutputType.BuildServer);
            arguments.Output.ShouldNotContain(OutputType.Json);
            arguments.Output.ShouldNotContain(OutputType.File);
        }

        [Test]
        public void OutputFileCanBeParsed()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath -output file");
            arguments.Output.ShouldContain(OutputType.File);
            arguments.Output.ShouldNotContain(OutputType.BuildServer);
            arguments.Output.ShouldNotContain(OutputType.Json);
        }

        [Test]
        public void MultipleOutputFileCanBeParsed()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath -output file -output file");
            arguments.Output.ShouldContain(OutputType.File);
            arguments.Output.ShouldNotContain(OutputType.BuildServer);
            arguments.Output.ShouldNotContain(OutputType.Json);
        }

        [Test]
        public void OutputBuildserverAndJsonCanBeParsed()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath -output buildserver -output json");
            arguments.Output.ShouldContain(OutputType.BuildServer);
            arguments.Output.ShouldContain(OutputType.Json);
            arguments.Output.ShouldNotContain(OutputType.File);
        }

        [Test]
        public void OutputBuildserverAndJsonAndFileCanBeParsed()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath -output buildserver -output json -output file");
            arguments.Output.ShouldContain(OutputType.BuildServer);
            arguments.Output.ShouldContain(OutputType.Json);
            arguments.Output.ShouldContain(OutputType.File);
        }

        [Test]
        public void MultipleArgsAndFlag()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath -output buildserver -updateAssemblyInfo");
            arguments.Output.ShouldContain(OutputType.BuildServer);
        }

        [TestCase("-output file", "GitVersion.json")]
        [TestCase("-output file -outputfile version.json", "version.json")]
        public void OutputFileArgumentCanBeParsed(string args, string outputFile)
        {
            var arguments = argumentParser.ParseArguments(args);

            arguments.Output.ShouldContain(OutputType.File);
            arguments.OutputFile.ShouldBe(outputFile);
        }

        [Test]
        public void UrlAndBranchNameCanBeParsed()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath -url http://github.com/Particular/GitVersion.git -b somebranch");
            arguments.TargetPath.ShouldBe("targetDirectoryPath");
            arguments.TargetUrl.ShouldBe("http://github.com/Particular/GitVersion.git");
            arguments.TargetBranch.ShouldBe("somebranch");
            arguments.IsHelp.ShouldBe(false);
        }

        [Test]
        public void WrongNumberOfArgumentsShouldThrow()
        {
            var exception = Assert.Throws<WarningException>(() => argumentParser.ParseArguments("targetDirectoryPath -l logFilePath extraArg"));
            exception.Message.ShouldBe("Could not parse command line parameter 'extraArg'.");
        }

        [TestCase("targetDirectoryPath -x logFilePath")]
        [TestCase("/invalid-argument")]
        public void UnknownArgumentsShouldThrow(string arguments)
        {
            var exception = Assert.Throws<WarningException>(() => argumentParser.ParseArguments(arguments));
            exception.Message.ShouldStartWith("Could not parse command line parameter");
        }

        [TestCase("-updateAssemblyInfo true")]
        [TestCase("-updateAssemblyInfo 1")]
        [TestCase("-updateAssemblyInfo")]
        [TestCase("-updateAssemblyInfo -proj foo.sln")]
        [TestCase("-updateAssemblyInfo assemblyInfo.cs")]
        [TestCase("-updateAssemblyInfo assemblyInfo.cs -ensureassemblyinfo")]
        [TestCase("-updateAssemblyInfo assemblyInfo.cs otherAssemblyInfo.cs")]
        [TestCase("-updateAssemblyInfo Assembly.cs Assembly.cs -ensureassemblyinfo")]
        public void UpdateAssemblyInfoTrue(string command)
        {
            var arguments = argumentParser.ParseArguments(command);
            arguments.UpdateAssemblyInfo.ShouldBe(true);
        }

        [TestCase("-updateProjectFiles assemblyInfo.csproj")]
        [TestCase("-updateProjectFiles assemblyInfo.csproj")]
        [TestCase("-updateProjectFiles assemblyInfo.csproj otherAssemblyInfo.fsproj")]
        [TestCase("-updateProjectFiles")]
        public void UpdateProjectTrue(string command)
        {
            var arguments = argumentParser.ParseArguments(command);
            arguments.UpdateProjectFiles.ShouldBe(true);
        }

        [TestCase("-updateAssemblyInfo false")]
        [TestCase("-updateAssemblyInfo 0")]
        public void UpdateAssemblyInfoFalse(string command)
        {
            var arguments = argumentParser.ParseArguments(command);
            arguments.UpdateAssemblyInfo.ShouldBe(false);
        }

        [TestCase("-updateAssemblyInfo Assembly.cs Assembly1.cs -ensureassemblyinfo")]
        public void CreateMulitpleAssemblyInfoProtected(string command)
        {
            var exception = Assert.Throws<WarningException>(() => argumentParser.ParseArguments(command));
            exception.Message.ShouldBe("Can't specify multiple assembly info files when using /ensureassemblyinfo switch, either use a single assembly info file or do not specify /ensureassemblyinfo and create assembly info files manually");
        }

        [TestCase("-updateProjectFiles Assembly.csproj -ensureassemblyinfo")]
        public void UpdateProjectInfoWithEnsureAssemblyInfoProtected(string command)
        {
            var exception = Assert.Throws<WarningException>(() => argumentParser.ParseArguments(command));
            exception.Message.ShouldBe("Cannot specify -ensureassemblyinfo with updateprojectfiles: please ensure your project file exists before attempting to update it");
        }

        [Test]
        public void UpdateAssemblyInfoWithFilename()
        {
            using var repo = new EmptyRepositoryFixture();

            var assemblyFile = Path.Combine(repo.RepositoryPath, "CommonAssemblyInfo.cs");
            using var file = File.Create(assemblyFile);

            var arguments = argumentParser.ParseArguments($"-targetpath {repo.RepositoryPath} -updateAssemblyInfo CommonAssemblyInfo.cs");
            arguments.UpdateAssemblyInfo.ShouldBe(true);
            arguments.UpdateAssemblyInfoFileName.Count.ShouldBe(1);
            arguments.UpdateAssemblyInfoFileName.ShouldContain(x => Path.GetFileName(x).Equals("CommonAssemblyInfo.cs"));
        }

        [Test]
        public void UpdateAssemblyInfoWithMultipleFilenames()
        {
            using var repo = new EmptyRepositoryFixture();

            var assemblyFile1 = Path.Combine(repo.RepositoryPath, "CommonAssemblyInfo.cs");
            using var file = File.Create(assemblyFile1);

            var assemblyFile2 = Path.Combine(repo.RepositoryPath, "VersionAssemblyInfo.cs");
            using var file2 = File.Create(assemblyFile2);

            var arguments = argumentParser.ParseArguments($"-targetpath {repo.RepositoryPath} -updateAssemblyInfo CommonAssemblyInfo.cs VersionAssemblyInfo.cs");
            arguments.UpdateAssemblyInfo.ShouldBe(true);
            arguments.UpdateAssemblyInfoFileName.Count.ShouldBe(2);
            arguments.UpdateAssemblyInfoFileName.ShouldContain(x => Path.GetFileName(x).Equals("CommonAssemblyInfo.cs"));
            arguments.UpdateAssemblyInfoFileName.ShouldContain(x => Path.GetFileName(x).Equals("VersionAssemblyInfo.cs"));
        }

        [Test]
        public void UpdateProjectFilesWithMultipleFilenames()
        {
            using var repo = new EmptyRepositoryFixture();

            var assemblyFile1 = Path.Combine(repo.RepositoryPath, "CommonAssemblyInfo.csproj");
            using var file = File.Create(assemblyFile1);

            var assemblyFile2 = Path.Combine(repo.RepositoryPath, "VersionAssemblyInfo.csproj");
            using var file2 = File.Create(assemblyFile2);

            var arguments = argumentParser.ParseArguments($"-targetpath {repo.RepositoryPath} -updateProjectFiles CommonAssemblyInfo.csproj VersionAssemblyInfo.csproj");
            arguments.UpdateProjectFiles.ShouldBe(true);
            arguments.UpdateAssemblyInfoFileName.Count.ShouldBe(2);
            arguments.UpdateAssemblyInfoFileName.ShouldContain(x => Path.GetFileName(x).Equals("CommonAssemblyInfo.csproj"));
            arguments.UpdateAssemblyInfoFileName.ShouldContain(x => Path.GetFileName(x).Equals("VersionAssemblyInfo.csproj"));
        }

        [Test]
        public void UpdateAssemblyInfoWithMultipleFilenamesMatchingGlobbing()
        {
            using var repo = new EmptyRepositoryFixture();

            var assemblyFile1 = Path.Combine(repo.RepositoryPath, "CommonAssemblyInfo.cs");
            using var file = File.Create(assemblyFile1);

            var assemblyFile2 = Path.Combine(repo.RepositoryPath, "VersionAssemblyInfo.cs");
            using var file2 = File.Create(assemblyFile2);

            var subdir = Path.Combine(repo.RepositoryPath, "subdir");
            Directory.CreateDirectory(subdir);
            var assemblyFile3 = Path.Combine(subdir, "LocalAssemblyInfo.cs");
            using var file3 = File.Create(assemblyFile3);

            var arguments = argumentParser.ParseArguments($"-targetpath {repo.RepositoryPath} -updateAssemblyInfo **/*AssemblyInfo.cs");
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

            var assemblyFile = Path.Combine(repo.RepositoryPath, "CommonAssemblyInfo.cs");
            using var file = File.Create(assemblyFile);

            var targetPath = Path.Combine(repo.RepositoryPath, "subdir1", "subdir2");
            Directory.CreateDirectory(targetPath);

            var arguments = argumentParser.ParseArguments($"-targetpath {targetPath} -updateAssemblyInfo ..\\..\\CommonAssemblyInfo.cs");
            arguments.UpdateAssemblyInfo.ShouldBe(true);
            arguments.UpdateAssemblyInfoFileName.Count.ShouldBe(1);
            arguments.UpdateAssemblyInfoFileName.ShouldContain(x => Path.GetFileName(x).Equals("CommonAssemblyInfo.cs"));
        }

        [Test]
        public void OverrideconfigWithNoOptions()
        {
            var arguments = argumentParser.ParseArguments("/overrideconfig");
            arguments.OverrideConfig.ShouldBeNull();
        }

        [Test]
        public void OverrideconfigWithSingleTagprefixOption()
        {
            var arguments = argumentParser.ParseArguments("/overrideconfig tag-prefix=sample");
            arguments.OverrideConfig.TagPrefix.ShouldBe("sample");
        }

        [TestCase("tag-prefix=sample;tag-prefix=other")]
        [TestCase("tag-prefix=sample;param2=other")]
        public void OverrideconfigWithSeveralOptions(string options)
        {
            var exception = Assert.Throws<WarningException>(() => argumentParser.ParseArguments($"/overrideconfig {options}"));
            exception.Message.ShouldContain("Can't specify multiple /overrideconfig options");
        }

        [TestCase("tag-prefix=sample=asdf")]
        public void OverrideconfigWithInvalidOption(string options)
        {
            var exception = Assert.Throws<WarningException>(() => argumentParser.ParseArguments($"/overrideconfig {options}"));
            exception.Message.ShouldContain("Could not parse /overrideconfig option");
        }

        [Test]
        public void EnsureAssemblyInfoTrueWhenFound()
        {
            var arguments = argumentParser.ParseArguments("-ensureAssemblyInfo");
            arguments.EnsureAssemblyInfo.ShouldBe(true);
        }

        [Test]
        public void EnsureAssemblyInfoTrue()
        {
            var arguments = argumentParser.ParseArguments("-ensureAssemblyInfo true");
            arguments.EnsureAssemblyInfo.ShouldBe(true);
        }

        [Test]
        public void EnsureAssemblyInfoFalse()
        {
            var arguments = argumentParser.ParseArguments("-ensureAssemblyInfo false");
            arguments.EnsureAssemblyInfo.ShouldBe(false);
        }

        [Test]
        public void DynamicRepoLocation()
        {
            var arguments = argumentParser.ParseArguments("-dynamicRepoLocation c:\\foo\\");
            arguments.DynamicRepositoryClonePath.ShouldBe("c:\\foo\\");
        }

        [Test]
        public void CanLogToConsole()
        {
            var arguments = argumentParser.ParseArguments("-l console -proj foo.sln");
            arguments.LogFilePath.ShouldBe("console");
        }

        [Test]
        public void NofetchTrueWhenDefined()
        {
            var arguments = argumentParser.ParseArguments("-nofetch");
            arguments.NoFetch.ShouldBe(true);
        }

        [Test]
        public void NonormilizeTrueWhenDefined()
        {
            var arguments = argumentParser.ParseArguments("-nonormalize");
            arguments.NoNormalize.ShouldBe(true);
        }

        [Test]
        public void OtherArgumentsCanBeParsedBeforeNofetch()
        {
            var arguments = argumentParser.ParseArguments("targetpath -nofetch ");
            arguments.TargetPath.ShouldBe("targetpath");
            arguments.NoFetch.ShouldBe(true);
        }

        [Test]
        public void OtherArgumentsCanBeParsedBeforeNonormalize()
        {
            var arguments = argumentParser.ParseArguments("targetpath -nonormalize");
            arguments.TargetPath.ShouldBe("targetpath");
            arguments.NoNormalize.ShouldBe(true);
        }

        [Test]
        public void OtherArgumentsCanBeParsedBeforeNocache()
        {
            var arguments = argumentParser.ParseArguments("targetpath -nocache");
            arguments.TargetPath.ShouldBe("targetpath");
            arguments.NoCache.ShouldBe(true);
        }

        [Test]
        public void OtherArgumentsCanBeParsedAfterNofetch()
        {
            var arguments = argumentParser.ParseArguments("-nofetch -proj foo.sln");
            arguments.NoFetch.ShouldBe(true);
#pragma warning disable CS0612 // Type or member is obsolete
            arguments.Proj.ShouldBe("foo.sln");
#pragma warning restore CS0612 // Type or member is obsolete
        }

        [Test]
        public void OtherArgumentsCanBeParsedAfterNonormalize()
        {
            var arguments = argumentParser.ParseArguments("-nonormalize -proj foo.sln");
            arguments.NoNormalize.ShouldBe(true);
#pragma warning disable CS0612 // Type or member is obsolete
            arguments.Proj.ShouldBe("foo.sln");
#pragma warning restore CS0612 // Type or member is obsolete
        }

        [Test]
        public void OtherArgumentsCanBeParsedAfterNocache()
        {
            var arguments = argumentParser.ParseArguments("-nocache -proj foo.sln");
            arguments.NoCache.ShouldBe(true);
#pragma warning disable CS0612 // Type or member is obsolete
            arguments.Proj.ShouldBe("foo.sln");
#pragma warning restore CS0612 // Type or member is obsolete
        }

        [TestCase("-nofetch -nonormalize -nocache")]
        [TestCase("-nofetch -nocache -nonormalize")]
        [TestCase("-nocache -nofetch -nonormalize")]
        [TestCase("-nocache -nonormalize -nofetch")]
        [TestCase("-nonormalize -nocache -nofetch")]
        [TestCase("-nonormalize -nofetch -nocache")]
        public void SeveralSwitchesCanBeParsed(string commandLineArgs)
        {
            var arguments = argumentParser.ParseArguments(commandLineArgs);
            arguments.NoCache.ShouldBe(true);
            arguments.NoNormalize.ShouldBe(true);
            arguments.NoFetch.ShouldBe(true);
        }

        [Test]
        public void LogPathCanContainForwardSlash()
        {
            var arguments = argumentParser.ParseArguments("-l /some/path");
            arguments.LogFilePath.ShouldBe("/some/path");
        }

        [Test]
        public void BooleanArgumentHandling()
        {
            var arguments = argumentParser.ParseArguments("/nofetch /updateassemblyinfo true");
            arguments.NoFetch.ShouldBe(true);
            arguments.UpdateAssemblyInfo.ShouldBe(true);
        }

        [Test]
        public void NocacheTrueWhenDefined()
        {
            var arguments = argumentParser.ParseArguments("-nocache");
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
                Assert.Throws<WarningException>(() => argumentParser.ParseArguments(command));
            }
            else
            {
                var arguments = argumentParser.ParseArguments(command);
                arguments.Verbosity.ShouldBe(expectedVerbosity);
            }
        }

        [Test]
        public void EmptyArgumentsRemoteUsernameDefinedSetsUsername()
        {
            environment.SetEnvironmentVariable("GITVERSION_REMOTE_USERNAME", "value");
            var arguments = argumentParser.ParseArguments(string.Empty);
            arguments.Authentication.Username.ShouldBe("value");
        }

        [Test]
        public void EmptyArgumentsRemotePasswordDefinedSetsPassword()
        {
            environment.SetEnvironmentVariable("GITVERSION_REMOTE_PASSWORD", "value");
            var arguments = argumentParser.ParseArguments(string.Empty);
            arguments.Authentication.Password.ShouldBe("value");
        }

        [Test]
        public void ArbitraryArgumentsRemoteUsernameDefinedSetsUsername()
        {
            environment.SetEnvironmentVariable("GITVERSION_REMOTE_USERNAME", "value");
            var arguments = argumentParser.ParseArguments("-nocache");
            arguments.Authentication.Username.ShouldBe("value");
        }

        [Test]
        public void ArbitraryArgumentsRemotePasswordDefinedSetsPassword()
        {
            environment.SetEnvironmentVariable("GITVERSION_REMOTE_PASSWORD", "value");
            var arguments = argumentParser.ParseArguments("-nocache");
            arguments.Authentication.Password.ShouldBe("value");
        }
    }
}
