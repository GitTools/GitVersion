using System;
using System.Collections.Generic;
using System.Linq;
using GitTools.Testing;
using GitVersion.BuildAgents;
using GitVersion.MSBuildTask;
using GitVersionCore.Tests.Helpers;
using GitVersionTask.Tests.Helpers;
using LibGit2Sharp;
using Microsoft.Build.Utilities.ProjectCreation;

namespace GitVersionTask.Tests
{
    public class TestTaskBase : TestBase
    {
        private static IDictionary<string, string> env = new Dictionary<string, string>
        {
            { AzurePipelines.EnvironmentVariableName, "true" },
            { "BUILD_SOURCEBRANCH", null }
        };

        protected static MsBuildTaskFixtureResult<T> ExecuteMsBuildTask<T>(T task) where T : GitVersionTaskBase
        {
            var fixture = CreateLocalRepositoryFixture();
            task.SolutionDirectory = fixture.RepositoryPath;

            var msbuildFixture = new MsBuildTaskFixture(fixture);
            return msbuildFixture.Execute(task);
        }

        protected static MsBuildExeFixtureResult ExecuteMsBuildExe(Action<ProjectCreator> extendProject)
        {
            var fixture = CreateLocalRepositoryFixture();

            var msbuildFixture = new MsBuildExeFixture(fixture, fixture.RepositoryPath);

            msbuildFixture.CreateTestProject(extendProject);

            return msbuildFixture.Execute();
        }

        protected static MsBuildTaskFixtureResult<T> ExecuteMsBuildTaskInBuildServer<T>(T task) where T : GitVersionTaskBase
        {
            var fixture = CreateRemoteRepositoryFixture();
            task.SolutionDirectory = fixture.LocalRepositoryFixture.RepositoryPath;

            var msbuildFixture = new MsBuildTaskFixture(fixture);
            msbuildFixture.WithEnv(env.ToArray());
            return msbuildFixture.Execute(task);
        }

        protected static MsBuildExeFixtureResult ExecuteMsBuildExeInBuildServer(Action<ProjectCreator> extendProject)
        {
            var fixture = CreateRemoteRepositoryFixture();

            var msbuildFixture = new MsBuildExeFixture(fixture, fixture.LocalRepositoryFixture.RepositoryPath);

            msbuildFixture.CreateTestProject(extendProject);
            msbuildFixture.WithEnv(env.ToArray());

            return msbuildFixture.Execute();
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
