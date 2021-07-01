using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using GitTools.Testing;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using LibGit2Sharp;
using NUnit.Framework;

namespace GitVersion.Core.Tests.IntegrationTests
{
    public class MainlineDevelopmentMode : TestBase
    {
        private readonly Config config = new Config
        {
            VersioningMode = VersioningMode.Mainline
        };

        [Test]
        public void VerifyNonMainMainlineVersionIdenticalAsMain()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit("1");

            fixture.BranchTo("feature/foo", "foo");
            fixture.MakeACommit("2 +semver: major");
            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("feature/foo");

            fixture.AssertFullSemver("1.0.0", config);

            fixture.BranchTo("support/1.0", "support");

            fixture.AssertFullSemver("1.0.0", config);
        }

        [Test]
        public void MergedFeatureBranchesToMainImpliesRelease()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit("1");
            fixture.MakeATaggedCommit("1.0.0");

            fixture.BranchTo("feature/foo", "foo");
            fixture.MakeACommit("2");
            fixture.AssertFullSemver("1.0.1-foo.1", config);
            fixture.MakeACommit("2.1");
            fixture.AssertFullSemver("1.0.1-foo.2", config);
            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("feature/foo");

            fixture.AssertFullSemver("1.0.1", config);

            fixture.BranchTo("feature/foo2", "foo2");
            fixture.MakeACommit("3 +semver: minor");
            fixture.AssertFullSemver("1.1.0-foo2.1", config);
            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("feature/foo2");
            fixture.AssertFullSemver("1.1.0", config);

            fixture.BranchTo("feature/foo3", "foo3");
            fixture.MakeACommit("4");
            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("feature/foo3");
            fixture.SequenceDiagram.NoteOver("Merge message contains '+semver: minor'", MainBranch);
            var commit = fixture.Repository.Head.Tip;
            // Put semver increment in merge message
            fixture.Repository.Commit(commit.Message + " +semver: minor", commit.Author, commit.Committer, new CommitOptions
            {
                AmendPreviousCommit = true
            });
            fixture.AssertFullSemver("1.2.0", config);

            fixture.BranchTo("feature/foo4", "foo4");
            fixture.MakeACommit("5 +semver: major");
            fixture.AssertFullSemver("2.0.0-foo4.1", config);
            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("feature/foo4");
            fixture.AssertFullSemver("2.0.0", config);

            // We should evaluate any commits not included in merge commit calculations for direct commit/push or squash to merge commits
            fixture.MakeACommit("6 +semver: major");
            fixture.AssertFullSemver("3.0.0", config);
            fixture.MakeACommit("7 +semver: minor");
            fixture.AssertFullSemver("3.1.0", config);
            fixture.MakeACommit("8");
            fixture.AssertFullSemver("3.1.1", config);

            // Finally verify that the merge commits still function properly
            fixture.BranchTo("feature/foo5", "foo5");
            fixture.MakeACommit("9 +semver: minor");
            fixture.AssertFullSemver("3.2.0-foo5.1", config);
            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("feature/foo5");
            fixture.AssertFullSemver("3.2.0", config);

