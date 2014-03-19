using System;
using System.Collections.Generic;
using GitVersion;
using NUnit.Framework;

[TestFixture]
public class ArgumentParserTests
{

    [Test]
    public void Empty_means_use_current_directory()
    {
        var arguments = ArgumentParser.ParseArguments("");
        Assert.AreEqual(Environment.CurrentDirectory, arguments.TargetPath);
        Assert.IsNull(arguments.LogFilePath);
        Assert.IsFalse(arguments.IsHelp);
    }

    [Test]
    public void Single_means_use_as_target_directory()
    {
        var arguments = ArgumentParser.ParseArguments("path");
        Assert.AreEqual("path", arguments.TargetPath);
        Assert.IsNull(arguments.LogFilePath);
        Assert.IsFalse(arguments.IsHelp);
    }

    [Test]
    public void No_path_and_logfile_should_use_current_directory_TargetDirectory()
    {
        var arguments = ArgumentParser.ParseArguments("-l logFilePath");
        Assert.AreEqual(Environment.CurrentDirectory, arguments.TargetPath);
        Assert.AreEqual("logFilePath", arguments.LogFilePath);
        Assert.IsFalse(arguments.IsHelp);
    }

    [Test]
    public void h_means_IsHelp()
    {
        var arguments = ArgumentParser.ParseArguments("-h");
        Assert.IsNull(arguments.TargetPath);
        Assert.IsNull(arguments.LogFilePath);
        Assert.IsTrue(arguments.IsHelp);
    }

    [Test]
    public void exec()
    {
        var arguments = ArgumentParser.ParseArguments("-exec rake");
        Assert.AreEqual("rake", arguments.Exec);
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
        Assert.AreEqual("rake", arguments.Exec);
        Assert.AreEqual("clean build", arguments.ExecArgs);
    }

    [Test]
    public void msbuild()
    {
        var arguments = ArgumentParser.ParseArguments("-proj msbuild.proj");
        Assert.AreEqual("msbuild.proj", arguments.Proj);
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
        Assert.AreEqual("msbuild.proj", arguments.Proj);
        Assert.AreEqual("/p:Configuration=Debug /p:Platform=AnyCPU", arguments.ProjArgs);
    }

    [Test]
    public void execwith_targetdirectory()
    {
        var arguments = ArgumentParser.ParseArguments("targetDirectoryPath -exec rake");
        Assert.AreEqual("targetDirectoryPath", arguments.TargetPath);
        Assert.AreEqual("rake", arguments.Exec);
    }

    [Test]
    public void TargetDirectory_and_LogFilePath_can_be_parsed()
    {
        var arguments = ArgumentParser.ParseArguments("targetDirectoryPath -l logFilePath");
        Assert.AreEqual("targetDirectoryPath", arguments.TargetPath);
        Assert.AreEqual("logFilePath", arguments.LogFilePath);
        Assert.IsFalse(arguments.IsHelp);
    }

    [Test]
    public void Username_and_Password_can_be_parsed()
    {
        var arguments = ArgumentParser.ParseArguments("targetDirectoryPath -u [username] -p [password]");
        Assert.AreEqual("targetDirectoryPath", arguments.TargetPath);
        Assert.AreEqual("[username]", arguments.Username);
        Assert.AreEqual("[password]", arguments.Password);
        Assert.IsFalse(arguments.IsHelp);
    }

    [Test]
    public void Unknown_output_should_throw()
    {
        var exception = Assert.Throws<ErrorException>(() => ArgumentParser.ParseArguments("targetDirectoryPath -output invalid_value"));
        Assert.AreEqual("Value 'invalid_value' cannot be parsed as output type, please use 'json' or 'buildserver'", exception.Message);
    }

    [Test]
    public void Output_defaults_to_json()
    {
        var arguments = ArgumentParser.ParseArguments("targetDirectoryPath");
        Assert.AreEqual(OutputType.Json, arguments.Output);
    }

    [Test]
    public void Output_json_can_be_parsed()
    {
        var arguments = ArgumentParser.ParseArguments("targetDirectoryPath -output json");
        Assert.AreEqual(OutputType.Json, arguments.Output);
    }

    [Test]
    public void Output_buildserver_can_be_parsed()
    {
        var arguments = ArgumentParser.ParseArguments("targetDirectoryPath -output buildserver");
        Assert.AreEqual(OutputType.BuildServer, arguments.Output);
    }

    [Test]
    public void Url_and_BranchName_can_be_parsed()
    {
        var arguments = ArgumentParser.ParseArguments("targetDirectoryPath -url http://github.com/Particular/GitVersion.git -b somebranch");
        Assert.AreEqual("targetDirectoryPath", arguments.TargetPath);
        Assert.AreEqual("http://github.com/Particular/GitVersion.git", arguments.TargetUrl);
        Assert.AreEqual("somebranch", arguments.TargetBranch);
        Assert.IsFalse(arguments.IsHelp);
    }

    [Test]
    public void Wrong_number_of_arguments_should_throw()
    {
        var exception = Assert.Throws<ErrorException>(() => ArgumentParser.ParseArguments("targetDirectoryPath -l logFilePath extraArg"));
        Assert.AreEqual("Could not parse arguments: 'targetDirectoryPath -l logFilePath extraArg'.", exception.Message);
    }

    [Test]
    public void Unknown_argument_should_throw()
    {
        var exception = Assert.Throws<ErrorException>(() => ArgumentParser.ParseArguments("targetDirectoryPath -x logFilePath"));
        Assert.AreEqual("Could not parse command line parameter '-x'.", exception.Message);
    }
}