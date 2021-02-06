using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitTools.Testing;
using GitVersion.BuildAgents;
using GitVersion.Core.Tests.Helpers;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.App.Tests
{
    [TestFixture]
    public class TagCheckoutInBuildAgentTests
    {
        [Test]
        public async Task VerifyTagCheckoutOnGitHubActions()
        {
            var env = new Dictionary<string, string>
            {
                { GitHubActions.EnvironmentVariableName, "true" },
                { "GITHUB_REF", "ref/tags/0.2.0" },
            };

            using var fixture = new EmptyRepositoryFixture();
            var remoteRepositoryPath = PathHelper.GetTempPath();
            RepositoryFixtureBase.Init(remoteRepositoryPath);
            using (var remoteRepository = new Repository(remoteRepositoryPath))
            {
                remoteRepository.Config.Set("user.name", "Test");
                remoteRepository.Config.Set("user.email", "test@email.com");
                fixture.Repository.Network.Remotes.Add("origin", remoteRepositoryPath);
                Console.WriteLine("Created git repository at {0}", remoteRepositoryPath);
                remoteRepository.MakeATaggedCommit("0.1.0");
                Commands.Checkout(remoteRepository, remoteRepository.CreateBranch("develop"));
                remoteRepository.MakeACommit();
                Commands.Checkout(remoteRepository, remoteRepository.CreateBranch("release/0.2.0"));
                remoteRepository.MakeACommit();
                Commands.Checkout(remoteRepository, TestBase.MainBranch);
                remoteRepository.MergeNoFF("release/0.2.0", Generate.SignatureNow());
                remoteRepository.MakeATaggedCommit("0.2.0");

                Commands.Fetch((Repository)fixture.Repository, "origin", new string[0], new FetchOptions(), null);
                Commands.Checkout(fixture.Repository, "0.2.0");
            }

            var programFixture = new ProgramFixture(fixture.RepositoryPath);
            programFixture.WithEnv(env.ToArray());

            var result = await programFixture.Run();

            result.ExitCode.ShouldBe(0);
            result.OutputVariables.FullSemVer.ShouldBe("0.2.0");

            // Cleanup repository files
            DirectoryHelper.DeleteDirectory(remoteRepositoryPath);
        }
    }
}
