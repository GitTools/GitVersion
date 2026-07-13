using GitVersion.Agents;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Helpers;
using GitVersion.Output;
using GitVersion.Testing.Extensions;
using GitVersion.Tests;

namespace GitVersion.App.Tests;

[TestFixture]
public class PullRequestInBuildAgentTest
{
    private const string PullRequestBranchName = "PR-5";
    private static readonly string[] PrMergeRefs =
    [
        "refs/pull-requests/5/merge",
        "refs/pull/5/merge",
        "refs/heads/pull/5/head",
        "refs/remotes/pull/5/merge",
        "refs/remotes/pull-requests/5/merge"
    ];

    private static readonly string[] GitLabMergeRequestRefs =
    [
        "refs/merge-requests/5/head",
        "refs/merge-requests/5/merge"
    ];

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

    [TestCaseSource(nameof(GitLabMergeRequestRefs))]
    public async Task VerifyGitLabCIPullRequest(string mergeRequestRef)
    {
        var env = new Dictionary<string, string>
        {
            { GitLabCi.EnvironmentVariableName, "true" },
            { GitLabCi.MergeRequestRefPathEnvironmentVariableName, mergeRequestRef },
            { GitLabCi.CommitRefNameEnvironmentVariableName, "FeatureBranch" }
        };
        await VerifyGitLabMergeRequestVersionIsCalculatedProperly(mergeRequestRef, env);
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

    private const string GitLabMergeRequestPullRequestConfig = """
        workflow: GitFlow/v1
        branches:
          pull-request:
            regex: ^merge-requests/(?<Number>\d+)/(head|merge)$
        """;

    private static async Task VerifyGitLabMergeRequestVersionIsCalculatedProperly(string mergeRequestRef, Dictionary<string, string> env)
    {
        using var fixture = new EmptyRepositoryFixture();
        var configPath = FileSystemHelper.Path.Combine(fixture.RepositoryPath, "GitVersion.yml");
        await File.WriteAllTextAsync(configPath, GitLabMergeRequestPullRequestConfig);
        await VerifyPullRequestVersionIsCalculatedProperly(fixture, mergeRequestRef, env);
    }

    private static async Task VerifyPullRequestVersionIsCalculatedProperly(string pullRequestRef, Dictionary<string, string> env)
    {
        using var fixture = new EmptyRepositoryFixture();
        await VerifyPullRequestVersionIsCalculatedProperly(fixture, pullRequestRef, env);
    }

    private static async Task VerifyPullRequestVersionIsCalculatedProperly(EmptyRepositoryFixture fixture, string pullRequestRef, Dictionary<string, string> env)
    {
        var remoteRepositoryPath = FileSystemHelper.Path.GetRepositoryTempPath();
        RepositoryFixtureBase.Init(remoteRepositoryPath);
        using var remoteRepository = new TestRepository(remoteRepositoryPath);
        remoteRepository.Config.Set("user.name", "Test");
        remoteRepository.Config.Set("user.email", "test@email.com");
        fixture.Repository.Network.Remotes.Add("origin", remoteRepositoryPath);
        Console.WriteLine("Created git repository at {0}", remoteRepositoryPath);
        remoteRepository.MakeATaggedCommit("1.0.3");

        var branch = remoteRepository.CreateBranch("FeatureBranch");
        Commands.Checkout(remoteRepository, branch);
        remoteRepository.MakeCommits(2);
        Commands.Checkout(remoteRepository, remoteRepository.Head.Tip);
        //Emulate merge commit
        var mergeCommitSha = remoteRepository.MakeACommit().Sha;
        Commands.Checkout(remoteRepository, TestBase.MainBranch); // HEAD cannot be pointing at the merge commit
        remoteRepository.Refs.Add(pullRequestRef, mergeCommitSha);

        // Checkout PR commit
        Commands.Fetch(fixture.Repository, "origin", [], new FetchOptions(), null);
        Commands.Checkout(fixture.Repository, mergeCommitSha);

        var programFixture = new ProgramFixture(fixture.RepositoryPath);
        programFixture.WithOverrides(services =>
        {
            services.AddModule(new GitVersionBuildAgentsModule());
            services.AddModule(new GitVersionOutputModule());
        });
        programFixture.WithEnv([.. env]);

        var result = await programFixture.Run();

        result.ExitCode.ShouldBe(0);
        result.Output.ShouldNotBeNull();
        result.OutputVariables.ShouldNotBeNull();
        result.OutputVariables.FullSemVer.ShouldBe("1.0.4-PullRequest5.3");

        // Cleanup repository files
        FileSystemHelper.Directory.DeleteDirectory(remoteRepositoryPath);
    }

    private static readonly object[] PrMergeRefInputs =
    [
        new object[] { "refs/pull-requests/5/merge", "refs/pull-requests/5/merge", false, true, false },
        new object[] { "refs/pull/5/merge", "refs/pull/5/merge", false, true, false },
        new object[] { "refs/heads/pull/5/head", "pull/5/head", true, false, false },
        new object[] { "refs/remotes/pull/5/merge", "pull/5/merge", false, true, true }
    ];

    [TestCaseSource(nameof(PrMergeRefInputs))]
    public void VerifyPullRequestInput(string pullRequestRef, string friendly, bool isBranch, bool isPullRequest, bool isRemote)
    {
        var refName = new ReferenceName(pullRequestRef);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(refName.Friendly, Is.EqualTo(friendly));
            Assert.That(refName.IsLocalBranch, Is.EqualTo(isBranch));
            Assert.That(refName.IsPullRequest, Is.EqualTo(isPullRequest));
            Assert.That(refName.IsRemoteBranch, Is.EqualTo(isRemote));
        }
    }
}
