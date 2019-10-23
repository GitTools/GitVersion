using GitVersion;
using NUnit.Framework;
using Shouldly;
using GitVersion.Exceptions;
using GitVersion.Logging;
using GitVersion.OutputFormatters;

namespace GitVersionExe.Tests
{
    [TestFixture]
    public class ArgumentParserTests
    {
        private IArgumentParser argumentParser;

        [SetUp]
        public void SetUp()
        {
            argumentParser = new ArgumentParser();
        }

        [Test]
        public void Empty_means_use_current_directory()
        {
            var arguments = argumentParser.ParseArguments("");
            arguments.TargetPath.ShouldBe(System.Environment.CurrentDirectory);
            arguments.LogFilePath.ShouldBe(null);
            arguments.IsHelp.ShouldBe(false);
        }

        [Test]
        public void Single_means_use_as_target_directory()
        {
            var arguments = argumentParser.ParseArguments("path");
            arguments.TargetPath.ShouldBe("path");
            arguments.LogFilePath.ShouldBe(null);
            arguments.IsHelp.ShouldBe(false);
        }

        [Test]
        public void No_path_and_logfile_should_use_current_directory_TargetDirectory()
        {
            var arguments = argumentParser.ParseArguments("-l logFilePath");
            arguments.TargetPath.ShouldBe(System.Environment.CurrentDirectory);
            arguments.LogFilePath.ShouldBe("logFilePath");
            arguments.IsHelp.ShouldBe(false);
        }

        [Test]
        public void H_means_IsHelp()
        {
            var arguments = argumentParser.ParseArguments("-h");
            Assert.IsNull(arguments.TargetPath);
            Assert.IsNull(arguments.LogFilePath);
            arguments.IsHelp.ShouldBe(true);
        }

        [Test]
        public void Exec()
        {
            var arguments = argumentParser.ParseArguments("-exec rake");
            arguments.Exec.ShouldBe("rake");
        }

        [Test]
        public void Exec_with_args()
        {
            var arguments = argumentParser.ParseArguments(new []
            {
                "-exec",
                "rake",
                "-execargs",
                "clean build"
            });
            arguments.Exec.ShouldBe("rake");
            arguments.ExecArgs.ShouldBe("clean build");
        }

        [Test]
        public void Msbuild()
        {
            var arguments = argumentParser.ParseArguments("-proj msbuild.proj");
            arguments.Proj.ShouldBe("msbuild.proj");
        }

        [Test]
        public void Msbuild_with_args()
        {
            var arguments = argumentParser.ParseArguments(new []
            {
                "-proj",
                "msbuild.proj",
                "-projargs",
                "/p:Configuration=Debug /p:Platform=AnyCPU"
            });
            arguments.Proj.ShouldBe("msbuild.proj");
            arguments.ProjArgs.ShouldBe("/p:Configuration=Debug /p:Platform=AnyCPU");
        }

        [Test]
        public void Execwith_targetdirectory()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath -exec rake");
            arguments.TargetPath.ShouldBe("targetDirectoryPath");
            arguments.Exec.ShouldBe("rake");
        }

