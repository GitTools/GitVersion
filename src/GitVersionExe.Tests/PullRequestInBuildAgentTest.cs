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
    public class PullRequestInBuildAgentTest
    {
        private const string PullRequestBranchName = "PR-5";

        [TestCase("refs/pull-requests/5/merge")]
        [TestCase("refs/pull/5/merge")]
        [TestCase("refs/heads/pull/5/head")]
        public void VerifyAzurePipelinesPullRequest(string pullRequestRef)
        {
            var env = new[]
            {
                new KeyValuePair<string, string>(AzurePipelines.EnvironmentVariableName, "true"),
                new KeyValuePair<string, string>("BUILD_SOURCEBRANCH", PullRequestBranchName)
            };
            VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
        }

        [TestCase("refs/pull-requests/5/merge")]
        [TestCase("refs/pull/5/merge")]
        [TestCase("refs/heads/pull/5/head")]
        public void VerifyCodeBuildPullRequest(string pullRequestRef)
        {
            var env = new[]
            {
                new KeyValuePair<string, string>(CodeBuild.EnvironmentVariableName, PullRequestBranchName),
            };
            VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
        }

        [TestCase("refs/pull-requests/5/merge")]
        [TestCase("refs/pull/5/merge")]
        [TestCase("refs/heads/pull/5/head")]
        public void VerifyContinuaCIPullRequest(string pullRequestRef)
        {
            var env = new[]
            {
                new KeyValuePair<string, string>(ContinuaCi.EnvironmentVariableName, "true"),
            };
            VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
        }


        [TestCase("refs/pull-requests/5/merge")]
        [TestCase("refs/pull/5/merge")]
        [TestCase("refs/heads/pull/5/head")]
        public void VerifyDronePullRequest(string pullRequestRef)
        {
            var env = new[]
            {
                new KeyValuePair<string, string>(Drone.EnvironmentVariableName, "true"),
                new KeyValuePair<string, string>("DRONE_PULL_REQUEST", PullRequestBranchName),
            };
            VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
        }

        [TestCase("refs/pull-requests/5/merge")]
        [TestCase("refs/pull/5/merge")]
        [TestCase("refs/heads/pull/5/head")]
        public void VerifyGitHubActionsPullRequest(string pullRequestRef)
        {
            var env = new[]
            {
                new KeyValuePair<string, string>(GitHubActions.EnvironmentVariableName, "true"),
                new KeyValuePair<string, string>("GITHUB_REF", PullRequestBranchName),
            };
            VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
        }

        [TestCase("refs/pull-requests/5/merge")]
        [TestCase("refs/pull/5/merge")]
        [TestCase("refs/heads/pull/5/head")]
        public void VerifyGitLabCIPullRequest(string pullRequestRef)
        {
            var env = new[]
            {
                new KeyValuePair<string, string>(GitLabCi.EnvironmentVariableName, "true"),
                new KeyValuePair<string, string>("CI_COMMIT_REF_NAME", PullRequestBranchName),
            };
            VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
        }

        [TestCase("refs/pull-requests/5/merge")]
        [TestCase("refs/pull/5/merge")]
        [TestCase("refs/heads/pull/5/head")]
        public void VerifyJenkinsPullRequest(string pullRequestRef)
        {
            var env = new[]
            {
                new KeyValuePair<string, string>(Jenkins.EnvironmentVariableName, "url"),
                new KeyValuePair<string, string>("BRANCH_NAME", PullRequestBranchName)
            };

            VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
        }

        [TestCase("refs/pull-requests/5/merge")]
        [TestCase("refs/pull/5/merge")]
        [TestCase("refs/heads/pull/5/head")]
        public void VerifyMyGetPullRequest(string pullRequestRef)
        {
            var env = new[]
            {
                new KeyValuePair<string, string>(MyGet.EnvironmentVariableName, "MyGet"),
            };

            VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
        }

        [TestCase("refs/pull-requests/5/merge")]
        [TestCase("refs/pull/5/merge")]
        [TestCase("refs/heads/pull/5/head")]
        public void VerifyTeamCityPullRequest(string pullRequestRef)
        {
            var env = new[]
            {
                new KeyValuePair<string, string>(TeamCity.EnvironmentVariableName, "8.0.0"),
            };
            VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
        }

        [TestCase("refs/pull-requests/5/merge")]
        [TestCase("refs/pull/5/merge")]
        [TestCase("refs/heads/pull/5/head")]
        public void VerifyTravisCIPullRequest(string pullRequestRef)
        {
            var env = new[]
            {
                new KeyValuePair<string, string>(TravisCi.EnvironmentVariableName, "true"),
                new KeyValuePair<string, string>("CI", "true"),
            };
            VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
        }

        private static void VerifyPullRequestVersionIsCalculatedProperly(string pullRequestRef, params KeyValuePair<string, string>[] env)
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

            var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, environments: env);

            result.ExitCode.ShouldBe(0);
            result.OutputVariables.FullSemVer.ShouldBe("1.0.4-PullRequest0005.3");

            // Cleanup repository files
            DirectoryHelper.DeleteDirectory(remoteRepositoryPath);
        }
    }
}
