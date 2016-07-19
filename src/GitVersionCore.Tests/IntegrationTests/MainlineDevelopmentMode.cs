using System;
using System.Reflection;
using System.Text;
using GitTools.Testing;
using GitVersion;
using GitVersionCore.Tests;
using LibGit2Sharp;
using NUnit.Framework;

public class MainlineDevelopmentMode
{
    private Config config = new Config
    {
        VersioningMode = VersioningMode.Mainline
    };

    [Test]
    public void MergedFeatureBranchesToMasterImpliesRelease()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeACommit("1");
            fixture.MakeATaggedCommit("1.0.0");

            fixture.BranchTo("feature/foo", "foo");
            fixture.MakeACommit("2");
            fixture.AssertFullSemver(config, "1.0.1-foo.1+1");
            fixture.Checkout("master");
            fixture.MergeNoFF("feature/foo");

            fixture.AssertFullSemver(config, "1.0.1+2");

            fixture.BranchTo("feature/foo2", "foo2");
            fixture.MakeACommit("3 +semver: minor");
            fixture.AssertFullSemver(config, "1.1.0-foo2.1+3");
            fixture.Checkout("master");
            fixture.MergeNoFF("feature/foo2");
            fixture.AssertFullSemver(config, "1.1.0+4");

            fixture.BranchTo("feature/foo3", "foo3");
            fixture.MakeACommit("4");
            fixture.Checkout("master");
            fixture.MergeNoFF("feature/foo3");
            fixture.SequenceDiagram.NoteOver("Merge message contains '+semver: minor'", "master");
            var commit = fixture.Repository.Head.Tip;
            // Put semver increment in merge message
            fixture.Repository.Commit(commit.Message + " +semver: minor", commit.Author, commit.Committer, new CommitOptions
            {
                AmendPreviousCommit = true
            });
            fixture.AssertFullSemver(config, "1.2.0+6");

            fixture.BranchTo("feature/foo4", "foo4");
            fixture.MakeACommit("5 +semver: major");
            fixture.AssertFullSemver(config, "2.0.0-foo4.1+7");
            fixture.Checkout("master");
            fixture.MergeNoFF("feature/foo4");
            fixture.AssertFullSemver(config, "2.0.0+8");

            // We should evaluate any commits not included in merge commit calculations for direct commit/push or squash to merge commits
            fixture.MakeACommit("6 +semver: major");
            fixture.AssertFullSemver(config, "3.0.0+9");
            fixture.MakeACommit("7 +semver: minor");
            fixture.AssertFullSemver(config, "3.1.0+10");
            fixture.MakeACommit("8");
            fixture.AssertFullSemver(config, "3.1.1+11");

            // Finally verify that the merge commits still function properly
            fixture.BranchTo("feature/foo5", "foo5");
            fixture.MakeACommit("9 +semver: minor");
            fixture.AssertFullSemver(config, "3.2.0-foo5.1+12");
            fixture.Checkout("master");
            fixture.MergeNoFF("feature/foo5");
            fixture.AssertFullSemver(config, "3.2.0+13");

            // One more direct commit for good measure
            fixture.MakeACommit("10 +semver: minor");
            fixture.AssertFullSemver(config, "3.3.0+14");
            // And we can commit without bumping semver
            fixture.MakeACommit("11 +semver: none");
            fixture.AssertFullSemver(config, "3.3.0+15");
            Console.WriteLine(fixture.SequenceDiagram.GetDiagram());
        }
    }
    // Write test which has a forward merge into a feature branch
}

static class CommitExtensions
{
    public static void MakeACommit(this RepositoryFixtureBase fixture, string commitMsg)
    {
        fixture.Repository.MakeACommit(commitMsg);
        var diagramBuilder = (StringBuilder)typeof(SequenceDiagram)
            .GetField("_diagramBuilder", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(fixture.SequenceDiagram);
        Func<string, string> getParticipant = participant => (string)typeof(SequenceDiagram)
            .GetMethod("GetParticipant", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(fixture.SequenceDiagram, new object[]
            {
                    participant
            });
        diagramBuilder.AppendLineFormat("{0} -> {0}: Commit '{1}'", getParticipant(fixture.Repository.Head.FriendlyName),
            commitMsg);
    }
}