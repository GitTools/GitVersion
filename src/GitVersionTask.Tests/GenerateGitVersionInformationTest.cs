using System.Collections.Generic;
using System.IO;
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
    public class GenerateGitVersionInformationTest : TestBase
    {
        [Test]
        public void GenerateGitVersionInformationTaskShouldCreateFile()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeATaggedCommit("1.2.3");
            fixture.MakeACommit();

            var task = new GenerateGitVersionInformation
            {
                SolutionDirectory = fixture.RepositoryPath,
                ProjectFile = fixture.RepositoryPath,
            };

            var msbuildFixture = new MsBuildFixture();
            var result = msbuildFixture.Execute(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Task.GitVersionInformationFilePath.ShouldNotBeNull();

            var fileContent = File.ReadAllText(result.Task.GitVersionInformationFilePath);
            fileContent.ShouldContain(@"FullSemVer = ""1.2.4+1""");
        }

        [Test]
        public void GenerateGitVersionInformationTaskShouldCreateFileWhenRunningInBuildServer()
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

            var task = new GenerateGitVersionInformation
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
            result.Task.GitVersionInformationFilePath.ShouldNotBeNull();

            var fileContent = File.ReadAllText(result.Task.GitVersionInformationFilePath);
            fileContent.ShouldContain(@"FullSemVer = ""1.0.1+1""");
        }
    }
}
