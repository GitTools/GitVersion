using System.Collections.Generic;
using System.Linq;
using GitTools.Testing;
using GitVersion.BuildServers;
using GitVersion.MSBuildTask.Tasks;
using GitVersion.OutputVariables;
using GitVersionCore.Tests.Helpers;
using GitVersionTask.Tests.Helpers;
using Microsoft.Build.Framework;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.MSBuildTask.Tests
{
    [TestFixture]
    public class GetVersionTaskTests : TestBase
    {
        [Test]
        public void OutputsShouldMatchVariableProvider()
        {
            var taskProperties = typeof(GetVersion)
                .GetProperties()
                .Where(p => p.GetCustomAttributes(typeof(OutputAttribute), false).Any())
                .Select(p => p.Name);

            var variablesProperties = VersionVariables.AvailableVariables;

            taskProperties.ShouldBe(variablesProperties, ignoreOrder: true);
        }

        [Test]
        public void GetVersionTaskShouldReturnVersionOutputVariables()
        {
            ResetEnvironment();
            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeATaggedCommit("1.2.3");
            fixture.MakeACommit();

            var task = new GetVersion
            {
                SolutionDirectory = fixture.RepositoryPath,
            };

            var result = MsBuildHelper.Execute(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Task.FullSemVer.ShouldBe("1.2.4+1");
        }

        private static void ResetEnvironment()
        {
            var environmentalVariables = new Dictionary<string, string>
            {
                { TeamCity.EnvironmentVariableName, null },
                { AppVeyor.EnvironmentVariableName, null },
                { TravisCi.EnvironmentVariableName, null },
                { Jenkins.EnvironmentVariableName, null },
                { AzurePipelines.EnvironmentVariableName, null },
                { GitHubActions.EnvironmentVariableName, null },
            };

            foreach (var variable in environmentalVariables)
            {
                System.Environment.SetEnvironmentVariable(variable.Key, variable.Value);
            }
        }
    }
}
