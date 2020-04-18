using System.Linq;
using GitVersion.MSBuildTask.Tasks;
using GitVersion.OutputVariables;
using Microsoft.Build.Framework;
using NUnit.Framework;
using Shouldly;

namespace GitVersionTask.Tests
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
            var task = new GetVersion();

            using var result = ExecuteMsBuildTask(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Task.Major.ShouldBe("1");
            result.Task.Minor.ShouldBe("2");
            result.Task.Patch.ShouldBe("4");
            result.Task.MajorMinorPatch.ShouldBe("1.2.4");
            result.Task.FullSemVer.ShouldBe("1.2.4+1");
        }

        [Test]
        public void GetVersionTaskShouldReturnVersionOutputVariablesForBuildServer()
        {
            var task = new GetVersion();

            using var result = ExecuteMsBuildTaskInBuildServer(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Task.Major.ShouldBe("1");
            result.Task.Minor.ShouldBe("0");
            result.Task.Patch.ShouldBe("1");
            result.Task.MajorMinorPatch.ShouldBe("1.0.1");
            result.Task.FullSemVer.ShouldBe("1.0.1+1");
        }
    }
}
