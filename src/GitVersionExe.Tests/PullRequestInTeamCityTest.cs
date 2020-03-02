using System;
using System.Collections.Generic;
using GitTools.Testing;
using GitVersion.BuildServers;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;
using GitVersionCore.Tests.Helpers;

namespace GitVersionExe.Tests
{
    [TestFixture]
    public class PullRequestInTeamCityTest
    {

        [TestCase("refs/pull-requests/5/merge")]
        [TestCase("refs/pull/5/merge")]
        [TestCase("refs/heads/pull/5/head")]
        public void GivenARemoteWithATagOnMasterAndAPullRequestWithTwoCommitsAndBuildIsRunningInTeamCityVersionIsCalculatedProperly(string pullRequestRef)
        {
            using var fixture = new EmptyRepositoryFixture();
            var remoteRepositoryPath = PathHelper.GetTempPath();
            Repository.Init(remoteRepositoryPath);
            using (var remoteRepository = new Repository(remoteRepositoryPath))
            {
                remoteRepository.Config.Set("user.name", "Test");
                remoteRepository.Config.Set("user.email", "test@email.com");
                fixture.Repository.Network.Remotes.Add("origin", remoteRepositoryPath);
                Console.WriteLine("Created git repository at {0}", remoteRepositoryPath);
                remoteRepository.MakeATaggedCommit("1.0.3");

                var branch = remoteRepository.CreateBranch("FeatureBranch");
                Commands.Checkout(remoteRepository, branch);
                remoteRepository.MakeCommits(2);
                Commands.Checkout(remoteRepository, remoteRepository.Head.Tip.Sha);
                //Emulate merge commit
                var mergeCommitSha = remoteRepository.MakeACommit().Sha;
                Commands.Checkout(remoteRepository, "master"); // HEAD cannot be pointing at the merge commit
                remoteRepository.Refs.Add(pullRequestRef, new ObjectId(mergeCommitSha));

                // Checkout PR commit
                Commands.Fetch((Repository)fixture.Repository, "origin", new string[0], new FetchOptions(), null);
                Commands.Checkout(fixture.Repository, mergeCommitSha);
            }

            var env = new KeyValuePair<string, string>(TeamCity.EnvironmentVariableName, "8.0.0");
            var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, environments: env);

            result.ExitCode.ShouldBe(0);
            result.OutputVariables.FullSemVer.ShouldBe("1.0.4-PullRequest0005.3");

            // Cleanup repository files
            DirectoryHelper.DeleteDirectory(remoteRepositoryPath);
        }
    }
}
