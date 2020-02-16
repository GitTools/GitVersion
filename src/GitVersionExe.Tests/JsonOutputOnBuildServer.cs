using System.Collections.Generic;
using GitTools.Testing;
using GitVersion.BuildServers;
using NUnit.Framework;
using Shouldly;

namespace GitVersionExe.Tests
{
    public class JsonOutputOnBuildServer
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
    }
}
