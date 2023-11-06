using GitVersion.Agents;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Output;
using LibGit2Sharp;

namespace GitVersion.App.Tests;

[TestFixture]
public class PullRequestInBuildAgentTest
{
    private const string PullRequestBranchName = "PR-5";
    private static readonly string[] PrMergeRefs =
    {
        "refs/pull-requests/5/merge",
        "refs/pull/5/merge",
        "refs/heads/pull/5/head",
        "refs/remotes/pull/5/merge",
        "refs/remotes/pull-requests/5/merge"
    };

    [TestCaseSource(nameof(PrMergeRefs))]
    public async Task VerifyAzurePipelinesPullRequest(string pullRequestRef)
    {
        var env = new Dictionary<string, string>
        {
            { AzurePipelines.EnvironmentVariableName, "true" },
            { "BUILD_SOURCEBRANCH", PullRequestBranchName }
        };
        await VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
    }

    [TestCaseSource(nameof(PrMergeRefs))]
    public async Task VerifyCodeBuildPullRequest(string pullRequestRef)
    {
        var env = new Dictionary<string, string>
        {
            { CodeBuild.WebHookEnvironmentVariableName, PullRequestBranchName }
        };
        await VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
    }

    [TestCaseSource(nameof(PrMergeRefs))]
    public async Task VerifyContinuaCIPullRequest(string pullRequestRef)
    {
        var env = new Dictionary<string, string>
        {
            { ContinuaCi.EnvironmentVariableName, "true" }
        };
        await VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
    }

    [TestCaseSource(nameof(PrMergeRefs))]
    public async Task VerifyDronePullRequest(string pullRequestRef)
    {
        var env = new Dictionary<string, string>
        {
            { Drone.EnvironmentVariableName, "true" },
            { "DRONE_PULL_REQUEST", PullRequestBranchName }
        };
        await VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
    }

    [TestCaseSource(nameof(PrMergeRefs))]
    public async Task VerifyGitHubActionsPullRequest(string pullRequestRef)
    {
        var env = new Dictionary<string, string>
        {
            { GitHubActions.EnvironmentVariableName, "true" },
            { "GITHUB_REF", PullRequestBranchName }
        };
        await VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
    }

    [TestCaseSource(nameof(PrMergeRefs))]
    public async Task VerifyGitLabCIPullRequest(string pullRequestRef)
    {
        var env = new Dictionary<string, string>
        {
            { GitLabCi.EnvironmentVariableName, "true" },
            { "CI_COMMIT_REF_NAME", PullRequestBranchName }
        };
        await VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
    }

    [TestCaseSource(nameof(PrMergeRefs))]
    public async Task VerifyJenkinsPullRequest(string pullRequestRef)
    {
        var env = new Dictionary<string, string>
        {
            { Jenkins.EnvironmentVariableName, "url" },
            {
                "BRANCH_NAME", PullRequestBranchName }
        };

        await VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
    }

    [TestCaseSource(nameof(PrMergeRefs))]
    public async Task VerifyMyGetPullRequest(string pullRequestRef)
    {
        var env = new Dictionary<string, string>
        {
            { MyGet.EnvironmentVariableName, "MyGet" }
        };

        await VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
    }

    [TestCaseSource(nameof(PrMergeRefs))]
    public async Task VerifyTeamCityPullRequest(string pullRequestRef)
    {
        var env = new Dictionary<string, string>
        {
            { TeamCity.EnvironmentVariableName, "8.0.0" }
        };
        await VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
    }

    [TestCaseSource(nameof(PrMergeRefs))]
    public async Task VerifyTravisCIPullRequest(string pullRequestRef)
    {
        var env = new Dictionary<string, string>
        {
            { TravisCi.EnvironmentVariableName, "true" },
            {  "CI", "true" }
        };
        await VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
    }

    [TestCaseSource(nameof(PrMergeRefs))]
    public async Task VerifyBitBucketPipelinesPullRequest(string pullRequestRef)
    {
        var env = new Dictionary<string, string>
        {
            { BitBucketPipelines.EnvironmentVariableName, "MyWorkspace" },
            { BitBucketPipelines.PullRequestEnvironmentVariableName, pullRequestRef }
        };
        await VerifyPullRequestVersionIsCalculatedProperly(pullRequestRef, env);
    }

    private static async Task VerifyPullRequestVersionIsCalculatedProperly(string pullRequestRef, Dictionary<string, string> env)
    {
        using var fixture = new EmptyRepositoryFixture("main");
        var remoteRepositoryPath = PathHelper.GetTempPath();
        RepositoryFixtureBase.Init(remoteRepositoryPath, "main");
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
            Commands.Checkout(remoteRepository, TestBase.MainBranch); // HEAD cannot be pointing at the merge commit
            remoteRepository.Refs.Add(pullRequestRef, new ObjectId(mergeCommitSha));

            // Checkout PR commit
            Commands.Fetch(fixture.Repository, "origin", Array.Empty<string>(), new FetchOptions(), null);
            Commands.Checkout(fixture.Repository, mergeCommitSha);
        }

        var programFixture = new ProgramFixture(fixture.RepositoryPath);
        programFixture.WithOverrides(services =>
        {
            services.AddModule(new GitVersionBuildAgentsModule());
            services.AddModule(new GitVersionOutputModule());
        });
        programFixture.WithEnv(env.ToArray());

        var result = await programFixture.Run();

        result.ExitCode.ShouldBe(0);
        result.OutputVariables.ShouldNotBeNull();
        result.OutputVariables.FullSemVer.ShouldBe("1.0.4-PullRequest5.3");

        // Cleanup repository files
        DirectoryHelper.DeleteDirectory(remoteRepositoryPath);
    }

    private static readonly object[] PrMergeRefInputs =
    {
        new object[] { "refs/pull-requests/5/merge", "refs/pull-requests/5/merge", false, true, false },
        new object[] { "refs/pull/5/merge", "refs/pull/5/merge", false, true, false},
        new object[] { "refs/heads/pull/5/head", "pull/5/head", true, false, false },
        new object[] { "refs/remotes/pull/5/merge", "pull/5/merge", false, true, true },
    };

    [TestCaseSource(nameof(PrMergeRefInputs))]
    public void VerifyPullRequestInput(string pullRequestRef, string friendly, bool isBranch, bool isPullRequest, bool isRemote)
    {
        var refName = new ReferenceName(pullRequestRef);
        Assert.Multiple(() =>
        {
            Assert.That(refName.Friendly, Is.EqualTo(friendly));
            Assert.That(refName.IsLocalBranch, Is.EqualTo(isBranch));
            Assert.That(refName.IsPullRequest, Is.EqualTo(isPullRequest));
            Assert.That(refName.IsRemoteBranch, Is.EqualTo(isRemote));
        });
    }
}
