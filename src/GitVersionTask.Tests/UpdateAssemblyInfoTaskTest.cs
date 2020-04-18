using System.IO;
using GitVersion.MSBuildTask.Tasks;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.MSBuildTask.Tests
{
    [TestFixture]
    public class UpdateAssemblyInfoTaskTest : TestTaskBase
    {
        [Test]
        public void UpdateAssemblyInfoTaskShouldCreateFile()
        {
            var task = new UpdateAssemblyInfo();

            var result = ExecuteMsBuildTask(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Task.AssemblyInfoTempFilePath.ShouldNotBeNull();

            var fileContent = File.ReadAllText(result.Task.AssemblyInfoTempFilePath);
            fileContent.ShouldContain(@"[assembly: AssemblyVersion(""1.2.4.0"")]");
        }

        [Test]
        public void UpdateAssemblyInfoTaskShouldCreateFileInBuildServer()
        {
            var task = new UpdateAssemblyInfo();

            var result = ExecuteMsBuildTaskInBuildServer(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Task.AssemblyInfoTempFilePath.ShouldNotBeNull();

            var fileContent = File.ReadAllText(result.Task.AssemblyInfoTempFilePath);
            fileContent.ShouldContain(@"[assembly: AssemblyVersion(""1.0.1.0"")]");
        }
    }
}
