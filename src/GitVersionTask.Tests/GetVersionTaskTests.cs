using System.Linq;
using GitVersion.MSBuildTask.Tasks;
using GitVersion.OutputVariables;
using Microsoft.Build.Framework;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.MSBuildTask.Tests
{
    [TestFixture]
    public class GetVersionTaskTests : TestTaskBase
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
            using var fixture = CreateLocalRepositoryFixture();

            var task = new GetVersion
            {
                SolutionDirectory = fixture.RepositoryPath,
            };

            var result = ExecuteMsBuildTask(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Task.FullSemVer.ShouldBe("1.2.4+1");
        }

        [Test]
        public void GetVersionTaskShouldReturnVersionOutputVariablesForBuildServer()
        {
            using var fixture = CreateRemoteRepositoryFixture();

            var task = new GetVersion
            {
                SolutionDirectory = fixture.LocalRepositoryFixture.RepositoryPath,
            };

            var result = ExecuteMsBuildTaskInBuildServer(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Task.FullSemVer.ShouldBe("1.0.1+1");
        }
    }
}
