using System;
using System.Collections.Generic;
using GitVersion;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class ArgumentParserTests
{
    [Test, Explicit]
    public void PrintHelp()
    {
        var p = ArgumentParser.GetOptionSet(new Arguments());
        p.WriteOptionDescriptions(Console.Out);
    }

    [Test]
    public void Empty_means_use_current_directory()
    {
        var arguments = ArgumentParser.ParseArguments("");
        arguments.TargetPath.ShouldBe(Environment.CurrentDirectory);
        arguments.LogFilePath.ShouldBe(null);
        arguments.IsHelp.ShouldBe(false);
    }

    [Test]
    public void Single_means_use_as_target_directory()
    {
        var arguments = ArgumentParser.ParseArguments("path");
        arguments.TargetPath.ShouldBe("path");
        arguments.LogFilePath.ShouldBe(null);
        arguments.IsHelp.ShouldBe(false);
    }

    [Test]
    [TestCase("-l logFilePath")]
    [TestCase("--log=logFilePath")]
    [TestCase("-l=logFilePath")]
    [TestCase("/l logFilePath")]
    [TestCase("/log=logFilePath")]
    public void No_path_and_logfile_should_use_current_directory_TargetDirectory(string args)
    {
        var arguments = ArgumentParser.ParseArguments(args);
        arguments.TargetPath.ShouldBe(Environment.CurrentDirectory);
        arguments.LogFilePath.ShouldBe("logFilePath");
        arguments.IsHelp.ShouldBe(false);
    }

    [Test]
    [TestCase("-h")]
    [TestCase("--help")]
    [TestCase("/h")]
    [TestCase("/help")]
    [TestCase("/?")]
    public void h_means_IsHelp(string helpArg)
    {
        var arguments = ArgumentParser.ParseArguments(helpArg);
        arguments.TargetPath.ShouldBe(null);
        arguments.LogFilePath.ShouldBe(null);
        arguments.IsHelp.ShouldBe(true);
    }

    [Test]
    public void exec()
    {
        var arguments = ArgumentParser.ParseArguments("-exec rake");
        arguments.Exec.ShouldBe("rake");
    }

    [Test]
    public void exec_with_args()
    {
        var arguments = ArgumentParser.ParseArguments(new List<string>
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
    public void msbuild()
    {
        var arguments = ArgumentParser.ParseArguments("-proj msbuild.proj");
        arguments.Proj.ShouldBe("msbuild.proj");
    }

    [Test]
    public void msbuild_with_args()
    {
        var arguments = ArgumentParser.ParseArguments(new List<string>
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
    public void execwith_targetdirectory()
    {
        var arguments = ArgumentParser.ParseArguments("targetDirectoryPath -exec rake");
        arguments.TargetPath.ShouldBe("targetDirectoryPath");
        arguments.Exec.ShouldBe("rake");
    }

    [Test]
    public void TargetDirectory_and_LogFilePath_can_be_parsed()
    {
        var arguments = ArgumentParser.ParseArguments("targetDirectoryPath -l logFilePath");
        arguments.TargetPath.ShouldBe("targetDirectoryPath");
        arguments.LogFilePath.ShouldBe("logFilePath");
        arguments.IsHelp.ShouldBe(false);
    }

    [Test]
    public void Username_and_Password_can_be_parsed()
    {
        var arguments = ArgumentParser.ParseArguments("targetDirectoryPath -u [username] -p [password]");
        arguments.TargetPath.ShouldBe("targetDirectoryPath");
        arguments.Authentication.Username.ShouldBe("[username]");
        arguments.Authentication.Password.ShouldBe("[password]");
        arguments.IsHelp.ShouldBe(false);
    }

    [Test]
    public void Unknown_output_should_throw()
    {
        var exception = Assert.Throws<WarningException>(() => ArgumentParser.ParseArguments("targetDirectoryPath -output invalid_value"));
        exception.Message.ShouldBe("Value 'invalid_value' cannot be parsed as output type, please use 'json' or 'buildserver'");
    }

    [Test]
    public void Output_defaults_to_json()
    {
        var arguments = ArgumentParser.ParseArguments("targetDirectoryPath");
        arguments.Output.ShouldBe(OutputType.Json);
    }

    [Test]
    public void Output_json_can_be_parsed()
    {
        var arguments = ArgumentParser.ParseArguments("targetDirectoryPath -output json");
        arguments.Output.ShouldBe(OutputType.Json);
    }

    [Test]
    public void Output_buildserver_can_be_parsed()
    {
        var arguments = ArgumentParser.ParseArguments("targetDirectoryPath -output buildserver");
        arguments.Output.ShouldBe(OutputType.BuildServer);
    }

    [Test]
    public void MultipleArgsAndFlag()
    {
        var arguments = ArgumentParser.ParseArguments("targetDirectoryPath -output buildserver -updateAssemblyInfo");
        arguments.Output.ShouldBe(OutputType.BuildServer);
    }

    [Test]
    public void Url_and_BranchName_can_be_parsed()
    {
        var arguments = ArgumentParser.ParseArguments("targetDirectoryPath -url http://github.com/Particular/GitVersion.git -b somebranch");
        arguments.TargetPath.ShouldBe("targetDirectoryPath");
        arguments.TargetUrl.ShouldBe("http://github.com/Particular/GitVersion.git");
        arguments.TargetBranch.ShouldBe("somebranch");
        arguments.IsHelp.ShouldBe(false);
    }

    [Test]
    public void Wrong_number_of_arguments_should_throw()
    {
        var exception = Assert.Throws<WarningException>(() => ArgumentParser.ParseArguments("targetDirectoryPath -l logFilePath extraArg"));
        exception.Message.ShouldBe("Could not parse command line parameter 'extraArg'.");
    }

    [Test]
    public void Unknown_argument_should_throw()
    {
        var exception = Assert.Throws<WarningException>(() => ArgumentParser.ParseArguments("targetDirectoryPath -x logFilePath"));
        exception.Message.ShouldBe("Could not parse command line parameter '-x'.");
    }

    [TestCase("-updateAssemblyInfo")]
    [TestCase("-updateAssemblyInfo+")]      // plus added to flag indicates true
    [TestCase("-updateAssemblyInfo -proj foo.sln")]
    [TestCase("-updateAssemblyInfo true")]  // bogus test: flags should not be implemented this way, breaking change
    [TestCase("-updateAssemblyInfo 1")]     // bogus test: flags should not be implemented this way, breaking change
    public void update_assembly_info_true(string command)
    {
        var arguments = ArgumentParser.ParseArguments(command);
        arguments.UpdateAssemblyInfo.ShouldBe(true);
    }

    [TestCase("-proj foo.sln")]             // absent updateAssemblyInfo flag implies false
    [TestCase("-updateAssemblyInfo-")]      // minus switch added to flag indicates explicit false value for flag
    [TestCase("-updateAssemblyInfo false")] // bogus test: flags should not be implemented this way, breaking change
    [TestCase("-updateAssemblyInfo 0")]     // bogus test: flags should not be implemented this way, breaking change
    public void update_assembly_info_false(string command)
    {
        var arguments = ArgumentParser.ParseArguments(command);
        arguments.UpdateAssemblyInfo.ShouldBe(false);
    }

    // how to do switch-and-value options?
    [Test]
    public void update_assembly_info_with_filename()
    {
        var arguments = ArgumentParser.ParseArguments("-updateAssemblyInfo CommonAssemblyInfo.cs");
        arguments.UpdateAssemblyInfo.ShouldBe(true);
        arguments.UpdateAssemblyInfoFileName.ShouldBe("CommonAssemblyInfo.cs");
    }

    [Test]
    public void update_assembly_info_with_relative_filename()
    {
        var arguments = ArgumentParser.ParseArguments("-updateAssemblyInfo ..\\..\\CommonAssemblyInfo.cs");
        arguments.UpdateAssemblyInfo.ShouldBe(true);
        arguments.UpdateAssemblyInfoFileName.ShouldBe("..\\..\\CommonAssemblyInfo.cs");
    }

    [Test]
    public void dynamicRepoLocation()
    {
        var arguments = ArgumentParser.ParseArguments("-dynamicRepoLocation c:\\foo\\");
        arguments.DynamicRepositoryLocation.ShouldBe("c:\\foo\\");
    }

    [Test]
    public void can_log_to_console()
    {
        var arguments = ArgumentParser.ParseArguments("-l console -proj foo.sln");
        arguments.LogFilePath.ShouldBe("console");
    }

    [Test]
    public void nofetch_true_when_defined()
    {
        var arguments = ArgumentParser.ParseArguments("-nofetch");
        arguments.NoFetch = true;
    }

    [Test]
    public void other_arguments_can_be_parsed_before_nofetch()
    {
        var arguments = ArgumentParser.ParseArguments("targetpath -nofetch ");
        arguments.TargetPath = "targetpath";
        arguments.NoFetch = true;
    }

    [Test]
    public void other_arguments_can_be_parsed_after_nofetch()
    {
        var arguments = ArgumentParser.ParseArguments("-nofetch -proj foo.sln");
        arguments.NoFetch = true;
        arguments.Proj = "foo.sln";
    }
}