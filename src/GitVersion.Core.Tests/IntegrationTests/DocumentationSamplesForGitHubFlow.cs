using GitVersion.Configuration;
using GitVersion.VersionCalculation;
using LibGit2Sharp;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class DocumentationSamplesForGitHubFlow
{
    [TestCase(false)]
    [TestCase(true)]
    public void FeatureBranch(bool withPullRequestIntoMain)
    {
        var configuration = GitHubFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();

        // GitFlow setup
        fixture.SequenceDiagram.Activate("main");
        fixture.Repository.MakeACommit();
        fixture.ApplyTag("1.2.0");
        fixture.AssertFullSemver("1.2.0", configuration);

        // Branch from main
        const string branchName = "feature/foo";
        fixture.BranchTo(branchName, "feature");
        fixture.SequenceDiagram.Activate("feature");
        fixture.SequenceDiagram.Deactivate("main");
        fixture.AssertFullSemver("1.2.1-foo.1+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.1-foo.1+1", configuration);

        // Create hotfix on main branch
        fixture.Checkout("main");
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.1-1", configuration);
        fixture.ApplyTag("1.2.1");
        fixture.AssertFullSemver("1.2.1", configuration);

        // Merge main to feature branch
        fixture.MergeTo(branchName);
        fixture.SequenceDiagram.Deactivate("main");
        fixture.AssertFullSemver("1.2.2-foo.1+2", configuration);

        // Bump to minor version increment
        fixture.MakeACommit("+semver: minor");
        fixture.AssertFullSemver("1.3.0-foo.1+3", configuration);

        // Create pre-release on feature branch
        fixture.ApplyTag("2.0.0-foo.1");
        fixture.AssertFullSemver("2.0.0-foo.2+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.0.0-foo.2+1", configuration);
        fixture.Checkout("main");

        if (withPullRequestIntoMain)
        {
            // Create a PullRequest into main
            fixture.BranchTo("pull/2/merge", "pull");
            fixture.SequenceDiagram.Activate("pull/2/merge");
            fixture.MergeNoFF(branchName);
            fixture.AssertFullSemver("2.0.0-PullRequest2.5", configuration);
            fixture.Checkout("main");
            fixture.Remove("pull/2/merge");
        }

        // Merge feature into main branch
        fixture.MergeNoFF(branchName);
        fixture.Remove(branchName);
        fixture.SequenceDiagram.NoteOver("Feature branches should\r\nbe deleted once merged", branchName);
        fixture.AssertFullSemver("2.0.0-5", configuration);

        // Commit on main branch
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.0.0-6", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FeatureBranchWithMainline(bool withPullRequestIntoMain)
    {
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithNextVersion("1.2.0")
            .WithVersionStrategies(VersionStrategies.ConfiguredNextVersion, VersionStrategies.Mainline)
            .Build();

        using var fixture = new EmptyRepositoryFixture();

        // GitFlow setup
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.0-1", configuration);
        fixture.Repository.ApplyTag("1.2.0");

        // Branch from main
        const string branchName = "feature/foo";
        fixture.BranchTo(branchName, "feature");
        fixture.SequenceDiagram.Activate("feature");
        fixture.SequenceDiagram.Deactivate("main");
        fixture.AssertFullSemver("1.2.1-foo.1+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.1-foo.1+1", configuration);

        // Create hotfix on main branch
        fixture.Checkout("main");
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.1-1", configuration);

        // Merge main to feature branch
        fixture.MergeTo(branchName);
        fixture.SequenceDiagram.Deactivate("main");
        fixture.AssertFullSemver("1.2.2-foo.1+2", configuration);

        // Bump to minor version increment
        fixture.MakeACommit("+semver: minor");
        fixture.AssertFullSemver("1.3.0-foo.1+3", configuration);

        // Create pre-release on feature branch
        fixture.ApplyTag("2.0.0-foo.1");
        fixture.AssertFullSemver("2.0.0-foo.2+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.0.0-foo.2+1", configuration);
        fixture.Checkout("main");

        if (withPullRequestIntoMain)
        {
            // Create a PullRequest into main
            fixture.BranchTo("pull/2/merge", "pull");
            fixture.SequenceDiagram.Activate("pull/2/merge");
            fixture.MergeNoFF(branchName);
            fixture.AssertFullSemver("2.0.0-PullRequest2.5", configuration);
            fixture.Checkout("main");
            fixture.Remove("pull/2/merge");
        }

        // Merge feature into main branch
        fixture.MergeNoFF(branchName);
        fixture.Remove(branchName);
        fixture.SequenceDiagram.NoteOver("Feature branches should\r\nbe deleted once merged", branchName);
        fixture.AssertFullSemver("2.0.0-5", configuration);

        // Commit on main branch
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.0.1-1", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReleaseBranch(bool withPullRequestIntoMain)
    {
        var configuration = GitHubFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();

        // GitFlow setup
        fixture.SequenceDiagram.Activate("main");
        fixture.Repository.MakeACommit();
        fixture.ApplyTag("1.2.0");
        fixture.AssertFullSemver("1.2.0", configuration);

        // Branch from main
        const string branchName = "release/next";
        fixture.BranchTo(branchName, "release");
        fixture.SequenceDiagram.Activate("release");
        fixture.SequenceDiagram.Deactivate("main");
        fixture.AssertFullSemver("1.2.1-beta.1+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.1-beta.1+1", configuration);

        // Create hotfix on main branch
        fixture.Checkout("main");
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.1-1", configuration);
        fixture.ApplyTag("1.2.1");
        fixture.AssertFullSemver("1.2.1", configuration);

        // Merge main to release branch
        fixture.MergeTo(branchName);
        fixture.SequenceDiagram.Deactivate("main");
        fixture.AssertFullSemver("1.2.2-beta.1+2", configuration);

        // Bump to minor version increment
        fixture.MakeACommit("+semver: minor");
        fixture.AssertFullSemver("1.3.0-beta.1+3", configuration);

        // Create pre-release on release branch
        fixture.ApplyTag("1.3.1-beta.1");
        fixture.AssertFullSemver("1.3.1-beta.2+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.3.1-beta.2+1", configuration);
        fixture.Checkout("main");

        if (withPullRequestIntoMain)
        {
            // Create a PullRequest into main
            fixture.BranchTo("pull/2/merge", "pull");
            fixture.SequenceDiagram.Activate("pull/2/merge");
            fixture.MergeNoFF(branchName);
            fixture.AssertFullSemver("1.3.1-PullRequest2.5", configuration);
            fixture.Checkout("main");
            fixture.Remove("pull/2/merge");
        }

        // Merge release into main branch
        fixture.MergeNoFF(branchName);
        fixture.Remove(branchName);
        fixture.SequenceDiagram.NoteOver("Release branches should\r\nbe deleted once merged", branchName);
        fixture.AssertFullSemver("1.3.1-5", configuration);

        // Commit on main branch
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.3.1-6", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReleaseBranchWithMainline(bool withPullRequestIntoMain)
    {
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithNextVersion("1.2.0")
            .WithVersionStrategies(VersionStrategies.ConfiguredNextVersion, VersionStrategies.Mainline)
            .Build();

        using var fixture = new EmptyRepositoryFixture();

        // GitFlow setup
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.0-1", configuration);
        fixture.Repository.ApplyTag("1.2.0");

        // Branch from main
        const string branchName = "release/next";
        fixture.BranchTo(branchName, "release");
        fixture.SequenceDiagram.Activate("release");
        fixture.SequenceDiagram.Deactivate("main");
        fixture.AssertFullSemver("1.2.1-beta.1+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.1-beta.1+1", configuration);

        // Create hotfix on main branch
        fixture.Checkout("main");
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.1-1", configuration);

        // Merge main to release branch
        fixture.MergeTo(branchName);
        fixture.SequenceDiagram.Deactivate("main");
        fixture.AssertFullSemver("1.2.2-beta.1+2", configuration);

        // Bump to minor version increment
        fixture.MakeACommit("+semver: minor");
        fixture.AssertFullSemver("1.3.0-beta.1+3", configuration);

        // Create pre-release on release branch
        fixture.ApplyTag("1.3.1-beta.1");
        fixture.AssertFullSemver("1.3.1-beta.2+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.3.1-beta.2+1", configuration);
        fixture.Checkout("main");

        if (withPullRequestIntoMain)
        {
            // Create a PullRequest into main
            fixture.BranchTo("pull/2/merge", "pull");
            fixture.SequenceDiagram.Activate("pull/2/merge");
            fixture.MergeNoFF(branchName);
            fixture.AssertFullSemver("1.3.1-PullRequest2.5", configuration);
            fixture.Checkout("main");
            fixture.Remove("pull/2/merge");
        }

        // Merge release into main branch
        fixture.MergeNoFF(branchName);
        fixture.Remove(branchName);
        fixture.SequenceDiagram.NoteOver("Release branches should\r\nbe deleted once merged", branchName);
        fixture.AssertFullSemver("1.3.1-5", configuration);

        // Commit on main branch
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.3.2-1", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void VersionedReleaseBranch(bool withPullRequestIntoMain)
    {
        var configuration = GitHubFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();

        // GitFlow setup
        fixture.SequenceDiagram.Activate("main");
        fixture.Repository.MakeACommit();
        fixture.ApplyTag("1.2.0");
        fixture.AssertFullSemver("1.2.0", configuration);

        // Branch from main
        const string branchName = "release/2.2.1";
        fixture.BranchTo(branchName, "release");
        fixture.SequenceDiagram.Activate("release");
        fixture.SequenceDiagram.Deactivate("main");
        fixture.AssertFullSemver("2.2.1-beta.1+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.2.1-beta.1+1", configuration);

        // Create hotfix on main branch
        fixture.Checkout("main");
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.1-1", configuration);
        fixture.ApplyTag("2.2.1");
        fixture.AssertFullSemver("2.2.1", configuration);

        // Merge main to release branch
        fixture.MergeTo(branchName);
        fixture.SequenceDiagram.Deactivate("main");
        fixture.AssertFullSemver("2.2.2-beta.1+2", configuration);

        // Bump to minor version increment
        fixture.MakeACommit("+semver: minor");
        fixture.AssertFullSemver("2.3.0-beta.1+3", configuration);

        // Create pre-release on release branch
        fixture.ApplyTag("2.3.1-beta.1");
        fixture.AssertFullSemver("2.3.1-beta.2+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.3.1-beta.2+1", configuration);
        fixture.Checkout("main");

        if (withPullRequestIntoMain)
        {
            // Create a PullRequest into main
            fixture.BranchTo("pull/2/merge", "pull");
            fixture.SequenceDiagram.Activate("pull/2/merge");
            fixture.MergeNoFF(branchName);
            fixture.AssertFullSemver("2.3.1-PullRequest2.5", configuration);
            fixture.Checkout("main");
            fixture.Remove("pull/2/merge");
        }

        // Merge release into main branch
        fixture.MergeNoFF(branchName);
        fixture.Remove(branchName);
        fixture.SequenceDiagram.NoteOver("Release branches should\r\nbe deleted once merged", branchName);
        fixture.AssertFullSemver("2.3.1-5", configuration);

        // Commit on main branch
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.3.1-6", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void VersionedReleaseBranchWithMainline(bool withPullRequestIntoMain)
    {
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithNextVersion("1.2.0")
            .WithVersionStrategies(VersionStrategies.ConfiguredNextVersion, VersionStrategies.Mainline)
            .Build();

        using var fixture = new EmptyRepositoryFixture();

        // GitFlow setup
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.0-1", configuration);
        fixture.Repository.ApplyTag("1.2.0");

        // Branch from main
        const string branchName = "release/2.2.1";
        fixture.BranchTo(branchName, "release");
        fixture.SequenceDiagram.Activate("release");
        fixture.SequenceDiagram.Deactivate("main");
        fixture.AssertFullSemver("2.2.1-beta.1+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.2.1-beta.1+1", configuration);

        // Create hotfix on main branch
        fixture.Checkout("main");
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.1-1", configuration);

        // Merge main to release branch
        fixture.MergeTo(branchName);
        fixture.SequenceDiagram.Deactivate("main");
        fixture.AssertFullSemver("2.2.1-beta.1+2", configuration);

        // Bump to minor version increment
        fixture.MakeACommit("+semver: major");
        fixture.AssertFullSemver("2.2.1-beta.1+3", configuration);

        // Create pre-release on release branch
        fixture.ApplyTag("2.2.1-beta.1");
        fixture.AssertFullSemver("2.2.1-beta.2+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.2.1-beta.2+1", configuration);
        fixture.Checkout("main");

        if (withPullRequestIntoMain)
        {
            // Create a PullRequest into main
            fixture.BranchTo("pull/2/merge", "pull");
            fixture.SequenceDiagram.Activate("pull/2/merge");
            fixture.MergeNoFF(branchName);
            fixture.AssertFullSemver("2.2.1-PullRequest2.5", configuration);
            fixture.Checkout("main");
            fixture.Remove("pull/2/merge");
        }

        // Merge release into main branch
        fixture.MergeNoFF(branchName);
        fixture.Remove(branchName);
        fixture.SequenceDiagram.NoteOver("Release branches should\r\nbe deleted once merged", branchName);
        fixture.AssertFullSemver("2.2.1-5", configuration);

        // Commit on main branch
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.2.2-1", configuration);
    }
}
