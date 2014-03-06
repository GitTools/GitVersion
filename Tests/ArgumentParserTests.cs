using System;
using GitVersion;
using NUnit.Framework;

[TestFixture]
public class ArgumentParserTests
{

    [Test]
    public void Empty_means_use_current_directory()
    {
        var arguments = ArgumentParser.ParseArguments("");
        Assert.AreEqual(Environment.CurrentDirectory,arguments.TargetPath);
        Assert.IsNull(arguments.LogFilePath);
        Assert.IsFalse(arguments.IsHelp);
    }

    [Test]
    public void Single_means_use_as_target_directory()
    {
        var arguments = ArgumentParser.ParseArguments("path");
        Assert.AreEqual("path",arguments.TargetPath);
        Assert.IsNull(arguments.LogFilePath);
        Assert.IsFalse(arguments.IsHelp);
    }

    [Test]
    public void No_path_and_logfile_should_use_current_directory_TargetDirectory()
    {
        var arguments = ArgumentParser.ParseArguments("-l logFilePath");
        Assert.AreEqual(Environment.CurrentDirectory,arguments.TargetPath);
        Assert.AreEqual("logFilePath",arguments.LogFilePath);
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
    public void TargetDirectory_and_LogFilePath_can_be_parsed()
    {
        var arguments = ArgumentParser.ParseArguments("targetDirectoryPath -l logFilePath");
        Assert.AreEqual("targetDirectoryPath", arguments.TargetPath);
        Assert.AreEqual("logFilePath",arguments.LogFilePath);
        Assert.IsFalse(arguments.IsHelp);
    }

    [Test]
    public void Wrong_number_of_arguments_should_throw()
    {
        var exception = Assert.Throws<ErrorException>(()=> ArgumentParser.ParseArguments("targetDirectoryPath -l logFilePath extraArg"));
        Assert.AreEqual("Could not parse arguments: 'targetDirectoryPath -l logFilePath extraArg'.", exception.Message);
    }

    [Test]
    public void Unknown_argument_should_throw()
    {
        var exception = Assert.Throws<ErrorException>(()=> ArgumentParser.ParseArguments("targetDirectoryPath -x logFilePath"));
        Assert.AreEqual("Could not parse command line parameter '-x'.", exception.Message);
    }
}