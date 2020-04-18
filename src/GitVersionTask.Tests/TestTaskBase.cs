using System.Collections.Generic;
using System.Linq;
using GitTools.Testing;
using GitVersion.BuildAgents;
using GitVersionCore.Tests.Helpers;
using GitVersionTask.Tests.Helpers;
using LibGit2Sharp;

namespace GitVersion.MSBuildTask.Tests
{
    public class TestTaskBase : TestBase
    {
        private static IDictionary<string, string> env = new Dictionary<string, string>
        {
            { AzurePipelines.EnvironmentVariableName, "true" },
            { "BUILD_SOURCEBRANCH", null }
        };

        protected static MsBuildExecutionResult<T> ExecuteMsBuildTask<T>(T task) where T : GitVersionTaskBase
        {
            using var fixture = CreateLocalRepositoryFixture();
            task.SolutionDirectory = fixture.RepositoryPath;

            var msbuildFixture = new MsBuildFixture();
            return msbuildFixture.Execute(task);
        }

        protected static MsBuildExecutionResult<T> ExecuteMsBuildTaskInBuildServer<T>(T task) where T : GitVersionTaskBase
        {
            using var fixture = CreateRemoteRepositoryFixture();
            task.SolutionDirectory = fixture.LocalRepositoryFixture.RepositoryPath;

            var msbuildFixture = new MsBuildFixture();
            msbuildFixture.WithEnv(env.ToArray());
            return msbuildFixture.Execute(task);
        }

        private static EmptyRepositoryFixture CreateLocalRepositoryFixture()
        {
            var fixture = new EmptyRepositoryFixture();
            fixture.MakeATaggedCommit("1.2.3");
            fixture.MakeACommit();
            return fixture;
        }

        private static RemoteRepositoryFixture CreateRemoteRepositoryFixture()
        {
            var fixture = new RemoteRepositoryFixture();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("develop");

            Commands.Fetch((Repository)fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Network.Remotes.First().Name, new string[0], new FetchOptions(), null);
            Commands.Checkout(fixture.LocalRepositoryFixture.Repository, fixture.Repository.Head.Tip);
            fixture.LocalRepositoryFixture.Repository.Branches.Remove("master");
            fixture.InitializeRepo();
            return fixture;
        }
    }
}
