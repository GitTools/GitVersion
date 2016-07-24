using System;
using GitTools;
using GitTools.Testing;
using GitVersionCore.Tests;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class DocumentationSamples
{
    [Test]
    public void GitFlowFeatureBranch()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.SequenceDiagram.Participant("master");
            fixture.SequenceDiagram.Participant("develop");

            // GitFlow setup
            fixture.Repository.MakeACommit();
            fixture.ApplyTag("1.2.0");
            fixture.BranchTo("develop");

            // Branch to develop
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.3.0-alpha.1");

            // Open Pull Request
            fixture.BranchTo("feature/myfeature", "feature");
            fixture.SequenceDiagram.Activate("feature/myfeature");
            fixture.AssertFullSemver("1.3.0-myfeature.1+1");
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.3.0-myfeature.1+2");

            // Merge into develop
            fixture.Checkout("develop");
            fixture.MergeNoFF("feature/myfeature");
            fixture.SequenceDiagram.Destroy("feature/myfeature");
            fixture.SequenceDiagram.NoteOver("Feature branches should\r\n" +
                             "be deleted once merged", "feature/myfeature");
            fixture.AssertFullSemver("1.3.0-alpha.3");
            Console.WriteLine(fixture.SequenceDiagram.GetDiagram());
        }
    }

    [Test]
    public void GitFlowPullRequestBranch()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.SequenceDiagram.Participant("master");
            fixture.SequenceDiagram.Participant("develop");

            // GitFlow setup
            fixture.Repository.MakeACommit();
            fixture.ApplyTag("1.2.0");
            fixture.BranchTo("develop");

            // Branch to develop
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.3.0-alpha.1");

            // Open Pull Request
            fixture.BranchTo("pull/2/merge", "pr");
            fixture.SequenceDiagram.Activate("pull/2/merge");
            fixture.AssertFullSemver("1.3.0-PullRequest.2+1");
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.3.0-PullRequest.2+2");

            // Merge into develop
            fixture.Checkout("develop");
            fixture.MergeNoFF("pull/2/merge");
            fixture.SequenceDiagram.Destroy("pull/2/merge");
            fixture.SequenceDiagram.NoteOver("Feature branches/pr's should\r\n" +
                             "be deleted once merged", "pull/2/merge");
            fixture.AssertFullSemver("1.3.0-alpha.3");
            Console.WriteLine(fixture.SequenceDiagram.GetDiagram());
        }
    }

    [Test]
    public void GitFlowHotfixBranch()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.SequenceDiagram.Participant("develop");
            fixture.SequenceDiagram.Participant("master");

            fixture.Repository.MakeACommit();
            fixture.ApplyTag("1.2.0");

            // Create hotfix branch
            fixture.Checkout("master");
            fixture.BranchTo("hotfix/1.2.1", "hotfix");
            fixture.SequenceDiagram.Activate("hotfix/1.2.1");
            fixture.MakeACommit();
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.2.1-beta.1+2");

            // Apply beta.1 tag should be exact tag
            fixture.ApplyTag("1.2.1-beta.1");
            fixture.AssertFullSemver("1.2.1-beta.1");
            fixture.Checkout("master");
            fixture.MergeNoFF("hotfix/1.2.1");
            fixture.SequenceDiagram.Destroy("hotfix/1.2.1");
            fixture.SequenceDiagram.NoteOver("Hotfix branches are deleted once merged", "hotfix/1.2.1");
            fixture.ApplyTag("1.2.1");
            Console.WriteLine(fixture.SequenceDiagram.GetDiagram());
        }
    }

    [Test]
    public void GitFlowMinorRelease()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.SequenceDiagram.Participant("master");
            fixture.SequenceDiagram.Participant("develop");

            // GitFlow setup
            fixture.Repository.MakeACommit();
            fixture.ApplyTag("1.2.1");

            // GitFlow setup
            fixture.BranchTo("develop");
            fixture.MakeACommit();

            // Create release branch
            fixture.BranchTo("release/1.3.0", "release");
            fixture.SequenceDiagram.Activate("release/1.3.0");
            fixture.AssertFullSemver("1.3.0-beta.1+0");

            // Make another commit on develop
            fixture.Checkout("develop");
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.4.0-alpha.1");

            // Make a commit to release-1.3.0
            fixture.Checkout("release/1.3.0");
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.3.0-beta.1+1");

            // Apply beta.1 tag should be exact tag
            fixture.ApplyTag("1.3.0-beta.1");
            fixture.AssertFullSemver("1.3.0-beta.1");

            // Make a commit after a tag should bump up the beta
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.3.0-beta.2+2");

            // Complete release
            fixture.Checkout("master");
            fixture.MergeNoFF("release/1.3.0");
            fixture.Checkout("develop");
            fixture.MergeNoFF("release/1.3.0");
            fixture.SequenceDiagram.Destroy("release/1.3.0");
            fixture.SequenceDiagram.NoteOver("Release branches are deleted once merged", "release/1.3.0");

            fixture.Checkout("master");
            fixture.ApplyTag("1.3.0");
            fixture.AssertFullSemver("1.3.0");

            // Not 0 for commit count as we can't know the increment rules of the merged branch
            fixture.Checkout("develop");
            fixture.AssertFullSemver("1.4.0-alpha.4");
            Console.WriteLine(fixture.SequenceDiagram.GetDiagram());
        }
    }

    [Test]
    public void GitFlowMajorRelease()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.SequenceDiagram.Participant("master");
            fixture.SequenceDiagram.Participant("develop");

            fixture.Repository.MakeACommit();
            fixture.ApplyTag("1.3.0");

            // GitFlow setup
            fixture.BranchTo("develop");
            fixture.MakeACommit();

            // Create release branch
            fixture.BranchTo("release/2.0.0", "release");
            fixture.SequenceDiagram.Activate("release/2.0.0");
            fixture.AssertFullSemver("2.0.0-beta.1+0");

            // Make another commit on develop
            fixture.Checkout("develop");
            fixture.MakeACommit();
            fixture.AssertFullSemver("2.1.0-alpha.1");

            // Make a commit to release-2.0.0
            fixture.Checkout("release/2.0.0");
            fixture.MakeACommit();
            fixture.AssertFullSemver("2.0.0-beta.1+1");

            // Apply beta.1 tag should be exact tag
            fixture.ApplyTag("2.0.0-beta.1");
            fixture.AssertFullSemver("2.0.0-beta.1");

            // Make a commit after a tag should bump up the beta
            fixture.MakeACommit();
            fixture.AssertFullSemver("2.0.0-beta.2+2");

            // Complete release
            fixture.Checkout("master");
            fixture.MergeNoFF("release/2.0.0");
            fixture.Checkout("develop");
            fixture.MergeNoFF("release/2.0.0");
            fixture.SequenceDiagram.Destroy("release/2.0.0");
            fixture.SequenceDiagram.NoteOver("Release branches are deleted once merged", "release/2.0.0");

            fixture.Checkout("master");
            fixture.AssertFullSemver("2.0.0+0");
            fixture.ApplyTag("2.0.0");
            fixture.AssertFullSemver("2.0.0");

            // Not 0 for commit count as we can't know the increment rules of the merged branch
            fixture.Checkout("develop");
            fixture.AssertFullSemver("2.1.0-alpha.4");
            Console.WriteLine(fixture.SequenceDiagram.GetDiagram());
        }
    }

    [Test]
    public void GitFlowSupportHotfixRelease()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.SequenceDiagram.Participant("develop");
            fixture.SequenceDiagram.Participant("master");

            fixture.Repository.MakeACommit();
            fixture.ApplyTag("1.3.0");

            // GitFlow setup
            fixture.Repository.MakeACommit();
            fixture.BranchTo("develop");
            fixture.SequenceDiagram.NoteOver("Create 2.0.0 release and complete", "develop", "master");
            fixture.Checkout("master");
            fixture.ApplyTag("2.0.0");

            // Create hotfix branch
            fixture.Checkout("1.3.0");
            fixture.BranchToFromTag("support/1.x", "1.3.0", "master", "support");
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.3.1+1");

            fixture.BranchTo("hotfix/1.3.1", "hotfix2");
            fixture.SequenceDiagram.Activate("hotfix/1.3.1");
            fixture.MakeACommit();
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.3.1-beta.1+3");

            // Apply beta.1 tag should be exact tag
            fixture.ApplyTag("1.3.1-beta.1");
            fixture.AssertFullSemver("1.3.1-beta.1");
            fixture.Checkout("support/1.x");
            fixture.MergeNoFF("hotfix/1.3.1");
            fixture.SequenceDiagram.Destroy("hotfix/1.3.1");
            fixture.SequenceDiagram.NoteOver("Hotfix branches are deleted once merged", "hotfix/1.3.1");
            fixture.AssertFullSemver("1.3.1+4");
            fixture.ApplyTag("1.3.1");
            fixture.AssertFullSemver("1.3.1");
            Console.WriteLine(fixture.SequenceDiagram.GetDiagram());
        }
    }

    [Test]
    public void GitFlowSupportMinorRelease()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.SequenceDiagram.Participant("develop");
            fixture.SequenceDiagram.Participant("master");

            fixture.Repository.MakeACommit();
            fixture.ApplyTag("1.3.0");

            // GitFlow setup
            fixture.Repository.MakeACommit();
            fixture.BranchTo("develop");
            fixture.SequenceDiagram.NoteOver("Create 2.0.0 release and complete", "develop", "master");
            fixture.Checkout("master");
            fixture.ApplyTag("2.0.0");

            // Create hotfix branch
            fixture.Checkout("1.3.0");
            fixture.BranchToFromTag("support/1.x", "1.3.0", "master", "support");
            fixture.SequenceDiagram.NoteOver("Create 1.3.1 release and complete", "master", "support/1.x");
            fixture.Repository.MakeACommit();
            fixture.ApplyTag("1.3.1");

            fixture.BranchTo("release/1.4.0", "supportRelease");
            fixture.SequenceDiagram.Activate("release/1.4.0");
            fixture.MakeACommit();
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.4.0-beta.1+2");

            // Apply beta.1 tag should be exact tag
            fixture.ApplyTag("1.4.0-beta.1");
            fixture.AssertFullSemver("1.4.0-beta.1");
            fixture.Checkout("support/1.x");
            fixture.MergeNoFF("release/1.4.0");
            fixture.SequenceDiagram.Destroy("release/1.4.0");
            fixture.SequenceDiagram.NoteOver("Release branches are deleted once merged", "release/1.4.0");
            fixture.AssertFullSemver("1.4.0+0");
            fixture.ApplyTag("1.4.0");
            fixture.AssertFullSemver("1.4.0");
            Console.WriteLine(fixture.SequenceDiagram.GetDiagram());
        }
    }


    [Test]
    public void GitHubFlowFeatureBranch()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.SequenceDiagram.Participant("master");

            // GitFlow setup
            fixture.Repository.MakeACommit();
            fixture.ApplyTag("1.2.0");

            // Branch to develop
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.2.1+1");

            // Open Pull Request
            fixture.BranchTo("feature/myfeature", "feature");
            fixture.SequenceDiagram.Activate("feature/myfeature");
            fixture.AssertFullSemver("1.2.1-myfeature.1+1");
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.2.1-myfeature.1+2");

            // Merge into master
            fixture.Checkout("master");
            fixture.MergeNoFF("feature/myfeature");
            fixture.SequenceDiagram.Destroy("feature/myfeature");
            fixture.SequenceDiagram.NoteOver("Feature branches should\r\n" +
                             "be deleted once merged", "feature/myfeature");
            fixture.AssertFullSemver("1.2.1+3");
        }
    }

    [Test]
    public void GitHubFlowPullRequestBranch()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.SequenceDiagram.Participant("master");

            // GitFlow setup
            fixture.Repository.MakeACommit();
            fixture.ApplyTag("1.2.0");

            // Branch to develop
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.2.1+1");

            // Open Pull Request
            fixture.BranchTo("pull/2/merge", "pr");
            fixture.SequenceDiagram.Activate("pull/2/merge");
            fixture.AssertFullSemver("1.2.1-PullRequest.2+1");
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.2.1-PullRequest.2+2");

            // Merge into master
            fixture.Checkout("master");
            fixture.MergeNoFF("pull/2/merge");
            fixture.SequenceDiagram.Destroy("pull/2/merge");
            fixture.SequenceDiagram.NoteOver("Feature branches/pr's should\r\n" +
                             "be deleted once merged", "pull/2/merge");
            fixture.AssertFullSemver("1.2.1+3");
        }
    }

    [Test]
    public void GitHubFlowMajorRelease()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.SequenceDiagram.Participant("master");

            fixture.Repository.MakeACommit();
            fixture.ApplyTag("1.3.0");

            // Create release branch
            fixture.BranchTo("release/2.0.0", "release");
            fixture.SequenceDiagram.Activate("release/2.0.0");
            fixture.MakeACommit();
            fixture.AssertFullSemver("2.0.0-beta.1+1");
            fixture.MakeACommit();
            fixture.AssertFullSemver("2.0.0-beta.1+2");

            // Apply beta.1 tag should be exact tag
            fixture.ApplyTag("2.0.0-beta.1");
            fixture.AssertFullSemver("2.0.0-beta.1");

            // test that the CommitsSinceVersionSource should still return commit count
            var version = fixture.GetVersion();
            version.CommitsSinceVersionSource.ShouldBe("2");

            // Make a commit after a tag should bump up the beta
            fixture.MakeACommit();
            fixture.AssertFullSemver("2.0.0-beta.2+3");

            // Complete release
            fixture.Checkout("master");
            fixture.MergeNoFF("release/2.0.0");
            fixture.SequenceDiagram.Destroy("release/2.0.0");
            fixture.SequenceDiagram.NoteOver("Release branches are deleted once merged", "release/2.0.0");
            
            fixture.AssertFullSemver("2.0.0+0");
            fixture.ApplyTag("2.0.0");
            fixture.AssertFullSemver("2.0.0");
            fixture.MakeACommit();
            fixture.Repository.DumpGraph();
            fixture.AssertFullSemver("2.0.1+1");
        }
    }
}