        [Test]
        public void TargetDirectory_and_LogFilePath_can_be_parsed()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath -l logFilePath");
            arguments.TargetPath.ShouldBe("targetDirectoryPath");
            arguments.LogFilePath.ShouldBe("logFilePath");
            arguments.IsHelp.ShouldBe(false);
        }

        [Test]
        public void Username_and_Password_can_be_parsed()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath -u [username] -p [password]");
            arguments.TargetPath.ShouldBe("targetDirectoryPath");
            arguments.Authentication.Username.ShouldBe("[username]");
            arguments.Authentication.Password.ShouldBe("[password]");
            arguments.IsHelp.ShouldBe(false);
        }

        [Test]
        public void Unknown_output_should_throw()
        {
            var exception = Assert.Throws<WarningException>(() => argumentParser.ParseArguments("targetDirectoryPath -output invalid_value"));
            exception.Message.ShouldBe("Value 'invalid_value' cannot be parsed as output type, please use 'json' or 'buildserver'");
        }

        [Test]
        public void Output_defaults_to_json()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath");
            arguments.Output.ShouldBe(OutputType.Json);
        }

        [Test]
        public void Output_json_can_be_parsed()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath -output json");
            arguments.Output.ShouldBe(OutputType.Json);
        }

        [Test]
        public void Output_buildserver_can_be_parsed()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath -output buildserver");
            arguments.Output.ShouldBe(OutputType.BuildServer);
        }

        [Test]
        public void MultipleArgsAndFlag()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath -output buildserver -updateAssemblyInfo");
            arguments.Output.ShouldBe(OutputType.BuildServer);
        }

        [Test]
        public void Url_and_BranchName_can_be_parsed()
        {
            var arguments = argumentParser.ParseArguments("targetDirectoryPath -url http://github.com/Particular/GitVersion.git -b somebranch");
            arguments.TargetPath.ShouldBe("targetDirectoryPath");
            arguments.TargetUrl.ShouldBe("http://github.com/Particular/GitVersion.git");
            arguments.TargetBranch.ShouldBe("somebranch");
            arguments.IsHelp.ShouldBe(false);
        }

        [Test]
        public void Wrong_number_of_arguments_should_throw()
        {
            var exception = Assert.Throws<WarningException>(() => argumentParser.ParseArguments("targetDirectoryPath -l logFilePath extraArg"));
            exception.Message.ShouldBe("Could not parse command line parameter 'extraArg'.");
        }

        [TestCase("targetDirectoryPath -x logFilePath")]
        [TestCase("/invalid-argument")]
        public void Unknown_arguments_should_throw(string arguments)
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
        public void Update_assembly_info_true(string command)
        {
            var arguments = argumentParser.ParseArguments(command);
            arguments.UpdateAssemblyInfo.ShouldBe(true);
        }

        [TestCase("-updateAssemblyInfo false")]
        [TestCase("-updateAssemblyInfo 0")]
        public void Update_assembly_info_false(string command)
        {
            var arguments = argumentParser.ParseArguments(command);
            arguments.UpdateAssemblyInfo.ShouldBe(false);
        }

        [TestCase("-updateAssemblyInfo Assembly.cs Assembly1.cs -ensureassemblyinfo")]
        public void Create_mulitple_assembly_info_protected(string command)
        {
            var exception = Assert.Throws<WarningException>(() => argumentParser.ParseArguments(command));
            exception.Message.ShouldBe("Can't specify multiple assembly info files when using /ensureassemblyinfo switch, either use a single assembly info file or do not specify /ensureassemblyinfo and create assembly info files manually");
        }

        [Test]
        public void Update_assembly_info_with_filename()
        {
            var arguments = argumentParser.ParseArguments("-updateAssemblyInfo CommonAssemblyInfo.cs");
            arguments.UpdateAssemblyInfo.ShouldBe(true);
            arguments.UpdateAssemblyInfoFileName.ShouldContain("CommonAssemblyInfo.cs");
        }

        [Test]
        public void Update_assembly_info_with_multiple_filenames()
        {
            var arguments = argumentParser.ParseArguments("-updateAssemblyInfo CommonAssemblyInfo.cs VersionAssemblyInfo.cs");
            arguments.UpdateAssemblyInfo.ShouldBe(true);
            arguments.UpdateAssemblyInfoFileName.Count.ShouldBe(2);
            arguments.UpdateAssemblyInfoFileName.ShouldContain("CommonAssemblyInfo.cs");
            arguments.UpdateAssemblyInfoFileName.ShouldContain("VersionAssemblyInfo.cs");
        }

        [Test]
        public void Overrideconfig_with_no_options()
        {
            var arguments = argumentParser.ParseArguments("/overrideconfig");
            arguments.HasOverrideConfig.ShouldBe(false);
            arguments.OverrideConfig.ShouldBeNull();
        }

        [Test]
        public void Overrideconfig_with_single_tagprefix_option()
        {
            var arguments = argumentParser.ParseArguments("/overrideconfig tag-prefix=sample");
            arguments.HasOverrideConfig.ShouldBe(true);
            arguments.OverrideConfig.TagPrefix.ShouldBe("sample");
        }

        [TestCase("tag-prefix=sample;tag-prefix=other")]
        [TestCase("tag-prefix=sample;param2=other")]
        public void Overrideconfig_with_several_options(string options)
        {
            var exception = Assert.Throws<WarningException>(() => argumentParser.ParseArguments($"/overrideconfig {options}"));
            exception.Message.ShouldContain("Can't specify multiple /overrideconfig options");
        }

        [TestCase("tag-prefix=sample=asdf")]
        public void Overrideconfig_with_invalid_option(string options)
        {
            var exception = Assert.Throws<WarningException>(() => argumentParser.ParseArguments($"/overrideconfig {options}"));
            exception.Message.ShouldContain("Could not parse /overrideconfig option");
        }

        [Test]
        public void Update_assembly_info_with_relative_filename()
        {
            var arguments = argumentParser.ParseArguments("-updateAssemblyInfo ..\\..\\CommonAssemblyInfo.cs");
            arguments.UpdateAssemblyInfo.ShouldBe(true);
            arguments.UpdateAssemblyInfoFileName.ShouldContain("..\\..\\CommonAssemblyInfo.cs");
        }

        [Test]
        public void Ensure_assembly_info_true_when_found()
        {
            var arguments = argumentParser.ParseArguments("-ensureAssemblyInfo");
            arguments.EnsureAssemblyInfo.ShouldBe(true);
        }

        [Test]
        public void Ensure_assembly_info_true()
        {
            var arguments = argumentParser.ParseArguments("-ensureAssemblyInfo true");
            arguments.EnsureAssemblyInfo.ShouldBe(true);
        }

        [Test]
        public void Ensure_assembly_info_false()
        {
            var arguments = argumentParser.ParseArguments("-ensureAssemblyInfo false");
            arguments.EnsureAssemblyInfo.ShouldBe(false);
        }

        [Test]
        public void DynamicRepoLocation()
        {
            var arguments = argumentParser.ParseArguments("-dynamicRepoLocation c:\\foo\\");
            arguments.DynamicRepositoryLocation.ShouldBe("c:\\foo\\");
        }

        [Test]
        public void Can_log_to_console()
        {
            var arguments = argumentParser.ParseArguments("-l console -proj foo.sln");
            arguments.LogFilePath.ShouldBe("console");
        }

        [Test]
        public void Nofetch_true_when_defined()
        {
            var arguments = argumentParser.ParseArguments("-nofetch");
            arguments.NoFetch.ShouldBe(true);
        }

        [Test]
        public void Nonormilize_true_when_defined()
        {
            var arguments = argumentParser.ParseArguments("-nonormalize");
            arguments.NoNormalize.ShouldBe(true);
        }

        [Test]
        public void Other_arguments_can_be_parsed_before_nofetch()
        {
            var arguments = argumentParser.ParseArguments("targetpath -nofetch ");
            arguments.TargetPath.ShouldBe("targetpath");
            arguments.NoFetch.ShouldBe(true);
        }

        [Test]
        public void Other_arguments_can_be_parsed_after_nofetch()
        {
            var arguments = argumentParser.ParseArguments("-nofetch -proj foo.sln");
            arguments.NoFetch.ShouldBe(true);
            arguments.Proj.ShouldBe("foo.sln");
        }

        [Test]
        public void Log_path_can_contain_forward_slash()
        {
            var arguments = argumentParser.ParseArguments("-l /some/path");
            arguments.LogFilePath.ShouldBe("/some/path");
        }

        [Test]
        public void Boolean_argument_handling()
        {
            var arguments = argumentParser.ParseArguments("/nofetch /updateassemblyinfo true");
            arguments.NoFetch.ShouldBe(true);
            arguments.UpdateAssemblyInfo.ShouldBe(true);
        }

        [Test]
        public void Nocache_true_when_defined()
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
        public void Check_verbosity_parsing(string command, bool shouldThrow, Verbosity expectedVerbosity)
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
    }
}
