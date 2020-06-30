using System.Collections.Generic;
using System.IO;
using GitTools.Testing;
using GitVersion.BuildAgents;
using GitVersion.OutputVariables;
using Newtonsoft.Json;
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

        [TestCase("", "GitVersion.json")]
        [TestCase("version.json", "version.json")]
        public void BeingOnBuildServerWithOutputJsonAndOutputFileDoesNotFail(string outputFile, string fileName)
        {
            using var fixture = new RemoteRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.2.3");
            fixture.Repository.MakeACommit();

            var env = new KeyValuePair<string, string>(TeamCity.EnvironmentVariableName, "8.0.0");

            var result = GitVersionHelper.ExecuteIn(fixture.LocalRepositoryFixture.RepositoryPath, arguments: $" /output json /output buildserver /output file /outputfile {outputFile}", environments: env);

            result.ExitCode.ShouldBe(0);
            const string version = "0.1.0+4";
            result.Output.ShouldContain($"##teamcity[buildNumber '{version}']");
            result.OutputVariables.ShouldNotBeNull();
            result.OutputVariables.FullSemVer.ShouldBeEquivalentTo(version);

            var filePath = Path.Combine(fixture.LocalRepositoryFixture.RepositoryPath, fileName);
            var json = File.ReadAllText(filePath);

            var outputVariables = VersionVariables.FromDictionary(JsonConvert.DeserializeObject<Dictionary<string, string>>(json));
            outputVariables.ShouldNotBeNull();
            outputVariables.FullSemVer.ShouldBeEquivalentTo(version);
        }
    }
}
