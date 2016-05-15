using System;
using GitTools.Testing;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class PullRequestInTeamCityTest
{

    [TestCase("refs/pull-requests/5/merge")]
    [TestCase("refs/pull/5/merge")]
    [TestCase("refs/heads/pull/5/head")]
    public void GivenARemoteWithATagOnMaster_AndAPullRequestWithTwoCommits_AndBuildIsRunningInTeamCity_VersionIsCalculatedProperly(string pullRequestRef)
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
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
                remoteRepository.Checkout(branch);
                remoteRepository.MakeCommits(2);
                remoteRepository.Checkout(remoteRepository.Head.Tip.Sha);
                //Emulate merge commit
                var mergeCommitSha = remoteRepository.MakeACommit().Sha;
                remoteRepository.Checkout("master"); // HEAD cannot be pointing at the merge commit
                remoteRepository.Refs.Add(pullRequestRef, new ObjectId(mergeCommitSha));

                // Checkout PR commit
                Commands.Fetch((Repository)fixture.Repository, "origin", new string[0], new FetchOptions(), null);
                fixture.Repository.Checkout(mergeCommitSha);
            }

            var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, isTeamCity: true);

            result.ExitCode.ShouldBe(0);
            result.OutputVariables.FullSemVer.ShouldBe("1.0.4-PullRequest.5+3");

            // Cleanup repository files
            DirectoryHelper.DeleteDirectory(remoteRepositoryPath);
        }
    }
}