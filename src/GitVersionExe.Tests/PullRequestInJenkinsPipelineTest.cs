using System;
using GitTools.Testing;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;
using GitVersionExe.Tests.Helpers;

namespace GitVersionExe.Tests
{
    [TestFixture]
    public class PullRequestInJenkinsTest
    {
        [TestCase]
        public void GivenJenkinsPipelineHasDuplicatedOriginVersionIsCalculatedProperly()
        {
            var pipelineBranch = "BRANCH_NAME";
            var pipelineBranchOrig = Environment.GetEnvironmentVariable(pipelineBranch);

            using var fixture = new EmptyRepositoryFixture();
            var remoteRepositoryPath = PathHelper.GetTempPath();
            Repository.Init(remoteRepositoryPath);
            using (var remoteRepository = new Repository(remoteRepositoryPath))
            {
                remoteRepository.Config.Set("user.name", "Test");
                remoteRepository.Config.Set("user.email", "test@email.com");
                fixture.Repository.Network.Remotes.Add("origin", remoteRepositoryPath);
                // Jenkins Pipeline will create a duplicate origin:
                fixture.Repository.Network.Remotes.Add("origin1", remoteRepositoryPath);
                Console.WriteLine("Created git repository at {0}", remoteRepositoryPath);
                remoteRepository.MakeATaggedCommit("1.0.3");

                var branch = remoteRepository.CreateBranch("FeatureBranch");
                Commands.Checkout(remoteRepository, branch);
                remoteRepository.MakeCommits(2);
                Commands.Checkout(remoteRepository, remoteRepository.Head.Tip.Sha);
                //Emulate merge commit
                var mergeCommitSha = remoteRepository.MakeACommit().Sha;
                Commands.Checkout(remoteRepository, "master"); // HEAD cannot be pointing at the merge commit
                remoteRepository.Refs.Add("refs/heads/pull/5/head", new ObjectId(mergeCommitSha));

                // Checkout PR commit
                Commands.Fetch((Repository)fixture.Repository, "origin", new string[0], new FetchOptions(), null);
                Commands.Checkout(fixture.Repository, mergeCommitSha);
            }

            // Emulating Jenkins environment variable
            Environment.SetEnvironmentVariable(pipelineBranch, "PR-5");
            Environment.SetEnvironmentVariable("JENKINS_URL", "url");

            var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath);
            
            result.ExitCode.ShouldBe(0);
            result.OutputVariables.FullSemVer.ShouldBe("1.0.4-PullRequest0005.3");

            // Cleanup repository files
            DirectoryHelper.DeleteDirectory(remoteRepositoryPath);

            Environment.SetEnvironmentVariable(pipelineBranch, pipelineBranchOrig);
            Environment.SetEnvironmentVariable("JENKINS_URL", null);
        }
    }
}
