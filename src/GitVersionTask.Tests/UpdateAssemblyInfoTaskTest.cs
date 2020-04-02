using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitVersion.BuildServers;
using GitVersion.MSBuildTask.Tasks;
using GitVersionTask.Tests.Helpers;
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
            using var fixture = CreateLocalRepositoryFixture();

            var task = new UpdateAssemblyInfo
            {
                SolutionDirectory = fixture.RepositoryPath,
                ProjectFile = fixture.RepositoryPath,
            };

            var msbuildFixture = new MsBuildFixture();
            var result = msbuildFixture.Execute(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Task.AssemblyInfoTempFilePath.ShouldNotBeNull();

            var fileContent = File.ReadAllText(result.Task.AssemblyInfoTempFilePath);
            fileContent.ShouldContain(@"[assembly: AssemblyVersion(""1.2.4.0"")]");
        }

        [Test]
        public void UpdateAssemblyInfoTaskShouldCreateFileWhenRunningInBuildServer()
        {
            using var fixture = CreateRemoteRepositoryFixture();

            var task = new UpdateAssemblyInfo
            {
                SolutionDirectory = fixture.LocalRepositoryFixture.RepositoryPath,
                ProjectFile = fixture.LocalRepositoryFixture.RepositoryPath,
            };

            var env = new Dictionary<string, string>
            {
                { AzurePipelines.EnvironmentVariableName, "true" }
            };

            var msbuildFixture = new MsBuildFixture();
            msbuildFixture.WithEnv(env.ToArray());
            var result = msbuildFixture.Execute(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Task.AssemblyInfoTempFilePath.ShouldNotBeNull();

            var fileContent = File.ReadAllText(result.Task.AssemblyInfoTempFilePath);
            fileContent.ShouldContain(@"[assembly: AssemblyVersion(""1.0.1.0"")]");
        }
    }
}