            // One more direct commit for good measure
            fixture.MakeACommit("10 +semver: minor");
            fixture.AssertFullSemver("3.3.0", config);
            // And we can commit without bumping semver
            fixture.MakeACommit("11 +semver: none");
            fixture.AssertFullSemver("3.3.0", config);
            Console.WriteLine(fixture.SequenceDiagram.GetDiagram());
        }

        [Test]
        public void VerifyPullRequestsActLikeContinuousDelivery()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit("1");
            fixture.MakeATaggedCommit("1.0.0");
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.0.1", config);

            fixture.BranchTo("feature/foo", "foo");
            fixture.AssertFullSemver("1.0.2-foo.0", config);
            fixture.MakeACommit();
            fixture.MakeACommit();
            fixture.Repository.CreatePullRequestRef("feature/foo", MainBranch, normalise: true, prNumber: 8);
            fixture.AssertFullSemver("1.0.2-PullRequest0008.3", config);
        }

        [Test]
        public void SupportBranches()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit("1");
            fixture.MakeATaggedCommit("1.0.0");
            fixture.MakeACommit(); // 1.0.1
            fixture.MakeACommit(); // 1.0.2
            fixture.AssertFullSemver("1.0.2", config);

            fixture.BranchTo("support/1.0", "support10");
            fixture.AssertFullSemver("1.0.2", config);

            // Move main on
            fixture.Checkout(MainBranch);
            fixture.MakeACommit("+semver: major"); // 2.0.0 (on main)
            fixture.AssertFullSemver("2.0.0", config);

            // Continue on support/1.0
            fixture.Checkout("support/1.0");
            fixture.MakeACommit(); // 1.0.3
            fixture.MakeACommit(); // 1.0.4
            fixture.AssertFullSemver("1.0.4", config);
            fixture.BranchTo("feature/foo", "foo");
            fixture.AssertFullSemver("1.0.5-foo.0", config);
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.0.5-foo.1", config);
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.0.5-foo.2", config);
            fixture.Repository.CreatePullRequestRef("feature/foo", "support/1.0", normalise: true, prNumber: 7);
            fixture.AssertFullSemver("1.0.5-PullRequest0007.3", config);
        }

        [Test]
        public void VerifyForwardMerge()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit("1");
            fixture.MakeATaggedCommit("1.0.0");
            fixture.MakeACommit(); // 1.0.1

            fixture.BranchTo("feature/foo", "foo");
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.0.2-foo.1", config);
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.0.2-foo.2", config);

            fixture.Checkout(MainBranch);
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.0.2", config);
            fixture.Checkout("feature/foo");
            // This may seem surprising, but this happens because we branched off mainline
            // and incremented. Mainline has then moved on. We do not follow mainline
            // in feature branches, you need to merge mainline in to get the mainline version
            fixture.AssertFullSemver("1.0.2-foo.2", config);
            fixture.MergeNoFF(MainBranch);
            fixture.AssertFullSemver("1.0.3-foo.3", config);
        }

        [Test]
        public void VerifySupportForwardMerge()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit("1");
            fixture.MakeATaggedCommit("1.0.0");
            fixture.MakeACommit(); // 1.0.1

            fixture.BranchTo("support/1.0", "support10");
            fixture.MakeACommit();
            fixture.MakeACommit();

            fixture.Checkout(MainBranch);
            fixture.MakeACommit("+semver: minor");
            fixture.AssertFullSemver("1.1.0", config);
            fixture.MergeNoFF("support/1.0");
            fixture.AssertFullSemver("1.1.1", config);
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.1.2", config);
            fixture.Checkout("support/1.0");
            fixture.AssertFullSemver("1.0.3", config);

            fixture.BranchTo("feature/foo", "foo");
            fixture.MakeACommit();
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.0.4-foo.2", config); // TODO This probably should be 1.0.5
        }

        [Test]
        public void VerifyDevelopTracksMainVersion()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit("1");
            fixture.MakeATaggedCommit("1.0.0");
            fixture.MakeACommit();

            // branching increments the version
            fixture.BranchTo("develop");
            fixture.AssertFullSemver("1.1.0-alpha.0", config);
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.1.0-alpha.1", config);

            // merging develop into main increments minor version on main
            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("develop");
            fixture.AssertFullSemver("1.1.0", config);

            // a commit on develop before the merge still has the same version number
            fixture.Checkout("develop");
            fixture.AssertFullSemver("1.1.0-alpha.1", config);

            // moving on to further work on develop tracks main's version from the merge
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.2.0-alpha.1", config);

            // adding a commit to main increments patch
            fixture.Checkout(MainBranch);
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.1.1", config);

            // adding a commit to main doesn't change develop's version
            fixture.Checkout("develop");
            fixture.AssertFullSemver("1.2.0-alpha.1", config);
        }

        [Test]
        public void VerifyDevelopFeatureTracksMainVersion()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit("1");
            fixture.MakeATaggedCommit("1.0.0");
            fixture.MakeACommit();

            // branching increments the version
            fixture.BranchTo("develop");
            fixture.AssertFullSemver("1.1.0-alpha.0", config);
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.1.0-alpha.1", config);

            // merging develop into main increments minor version on main
            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("develop");
            fixture.AssertFullSemver("1.1.0", config);

            // a commit on develop before the merge still has the same version number
            fixture.Checkout("develop");
            fixture.AssertFullSemver("1.1.0-alpha.1", config);

            // a branch from develop before the merge tracks the pre-merge version from main
            // (note: the commit on develop looks like a commit to this branch, thus the .1)
            fixture.BranchTo("feature/foo");
            fixture.AssertFullSemver("1.0.2-foo.1", config);

            // further work on the branch tracks the merged version from main
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.1.1-foo.1", config);

            // adding a commit to main increments patch
            fixture.Checkout(MainBranch);
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.1.1", config);

            // adding a commit to main doesn't change the feature's version
            fixture.Checkout("feature/foo");
            fixture.AssertFullSemver("1.1.1-foo.1", config);

            // merging the feature to develop increments develop
            fixture.Checkout("develop");
            fixture.MergeNoFF("feature/foo");
            fixture.AssertFullSemver("1.2.0-alpha.2", config);
        }

        [Test]
        public void VerifyMergingMainToFeatureDoesNotCauseBranchCommitsToIncrementVersion()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeACommit($"first in {MainBranch}");

            fixture.BranchTo("feature/foo", "foo");
            fixture.MakeACommit("first in foo");

            fixture.Checkout(MainBranch);
            fixture.MakeACommit($"second in {MainBranch}");

            fixture.Checkout("feature/foo");
            fixture.MergeNoFF(MainBranch);
            fixture.MakeACommit("second in foo");

            fixture.Checkout(MainBranch);
            fixture.MakeATaggedCommit("1.0.0");

            fixture.MergeNoFF("feature/foo");
            fixture.AssertFullSemver("1.0.1", config);
        }

        [Test]
        public void VerifyMergingMainToFeatureDoesNotStopMainCommitsIncrementingVersion()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeACommit($"first in {MainBranch}");

            fixture.BranchTo("feature/foo", "foo");
            fixture.MakeACommit("first in foo");

            fixture.Checkout(MainBranch);
            fixture.MakeATaggedCommit("1.0.0");
            fixture.MakeACommit($"third in {MainBranch}");

            fixture.Checkout("feature/foo");
            fixture.MergeNoFF(MainBranch);
            fixture.MakeACommit("second in foo");

            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("feature/foo");
            fixture.AssertFullSemver("1.0.2", config);
        }

        [Test]
        public void VerifyIssue1154CanForwardMergeMainToFeatureBranch()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeACommit();
            fixture.BranchTo("feature/branch2");
            fixture.BranchTo("feature/branch1");
            fixture.MakeACommit();
            fixture.MakeACommit();

            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("feature/branch1");
            fixture.AssertFullSemver("0.1.1", config);

            fixture.Checkout("feature/branch2");
            fixture.MakeACommit();
            fixture.MakeACommit();
            fixture.MakeACommit();
            fixture.MergeNoFF(MainBranch);

            fixture.AssertFullSemver("0.1.2-branch2.4", config);
        }

        [Test]
        public void VerifyMergingMainIntoAFeatureBranchWorksWithMultipleBranches()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeACommit($"first in {MainBranch}");

            fixture.BranchTo("feature/foo", "foo");
            fixture.MakeACommit("first in foo");

            fixture.BranchTo("feature/bar", "bar");
            fixture.MakeACommit("first in bar");

            fixture.Checkout(MainBranch);
            fixture.MakeACommit($"second in {MainBranch}");

            fixture.Checkout("feature/foo");
            fixture.MergeNoFF(MainBranch);
            fixture.MakeACommit("second in foo");

            fixture.Checkout("feature/bar");
            fixture.MergeNoFF(MainBranch);
            fixture.MakeACommit("second in bar");

            fixture.Checkout(MainBranch);
            fixture.MakeATaggedCommit("1.0.0");

            fixture.MergeNoFF("feature/foo");
            fixture.MergeNoFF("feature/bar");
            fixture.AssertFullSemver("1.0.2", config);
        }

        [Test]
        public void MergingFeatureBranchThatIncrementsMinorNumberIncrementsMinorVersionOfMain()
        {
            var currentConfig = new Config
            {
                VersioningMode = VersioningMode.Mainline,
                Branches = new Dictionary<string, BranchConfig>
                {
                    {
                        "feature", new BranchConfig
                        {
                            VersioningMode = VersioningMode.ContinuousDeployment,
                            Increment = IncrementStrategy.Minor
                        }
                    }
                }
            };

            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeACommit($"first in {MainBranch}");
            fixture.MakeATaggedCommit("1.0.0");
            fixture.AssertFullSemver("1.0.0", currentConfig);

            fixture.BranchTo("feature/foo", "foo");
            fixture.MakeACommit("first in foo");
            fixture.MakeACommit("second in foo");
            fixture.AssertFullSemver("1.1.0-foo.2", currentConfig);

            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("feature/foo");
            fixture.AssertFullSemver("1.1.0", currentConfig);
        }

        [Test]
        public void VerifyMergeRemoteMainIncrementsAllCommits()
        {
            using var remote = new EmptyRepositoryFixture();

            remote.Repository.MakeACommit("1");
            remote.MakeATaggedCommit("1.0.0");

            using var local = remote.CloneRepository();

            local.Repository.MakeACommit("l.1");

            remote.Repository.MakeACommit("r.1");
            remote.Repository.MakeACommit("r.2");
            remote.Repository.MakeACommit("r.3");

            local.AssertFullSemver("1.0.1", config);
            remote.AssertFullSemver("1.0.3", config);

            Commands.Pull((Repository)local.Repository, Generate.SignatureNow(), new PullOptions()
            {
                MergeOptions = new MergeOptions()
                {
                    FastForwardStrategy = FastForwardStrategy.NoFastForward
                }
            });

            var latestCommitMessage = local.Repository.Head.Tip.Message;
            Assert.That(latestCommitMessage, Does.StartWith("Merge branch 'main' of "));

            // There are 5 commits including the merge commit.
            // Since all of them were in their 'main' branch, they should all be counted.
            local.AssertFullSemver("1.0.5", config);
        }

        [Test]
        public void VerifyIncrementConfigIsHonoured()
        {
            var minorIncrementConfig = new Config
            {
                VersioningMode = VersioningMode.Mainline,
                Increment = IncrementStrategy.Minor,
                Branches = new Dictionary<string, BranchConfig>
                {
                    {
                        MainBranch,
                        new BranchConfig
                        {
                            Increment = IncrementStrategy.Minor,
                            Name = MainBranch,
                            Regex = MainBranch
                        }
                    },
                    {
                        "feature",
                        new BranchConfig
                        {
                            Increment = IncrementStrategy.Minor,
                            Name = "feature",
                            Regex = "features?[/-]"
                        }
                    }
                }
            };

            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit("1");
            fixture.MakeATaggedCommit("1.0.0");

            fixture.BranchTo("feature/foo", "foo");
            fixture.MakeACommit("2");
            fixture.AssertFullSemver("1.1.0-foo.1", minorIncrementConfig);
            fixture.MakeACommit("2.1");
            fixture.AssertFullSemver("1.1.0-foo.2", minorIncrementConfig);
            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("feature/foo");

            fixture.AssertFullSemver("1.1.0", minorIncrementConfig);

            fixture.BranchTo("feature/foo2", "foo2");
            fixture.MakeACommit("3 +semver: patch");
            fixture.AssertFullSemver("1.1.1-foo2.1", minorIncrementConfig);
            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("feature/foo2");
            fixture.AssertFullSemver("1.1.1", minorIncrementConfig);

            fixture.BranchTo("feature/foo3", "foo3");
            fixture.MakeACommit("4");
            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("feature/foo3");
            fixture.SequenceDiagram.NoteOver("Merge message contains '+semver: patch'", MainBranch);
            var commit = fixture.Repository.Head.Tip;
            // Put semver increment in merge message
            fixture.Repository.Commit(commit.Message + " +semver: patch", commit.Author, commit.Committer, new CommitOptions
            {
                AmendPreviousCommit = true
            });
            fixture.AssertFullSemver("1.1.2", minorIncrementConfig);

            fixture.BranchTo("feature/foo4", "foo4");
            fixture.MakeACommit("5 +semver: major");
            fixture.AssertFullSemver("2.0.0-foo4.1", minorIncrementConfig);
            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("feature/foo4");
            fixture.AssertFullSemver("2.0.0", config);

            // We should evaluate any commits not included in merge commit calculations for direct commit/push or squash to merge commits
            fixture.MakeACommit("6 +semver: major");
            fixture.AssertFullSemver("3.0.0", minorIncrementConfig);
            fixture.MakeACommit("7");
            fixture.AssertFullSemver("3.1.0", minorIncrementConfig);
            fixture.MakeACommit("8 +semver: patch");
            fixture.AssertFullSemver("3.1.1", minorIncrementConfig);

            // Finally verify that the merge commits still function properly
            fixture.BranchTo("feature/foo5", "foo5");
            fixture.MakeACommit("9 +semver: patch");
            fixture.AssertFullSemver("3.1.2-foo5.1", minorIncrementConfig);
            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("feature/foo5");
            fixture.AssertFullSemver("3.1.2", minorIncrementConfig);

            // One more direct commit for good measure
            fixture.MakeACommit("10 +semver: patch");
            fixture.AssertFullSemver("3.1.3", minorIncrementConfig);
            // And we can commit without bumping semver
            fixture.MakeACommit("11 +semver: none");
            fixture.AssertFullSemver("3.1.3", minorIncrementConfig);
            Console.WriteLine(fixture.SequenceDiagram.GetDiagram());
        }
    }

    internal static class CommitExtensions
    {
        public static void MakeACommit(this RepositoryFixtureBase fixture, string commitMsg)
        {
            fixture.Repository.MakeACommit(commitMsg);
            var diagramBuilder = (StringBuilder)typeof(SequenceDiagram)
                .GetField("_diagramBuilder", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(fixture.SequenceDiagram);

            string GetParticipant(string participant) =>
                (string)typeof(SequenceDiagram).GetMethod("GetParticipant", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(fixture.SequenceDiagram, new object[]
                    {
                        participant
                    });

            diagramBuilder.AppendLineFormat("{0} -> {0}: Commit '{1}'", GetParticipant(fixture.Repository.Head.FriendlyName),
                commitMsg);
        }
    }
}
