using System;
using System.Collections.Generic;
using System.Linq;
using GitTools.Testing;
using GitVersion.BuildServers;
using GitVersionCore.Tests.Helpers;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;

namespace GitVersionExe.Tests
{
    [TestFixture]
    public class PullRequestInBuildAgentTest
    {
        private const string PullRequestBranchName = "PR-5";
        private static readonly string[] PrMergeRefs =
        {
            "refs/pull-requests/5/merge",
            "refs/pull/5/merge",
            "refs/heads/pull/5/head"
        };

        [TestCaseSource(nameof(PrMergeRefs))]
        public void VerifyAzurePipelinesPullRequest(string pullRequestRef)
        {
            var env = new Dictionary<string, string>
            {
                { AzurePipelines.EnvironmentVariableName, "true" },
                { "BUILD_SOURCEBRANCH", PullRequestBranchName }
            };
            VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
        }

        [TestCaseSource(nameof(PrMergeRefs))]
        public void VerifyCodeBuildPullRequest(string pullRequestRef)
        {
            var env = new Dictionary<string, string>
            {
                { CodeBuild.EnvironmentVariableName, PullRequestBranchName },
            };
            VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
        }

        [TestCaseSource(nameof(PrMergeRefs))]
        public void VerifyContinuaCIPullRequest(string pullRequestRef)
        {
            var env = new Dictionary<string, string>
            {
                { ContinuaCi.EnvironmentVariableName, "true" },
            };
            VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
        }


        [TestCaseSource(nameof(PrMergeRefs))]
        public void VerifyDronePullRequest(string pullRequestRef)
        {
            var env = new Dictionary<string, string>
            {
                { Drone.EnvironmentVariableName, "true" },
                { "DRONE_PULL_REQUEST", PullRequestBranchName },
            };
            VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
        }

        [TestCaseSource(nameof(PrMergeRefs))]
        public void VerifyGitHubActionsPullRequest(string pullRequestRef)
        {
            var env = new Dictionary<string, string>
            {
                { GitHubActions.EnvironmentVariableName, "true" },
                { "GITHUB_REF", PullRequestBranchName },
            };
            VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
        }

        [TestCaseSource(nameof(PrMergeRefs))]
        public void VerifyGitLabCIPullRequest(string pullRequestRef)
        {
            var env = new Dictionary<string, string>
            {
                { GitLabCi.EnvironmentVariableName, "true" },
                { "CI_COMMIT_REF_NAME", PullRequestBranchName },
            };
            VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
        }

        [TestCaseSource(nameof(PrMergeRefs))]
        public void VerifyJenkinsPullRequest(string pullRequestRef)
        {
            var env = new Dictionary<string, string>
            {
                { Jenkins.EnvironmentVariableName, "url" },
                 {
                "BRANCH_NAME", PullRequestBranchName }
            };

            VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
        }

        [TestCaseSource(nameof(PrMergeRefs))]
        public void VerifyMyGetPullRequest(string pullRequestRef)
        {
            var env = new Dictionary<string, string>
            {
                { MyGet.EnvironmentVariableName, "MyGet" },
            };

            VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
        }

        [TestCaseSource(nameof(PrMergeRefs))]
        public void VerifyTeamCityPullRequest(string pullRequestRef)
        {
            var env = new Dictionary<string, string>
            {
                { TeamCity.EnvironmentVariableName, "8.0.0" },
            };
            VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
        }

        [TestCaseSource(nameof(PrMergeRefs))]
        public void VerifyTravisCIPullRequest(string pullRequestRef)
        {
            var env = new Dictionary<string, string>
            {
                { TravisCi.EnvironmentVariableName, "true" },
                {  "CI", "true" },
            };
            VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
        }

        private static void VerifyPullRequestVersionIsCalculatedProperly(string pullRequestRef, Dictionary<string, string> env)
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

            var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, environments: env.ToArray());

            result.ExitCode.ShouldBe(0);
            result.OutputVariables.FullSemVer.ShouldBe("1.0.4-PullRequest0005.3");

            // Cleanup repository files
            DirectoryHelper.DeleteDirectory(remoteRepositoryPath);
        }
    }
}
