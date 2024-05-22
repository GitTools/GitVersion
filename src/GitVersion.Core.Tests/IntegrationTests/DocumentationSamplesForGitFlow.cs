using GitVersion.Configuration;
using GitVersion.VersionCalculation;
using LibGit2Sharp;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class DocumentationSamplesForGitFlow
{
    [TestCase(false)]
    [TestCase(true)]
    public void FeatureFromMainBranch(bool withPullRequestIntoMain)
    {
        var configuration = GitFlowConfigurationBuilder.New.Build();

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
    public void FeatureFromMainBranchWithMainline(bool withPullRequestIntoMain)
    {
        var configuration = GitFlowConfigurationBuilder.New
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
    public void HotfixBranch(bool withPullRequestIntoMain)
    {
        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();

        // GitFlow setup
        fixture.SequenceDiagram.Activate("main");
        fixture.Repository.MakeACommit();
        fixture.ApplyTag("1.2.0");
        fixture.AssertFullSemver("1.2.0", configuration);

        // Branch from main
        const string branchName = "hotfix/next";
        fixture.BranchTo(branchName, "hotfix");
        fixture.SequenceDiagram.Activate("hotfix");
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

        // Merge main to hotfix branch
        fixture.MergeTo(branchName);
        fixture.SequenceDiagram.Deactivate("main");
        fixture.AssertFullSemver("1.2.2-beta.1+2", configuration);

        // Bump to minor version increment
        fixture.MakeACommit("+semver: minor");
        fixture.AssertFullSemver("1.3.0-beta.1+3", configuration);

        // Create pre-release on hotfix branch
        fixture.ApplyTag("2.0.0-beta.1");
        fixture.AssertFullSemver("2.0.0-beta.2+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.0.0-beta.2+1", configuration);
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

        // Merge hotfix into main branch
        fixture.MergeNoFF(branchName);
        fixture.Remove(branchName);
        fixture.SequenceDiagram.NoteOver("Hotfix branches should\r\nbe deleted once merged", branchName);
        fixture.AssertFullSemver("2.0.0-5", configuration);

        // Commit on main branch
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.0.0-6", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void HotfixBranchWithMainline(bool withPullRequestIntoMain)
    {
        var configuration = GitFlowConfigurationBuilder.New
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
        const string branchName = "hotfix/next";
        fixture.BranchTo(branchName, "hotfix");
        fixture.SequenceDiagram.Activate("hotfix");
        fixture.SequenceDiagram.Deactivate("main");
        fixture.AssertFullSemver("1.2.1-beta.1+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.1-beta.1+1", configuration);

        // Create hotfix on main branch
        fixture.Checkout("main");
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.1-1", configuration);

        // Merge main to hotfix branch
        fixture.MergeTo(branchName);
        fixture.SequenceDiagram.Deactivate("main");
        fixture.AssertFullSemver("1.2.2-beta.1+2", configuration);

        // Bump to minor version increment
        fixture.MakeACommit("+semver: minor");
        fixture.AssertFullSemver("1.3.0-beta.1+3", configuration);

        // Create pre-release on hotfix branch
        fixture.ApplyTag("2.0.0-beta.1");
        fixture.AssertFullSemver("2.0.0-beta.2+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.0.0-beta.2+1", configuration);
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

        // Merge hotfix into main branch
        fixture.MergeNoFF(branchName);
        fixture.Remove(branchName);
        fixture.SequenceDiagram.NoteOver("Hotfix branches should\r\nbe deleted once merged", branchName);
        fixture.AssertFullSemver("2.0.0-5", configuration);

        // Commit on main branch
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.0.1-1", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void VersionedHotfixBranch(bool withPullRequestIntoMain)
    {
        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();

        // GitFlow setup
        fixture.SequenceDiagram.Activate("main");
        fixture.Repository.MakeACommit();
        fixture.ApplyTag("1.2.0");
        fixture.AssertFullSemver("1.2.0", configuration);

        // Branch from main
        const string branchName = "hotfix/2.2.1";
        fixture.BranchTo(branchName, "hotfix");
        fixture.SequenceDiagram.Activate("hotfix");
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

        // Merge main to hotfix branch
        fixture.MergeTo(branchName);
        fixture.SequenceDiagram.Deactivate("main");
        fixture.AssertFullSemver("2.2.2-beta.1+2", configuration);

        // Bump to minor version increment
        fixture.MakeACommit("+semver: major");
        fixture.AssertFullSemver("3.0.0-beta.1+3", configuration);

        // Create pre-release on hotfix branch
        fixture.ApplyTag("3.0.1-beta.1");
        fixture.AssertFullSemver("3.0.1-beta.2+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("3.0.1-beta.2+1", configuration);
        fixture.Checkout("main");

        if (withPullRequestIntoMain)
        {
            // Create a PullRequest into main
            fixture.BranchTo("pull/2/merge", "pull");
            fixture.SequenceDiagram.Activate("pull/2/merge");
            fixture.MergeNoFF(branchName);
            fixture.AssertFullSemver("3.0.1-PullRequest2.5", configuration);
            fixture.Checkout("main");
            fixture.Remove("pull/2/merge");
        }

        // Merge hotfix into main branch
        fixture.MergeNoFF(branchName);
        fixture.Remove(branchName);
        fixture.SequenceDiagram.NoteOver("Hotfix branches should\r\nbe deleted once merged", branchName);
        fixture.AssertFullSemver("3.0.1-5", configuration);

        // Commit on main branch
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("3.0.1-6", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void VersionedHotfixBranchWithMainline(bool withPullRequestIntoMain)
    {
        var configuration = GitFlowConfigurationBuilder.New
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
        const string branchName = "hotfix/2.2.1";
        fixture.BranchTo(branchName, "hotfix");
        fixture.SequenceDiagram.Activate("hotfix");
        fixture.SequenceDiagram.Deactivate("main");
        fixture.AssertFullSemver("2.2.1-beta.1+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.2.1-beta.1+1", configuration);

        // Create hotfix on main branch
        fixture.Checkout("main");
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.1-1", configuration);

        // Merge main to hotfix branch
        fixture.MergeTo(branchName);
        fixture.SequenceDiagram.Deactivate("main");
        fixture.AssertFullSemver("2.2.1-beta.1+2", configuration);

        // Bump to minor version increment
        fixture.MakeACommit("+semver: major");
        fixture.AssertFullSemver("2.2.1-beta.1+3", configuration);

        // Create pre-release on hotfix branch
        fixture.ApplyTag("2.2.2-beta.1");
        fixture.AssertFullSemver("2.2.2-beta.2+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.2.2-beta.2+1", configuration);
        fixture.Checkout("main");

        if (withPullRequestIntoMain)
        {
            // Create a PullRequest into main
            fixture.BranchTo("pull/2/merge", "pull");
            fixture.SequenceDiagram.Activate("pull/2/merge");
            fixture.MergeNoFF(branchName);
            fixture.AssertFullSemver("2.2.2-PullRequest2.5", configuration);
            fixture.Checkout("main");
            fixture.Remove("pull/2/merge");
        }

        // Merge hotfix into main branch
        fixture.MergeNoFF(branchName);
        fixture.Remove(branchName);
        fixture.SequenceDiagram.NoteOver("Hotfix branches should\r\nbe deleted once merged", branchName);
        fixture.AssertFullSemver("2.2.2-5", configuration);

        // Commit on main branch
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.2.3-1", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReleaseBranch(bool withPullRequestIntoMain)
    {
        var configuration = GitFlowConfigurationBuilder.New.Build();

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
        fixture.AssertFullSemver("1.3.0-beta.1+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.3.0-beta.1+1", configuration);

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
        fixture.AssertFullSemver("1.3.0-beta.1+2", configuration);

        // Bump to minor version increment
        fixture.MakeACommit("+semver: major");
        fixture.AssertFullSemver("2.0.0-beta.1+3", configuration);

        // Create pre-release on release branch
        fixture.ApplyTag("2.0.1-beta.1");
        fixture.AssertFullSemver("2.0.1-beta.2+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.0.1-beta.2+1", configuration);
        fixture.Checkout("main");

        if (withPullRequestIntoMain)
        {
            // Create a PullRequest into main
            fixture.BranchTo("pull/2/merge", "pull");
            fixture.SequenceDiagram.Activate("pull/2/merge");
            fixture.MergeNoFF(branchName);
            fixture.AssertFullSemver("2.0.1-PullRequest2.5", configuration);
            fixture.Checkout("main");
            fixture.Remove("pull/2/merge");
        }

        // Merge release into main branch
        fixture.MergeNoFF(branchName);
        fixture.Remove(branchName);
        fixture.SequenceDiagram.NoteOver("Release branches should\r\nbe deleted once merged", branchName);
        fixture.AssertFullSemver("2.0.1-5", configuration);

        // Commit on main branch
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.0.1-6", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReleaseBranchWithMainline(bool withPullRequestIntoMain)
    {
        var configuration = GitFlowConfigurationBuilder.New
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
        fixture.AssertFullSemver("1.3.0-beta.1+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.3.0-beta.1+1", configuration);

        // Create hotfix on main branch
        fixture.Checkout("main");
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.1-1", configuration);

        // Merge main to release branch
        fixture.MergeTo(branchName);
        fixture.SequenceDiagram.Deactivate("main");
        fixture.AssertFullSemver("1.3.0-beta.1+2", configuration);

        // Bump to minor version increment
        fixture.MakeACommit("+semver: major");
        fixture.AssertFullSemver("2.0.0-beta.1+3", configuration);

        // Create pre-release on release branch
        fixture.ApplyTag("2.0.1-beta.1");
        fixture.AssertFullSemver("2.0.1-beta.2+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.0.1-beta.2+1", configuration);
        fixture.Checkout("main");

        if (withPullRequestIntoMain)
        {
            // Create a PullRequest into main
            fixture.BranchTo("pull/2/merge", "pull");
            fixture.SequenceDiagram.Activate("pull/2/merge");
            fixture.MergeNoFF(branchName);
            fixture.AssertFullSemver("2.0.1-PullRequest2.5", configuration);
            fixture.Checkout("main");
            fixture.Remove("pull/2/merge");
        }

        // Merge release into main branch
        fixture.MergeNoFF(branchName);
        fixture.Remove(branchName);
        fixture.SequenceDiagram.NoteOver("Release branches should\r\nbe deleted once merged", branchName);
        fixture.AssertFullSemver("2.0.1-5", configuration);

        // Commit on main branch
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.0.2-1", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void VersionedReleaseBranch(bool withPullRequestIntoMain)
    {
        var configuration = GitFlowConfigurationBuilder.New.Build();

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
        fixture.AssertFullSemver("2.3.0-beta.1+2", configuration);

        // Bump to minor version increment
        fixture.MakeACommit("+semver: major");
        fixture.AssertFullSemver("3.0.0-beta.1+3", configuration);

        // Create pre-release on release branch
        fixture.ApplyTag("3.0.1-beta.1");
        fixture.AssertFullSemver("3.0.1-beta.2+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("3.0.1-beta.2+1", configuration);
        fixture.Checkout("main");

        if (withPullRequestIntoMain)
        {
            // Create a PullRequest into main
            fixture.BranchTo("pull/2/merge", "pull");
            fixture.SequenceDiagram.Activate("pull/2/merge");
            fixture.MergeNoFF(branchName);
            fixture.AssertFullSemver("3.0.1-PullRequest2.5", configuration);
            fixture.Checkout("main");
            fixture.Remove("pull/2/merge");
        }

        // Merge release into main branch
        fixture.MergeNoFF(branchName);
        fixture.Remove(branchName);
        fixture.SequenceDiagram.NoteOver("Release branches should\r\nbe deleted once merged", branchName);
        fixture.AssertFullSemver("3.0.1-5", configuration);

        // Commit on main branch
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("3.0.1-6", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void VersionedReleaseBranchWithMainline(bool withPullRequestIntoMain)
    {
        var configuration = GitFlowConfigurationBuilder.New
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

    [TestCase(false)]
    [TestCase(true)]
    public void DevelopBranch(bool withPullRequestIntoMain)
    {
        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.SequenceDiagram.Participant("main");
        fixture.SequenceDiagram.Participant("develop");

        // GitFlow setup
        fixture.SequenceDiagram.Activate("main");
        fixture.Repository.MakeACommit();
        fixture.ApplyTag("1.2.0");
        fixture.AssertFullSemver("1.2.0", configuration);

        // Branch from main
        const string branchName = "develop";
        fixture.BranchTo(branchName);
        fixture.SequenceDiagram.Deactivate("main");
        fixture.SequenceDiagram.Activate(branchName);
        fixture.AssertFullSemver("1.3.0-alpha.0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.3.0-alpha.1", configuration);

        // Create release from develop branch
        fixture.BranchTo("release/1.3.0", "release");
        fixture.SequenceDiagram.Deactivate("develop");
        fixture.SequenceDiagram.Activate("release");
        fixture.AssertFullSemver("1.3.0-beta.1+1", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.3.0-beta.1+2", configuration);

        // Bump to major version increment
        fixture.Checkout("develop");
        fixture.SequenceDiagram.Activate("develop");
        fixture.AssertFullSemver("1.4.0-alpha.0", configuration);

        // Merge release into develop branch
        fixture.MergeNoFF("release/1.3.0");
        fixture.AssertFullSemver("1.4.0-alpha.2", configuration);

        // Merge release into main branch
        fixture.Checkout("main");
        fixture.SequenceDiagram.Activate("main");
        fixture.MergeNoFF("release/1.3.0");
        fixture.Remove("release/1.3.0");
        fixture.SequenceDiagram.NoteOver("Release branches should\r\nbe deleted once merged", "release");
        fixture.ApplyTag("1.3.0");
        fixture.AssertFullSemver("1.3.0", configuration);

        // Create hotfix on main branch
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.3.1-1", configuration);

        // Merge main to release branch
        fixture.MergeTo(branchName);
        fixture.SequenceDiagram.Deactivate("main");
        fixture.AssertFullSemver("1.4.0-alpha.3", configuration);
        fixture.MakeACommit("+semver: major");
        fixture.AssertFullSemver("2.0.0-alpha.4", configuration);

        // Create pre-release on release branch
        fixture.ApplyTag("2.1.0-alpha.1");
        fixture.AssertFullSemver("2.1.0-alpha.1", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.1.0-alpha.2", configuration);
        fixture.Checkout("main");

        if (withPullRequestIntoMain)
        {
            // Create a PullRequest into main
            fixture.BranchTo("pull/2/merge", "pull");
            fixture.SequenceDiagram.Activate("pull/2/merge");
            fixture.MergeNoFF(branchName);
            fixture.AssertFullSemver("2.1.0-PullRequest2.6", configuration);
            fixture.Checkout("main");
            fixture.Remove("pull/2/merge");
        }

        // Merge develop into main branch
        fixture.MergeNoFF(branchName);
        fixture.SequenceDiagram.Deactivate(branchName);
        fixture.AssertFullSemver("2.1.0-6", configuration);

        // Commit on main branch
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.1.0-7", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DevelopBranchWithMainline(bool withPullRequestIntoMain)
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithNextVersion("1.2.0")
            .WithVersionStrategies(VersionStrategies.ConfiguredNextVersion, VersionStrategies.TrackReleaseBranches, VersionStrategies.Mainline)
            .Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.SequenceDiagram.Participant("main");
        fixture.SequenceDiagram.Participant("develop");

        // GitFlow setup
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.0-1", configuration);
        fixture.Repository.ApplyTag("1.2.0");

        // Branch from main
        const string branchName = "develop";
        fixture.BranchTo(branchName);
        fixture.SequenceDiagram.Deactivate("main");
        fixture.SequenceDiagram.Activate(branchName);
        fixture.AssertFullSemver("1.3.0-alpha.0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.3.0-alpha.1", configuration);

        // Create release from develop branch
        fixture.BranchTo("release/1.3.0", "release");
        fixture.SequenceDiagram.Deactivate("develop");
        fixture.SequenceDiagram.Activate("release");
        fixture.AssertFullSemver("1.3.0-beta.1+1", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.3.0-beta.1+2", configuration);

        // Bump to major version increment
        fixture.Checkout("develop");
        fixture.SequenceDiagram.Activate("develop");
        fixture.AssertFullSemver("1.4.0-alpha.0", configuration);

        // Merge release into develop branch
        fixture.MergeNoFF("release/1.3.0");
        fixture.AssertFullSemver("1.4.0-alpha.2", configuration);

        // Merge release into main branch
        fixture.Checkout("main");
        fixture.SequenceDiagram.Activate("main");
        fixture.MergeNoFF("release/1.3.0");
        fixture.Remove("release/1.3.0");
        fixture.SequenceDiagram.NoteOver("Release branches should\r\nbe deleted once merged", "release");

        // Create hotfix on main branch
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.3.1-1", configuration);

        // Merge main to release branch
        fixture.MergeTo(branchName);
        fixture.SequenceDiagram.Deactivate("main");
        fixture.AssertFullSemver("1.4.0-alpha.2", configuration);
        fixture.MakeACommit("+semver: major");
        fixture.AssertFullSemver("2.0.0-alpha.3", configuration);

        // Create pre-release on release branch
        fixture.ApplyTag("2.1.0-alpha.1");
        fixture.AssertFullSemver("2.1.0-alpha.1", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.1.0-alpha.2", configuration);
        fixture.Checkout("main");

        if (withPullRequestIntoMain)
        {
            // Create a PullRequest into main
            fixture.BranchTo("pull/2/merge", "pull");
            fixture.SequenceDiagram.Activate("pull/2/merge");
            fixture.MergeNoFF(branchName);
            fixture.AssertFullSemver("2.1.0-PullRequest2.5", configuration);
            fixture.Checkout("main");
            fixture.Remove("pull/2/merge");
        }

        // Merge develop into main branch
        fixture.MergeNoFF(branchName);
        fixture.SequenceDiagram.Deactivate(branchName);
        fixture.AssertFullSemver("2.1.0-5", configuration);

        // Commit on main branch
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.1.1-1", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SupportBranch(bool withPullRequestIntoSupport)
    {
        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.SequenceDiagram.Participant("main");

        // GitFlow setup
        fixture.SequenceDiagram.Activate("main");
        fixture.Repository.MakeACommit();
        fixture.ApplyTag("1.2.0");
        fixture.AssertFullSemver("1.2.0", configuration);

        // Branch from main
        fixture.BranchToFromTag("support/1.x", "1.2.0", "main", "support");
        fixture.SequenceDiagram.Activate("support");
        fixture.AssertFullSemver("1.2.0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.1-1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("+semver: major");
        fixture.AssertFullSemver("2.0.0-1", configuration);
        fixture.ApplyTag("2.0.0");
        fixture.AssertFullSemver("2.0.0", configuration);

        fixture.Checkout("support/1.x");
        fixture.BranchTo("hotfix/1.2.2", "hotfix");
        fixture.SequenceDiagram.Deactivate("support");
        fixture.SequenceDiagram.Activate("hotfix");
        fixture.AssertFullSemver("1.2.2-beta.1+1", configuration);
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.2-beta.1+3", configuration);
        fixture.ApplyTag("1.2.3-beta.1");
        fixture.AssertFullSemver("1.2.3-beta.2+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.3-beta.2+1", configuration);
        fixture.Checkout("support/1.x");

        if (withPullRequestIntoSupport)
        {
            // Create a PullRequest into support
            fixture.BranchTo("pull/2/merge", "pull");
            fixture.SequenceDiagram.Activate("pull/2/merge");
            fixture.MergeNoFF("hotfix/1.2.2");
            fixture.AssertFullSemver("1.2.3-PullRequest2.5", configuration);
            fixture.Checkout("support/1.x");
            fixture.Remove("pull/2/merge");
        }

        // Merge hotfix into support branch
        fixture.MergeNoFF("hotfix/1.2.2");
        fixture.Remove("hotfix/1.2.2");
        fixture.SequenceDiagram.NoteOver("Hotfix branches should\r\nbe deleted once merged", "hotfix/1.2.2");
        fixture.AssertFullSemver("1.2.3-5", configuration);

        // Commit on support branch
        fixture.SequenceDiagram.Activate("support");
        fixture.ApplyTag("1.2.3");
        fixture.AssertFullSemver("1.2.3", configuration);

        fixture.Checkout("main");
        fixture.AssertFullSemver("2.0.0", configuration);
        fixture.MergeNoFF("support/1.x");
        fixture.AssertFullSemver("2.0.1-6", configuration);
        fixture.ApplyTag("2.0.1");
        fixture.AssertFullSemver("2.0.1", configuration);

        fixture.Checkout("support/1.x");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.4-1", configuration);

        fixture.Checkout("main");
        fixture.MergeNoFF("support/1.x");
        fixture.AssertFullSemver("2.0.2-2", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SupportBranchWithMainline(bool withPullRequestIntoSupport)
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithNextVersion("1.2.0")
            .WithVersionStrategies(VersionStrategies.ConfiguredNextVersion, VersionStrategies.TrackReleaseBranches, VersionStrategies.Mainline)
            .Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.SequenceDiagram.Participant("main");

        // GitFlow setup
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.0-1", configuration);
        fixture.Repository.ApplyTag("1.2.0");

        // Branch from main
        fixture.BranchToFromTag("support/1.x", "1.2.0", "main", "support");
        fixture.SequenceDiagram.Activate("support");
        fixture.AssertFullSemver("1.2.0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.1-1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("+semver: major");
        fixture.AssertFullSemver("2.0.0-1", configuration);

        fixture.Checkout("support/1.x");
        fixture.BranchTo("hotfix/1.2.2", "hotfix");
        fixture.SequenceDiagram.Deactivate("support");
        fixture.SequenceDiagram.Activate("hotfix");
        fixture.AssertFullSemver("1.2.2-beta.1+0", configuration);
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.2-beta.1+2", configuration);
        fixture.ApplyTag("1.2.3-beta.1");
        fixture.AssertFullSemver("1.2.3-beta.2+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.3-beta.2+1", configuration);
        fixture.Checkout("support/1.x");

        if (withPullRequestIntoSupport)
        {
            // Create a PullRequest into support
            fixture.BranchTo("pull/2/merge", "pull");
            fixture.SequenceDiagram.Activate("pull/2/merge");
            fixture.MergeNoFF("hotfix/1.2.2");
            fixture.AssertFullSemver("1.2.3-PullRequest2.4", configuration);
            fixture.Checkout("support/1.x");
            fixture.Remove("pull/2/merge");
        }

        // Merge hotfix into support branch
        fixture.MergeNoFF("hotfix/1.2.2");
        fixture.Remove("hotfix/1.2.2");
        fixture.SequenceDiagram.NoteOver("Hotfix branches should\r\nbe deleted once merged", "hotfix/1.2.2");
        fixture.AssertFullSemver("1.2.3-4", configuration);

        // Commit on support branch
        fixture.SequenceDiagram.Activate("support");

        fixture.Checkout("main");
        fixture.AssertFullSemver("2.0.0-1", configuration);
        //fixture.MergeNoFF("support/1.x");
        //fixture.AssertFullSemver("2.0.1-6", configuration);

        //fixture.Checkout("support/1.x");
        //fixture.MakeACommit();
        //fixture.AssertFullSemver("1.2.4-1", configuration);

        //fixture.Checkout("main");
        //fixture.MergeNoFF("support/1.x");
        //fixture.AssertFullSemver("2.0.2-2", configuration);
    }
}
