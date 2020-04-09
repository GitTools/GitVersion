using System.Collections.Generic;
using GitTools.Testing;
using GitVersion.BuildAgents;
using NUnit.Framework;
using Shouldly;

namespace GitVersionExe.Tests
{
    public class JsonOutputOnBuildServerTest
    {
        [Test]
        public void BeingOnBuildServerDoesntOverrideOutputJson()
        {
            using var fixture = new RemoteRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.2.3");
            fixture.Repository.MakeACommit();

            var env = new KeyValuePair<string, string>(TeamCity.EnvironmentVariableName, "8.0.0");

            var result = GitVersionHelper.ExecuteIn(fixture.LocalRepositoryFixture.RepositoryPath, arguments: " /output json", environments: env);

            result.ExitCode.ShouldBe(0);
            result.Output.ShouldStartWith("{");
            result.Output.TrimEnd().ShouldEndWith("}");
        }

        [Test]
        public void BeingOnBuildServerWithOutputJsonDoesNotFail()
        {
            using var fixture = new RemoteRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.2.3");
            fixture.Repository.MakeACommit();

            var env = new KeyValuePair<string, string>(TeamCity.EnvironmentVariableName, "8.0.0");

            var result = GitVersionHelper.ExecuteIn(fixture.LocalRepositoryFixture.RepositoryPath, arguments: " /output json /output buildserver", environments: env);

            result.ExitCode.ShouldBe(0);
            const string version = "0.1.0+4";
            result.Output.ShouldContain($"##teamcity[buildNumber '{version}']");
            result.OutputVariables.ShouldNotBeNull();
            result.OutputVariables.FullSemVer.ShouldBeEquivalentTo(version);
        }
    }
}
