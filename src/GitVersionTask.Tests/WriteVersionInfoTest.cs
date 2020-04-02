using System.Collections.Generic;
using System.Linq;
using GitTools.Testing;
using GitVersion.BuildServers;
using GitVersion.MSBuildTask.Tasks;
using GitVersionCore.Tests.Helpers;
using GitVersionTask.Tests.Helpers;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.MSBuildTask.Tests
{
    [TestFixture]
    public class WriteVersionInfoTest : TestBase
    {
        [Test]
        public void WriteVersionInfoTaskShouldNotLogOutputVariablesToBuildOutputIfNotRunningInBuildServer()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeATaggedCommit("1.2.3");
            fixture.MakeACommit();

            var task = new WriteVersionInfoToBuildLog
            {
                SolutionDirectory = fixture.RepositoryPath,
            };

            var msbuildFixture = new MsBuildFixture();
            var result = msbuildFixture.Execute(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Log.ShouldNotContain("##vso[task.setvariable variable=GitVersion.FullSemVer]");
        }

        [Test]
        public void WriteVersionInfoTaskShouldLogOutputVariablesToBuildOutput()
        {
            using var fixture = new RemoteRepositoryFixture();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("develop");

            Commands.Fetch((Repository)fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Network.Remotes.First().Name, new string[0], new FetchOptions(), null);
            Commands.Checkout(fixture.LocalRepositoryFixture.Repository, fixture.Repository.Head.Tip);
            fixture.LocalRepositoryFixture.Repository.Branches.Remove("master");
            fixture.InitializeRepo();

            var task = new WriteVersionInfoToBuildLog
            {
                SolutionDirectory = fixture.LocalRepositoryFixture.RepositoryPath,
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

            result.Log.ShouldContain("##vso[task.setvariable variable=GitVersion.FullSemVer]1.0.1+1");
        }
    }
}
