﻿using System;
using System.IO;
using GitTools.Testing;
using GitVersion;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class ExecCmdLineArgumentTest
{
    [Test]
    public void RunExecViaCommandLine()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.MakeATaggedCommit("1.2.3");
            fixture.MakeACommit();

            var buildFile = Path.Combine(fixture.RepositoryPath, "RunExecViaCommandLine.proj");
            File.Delete(buildFile);
            const string buildFileContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Target Name=""OutputResults"">
    <Message Text=""GitVersion_FullSemVer: $(GitVersion_FullSemVer)""/>
  </Target>
</Project>";
            File.WriteAllText(buildFile, buildFileContent);
            var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, SpecifiedArgumentRunner.BuildTool, "RunExecViaCommandLine.proj /target:OutputResults");

            result.ExitCode.ShouldBe(0);
            result.Log.ShouldContain("GitVersion_FullSemVer: 1.2.4+1");
        }
    }


    [Test]
    public void InvalidArgumentsExitCodeShouldNotBeZero()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: " /invalid-argument");

            result.ExitCode.ShouldBe(1);
            result.Output.ShouldContain("Could not parse command line parameter '/invalid-argument'");
        }
    }

    [Test]
    public void LogPathContainsForwardSlash()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.MakeATaggedCommit("1.2.3");
            fixture.MakeACommit();

            var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: @" /l ""/some/path""", logToFile: false);

            result.ExitCode.ShouldBe(0);
            result.Output.ShouldContain(@"""MajorMinorPatch"":""1.2.4""");
        }
    }

    [Test]
    public void WorkingDirectoryWithoutGitFolderCrashesWithInformativeMessage()
    {
        var results = GitVersionHelper.ExecuteIn(Environment.SystemDirectory, null, isTeamCity: false, logToFile: false);
        results.Output.ShouldContain("Can't find the .git directory in");
    }
}