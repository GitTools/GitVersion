using System.Collections.Generic;
using GitTools.Testing;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using GitVersionCore.Tests.Helpers;
using LibGit2Sharp;
using NUnit.Framework;

namespace GitVersionCore.Tests.IntegrationTests
{
    [TestFixture]
    public class DevelopScenarios : TestBase
    {
        [Test]
        public void WhenDevelopHasMultipleCommitsSpecifyExistingCommitId()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));

            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();
            var thirdCommit = fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();

            fixture.AssertFullSemver("1.1.0-alpha.3", commitId: thirdCommit.Sha);
        }

        [Test]
        public void WhenDevelopHasMultipleCommitsSpecifyNonExistingCommitId()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));

            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();

            fixture.AssertFullSemver("1.1.0-alpha.5", commitId: "nonexistingcommitid");
        }

        [Test]
        public void WhenDevelopBranchedFromTaggedCommitOnMasterVersionDoesNotChange()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
            fixture.AssertFullSemver("1.0.0");
        }

        [Test]
        public void CanChangeDevelopTagViaConfig()
        {
            var config = new Config
            {
                Branches =
                {
                    {
                        "develop", new BranchConfig
                        {
                            Tag = "alpha",
                            SourceBranches = new List<string>()
                        }
                    }
                }
            };
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.1.0-alpha.1", config);
        }

        [Test]
        public void WhenDeveloperBranchExistsDontTreatAsDevelop()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("developer"));
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.0.1-developer.1+1"); // this tag should be the branch name by default, not unstable
        }

        [Test]
        public void WhenDevelopBranchedFromMasterMinorIsIncreased()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.1.0-alpha.1");
        }

        [Test]
        public void MergingReleaseBranchBackIntoDevelopWithMergingToMasterDoesBumpDevelopVersion()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
            fixture.Repository.MakeACommit();
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("release-2.0.0"));
            fixture.Repository.MakeACommit();
            Commands.Checkout(fixture.Repository, "master");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

            Commands.Checkout(fixture.Repository, "develop");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());
            fixture.AssertFullSemver("2.1.0-alpha.2");
        }

        [Test]
        public void CanHandleContinuousDelivery()
        {
            var config = new Config
            {
                Branches =
                {
                    {"develop", new BranchConfig
                        {
                            VersioningMode = VersioningMode.ContinuousDelivery
                        }
                    }
                }
            };
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
            fixture.Repository.MakeATaggedCommit("1.1.0-alpha7");
            fixture.AssertFullSemver("1.1.0-alpha.7", config);
        }

        [Test]
        public void WhenDevelopBranchedFromMasterDetachedHeadMinorIsIncreased()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
            fixture.Repository.MakeACommit();
            var commit = fixture.Repository.Head.Tip;
            fixture.Repository.MakeACommit();
            Commands.Checkout(fixture.Repository, commit);
            fixture.AssertFullSemver("1.1.0-alpha.1", onlyTrackedBranches: false);
        }

        [Test]
        public void InheritVersionFromReleaseBranch()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeATaggedCommit("1.0.0");
            fixture.BranchTo("develop");
            fixture.MakeACommit();
            fixture.BranchTo("release/2.0.0");
            fixture.MakeACommit();
            fixture.MakeACommit();
            fixture.Checkout("develop");
            fixture.AssertFullSemver("1.1.0-alpha.1");
            fixture.MakeACommit();
            fixture.AssertFullSemver("2.1.0-alpha.1");
            fixture.MergeNoFF("release/2.0.0");
            fixture.AssertFullSemver("2.1.0-alpha.4");
            fixture.BranchTo("feature/MyFeature");
            fixture.MakeACommit();
            fixture.AssertFullSemver("2.1.0-MyFeature.1+5");
        }

        [Test]
        public void WhenMultipleDevelopBranchesExistAndCurrentBranchHasIncrementInheritPolicyAndCurrentCommitIsAMerge()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("bob_develop");
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.CreateBranch("feature/x");

            Commands.Checkout(fixture.Repository, "develop");
            fixture.Repository.MakeACommit();

            Commands.Checkout(fixture.Repository, "feature/x");
            fixture.Repository.MakeACommit();
            fixture.Repository.MergeNoFF("develop");

            fixture.AssertFullSemver("1.1.0-x.1+3");
        }

        [Test]
        public void TagOnHotfixShouldNotAffectDevelop()
        {
            using var fixture = new BaseGitFlowRepositoryFixture("1.2.0");
            Commands.Checkout(fixture.Repository, "master");
            var hotfix = fixture.Repository.CreateBranch("hotfix-1.2.1");
            Commands.Checkout(fixture.Repository, hotfix);
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.2.1-beta.1+1");
            fixture.Repository.ApplyTag("1.2.1-beta.1");
            fixture.AssertFullSemver("1.2.1-beta.1");
            Commands.Checkout(fixture.Repository, "develop");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.3.0-alpha.2");
        }

        [Test]
        public void CommitsSinceVersionSourceShouldNotGoDownUponGitFlowReleaseFinish()
        {
            var config = new Config
            {
                VersioningMode = VersioningMode.ContinuousDeployment
            };

            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeACommit();
            fixture.ApplyTag("1.1.0");
            fixture.BranchTo("develop");
            fixture.MakeACommit("commit in develop - 1");
            fixture.AssertFullSemver("1.2.0-alpha.1");
            fixture.BranchTo("release/1.2.0");
            fixture.AssertFullSemver("1.2.0-beta.1+0");
            fixture.Checkout("develop");
            fixture.MakeACommit("commit in develop - 2");
            fixture.MakeACommit("commit in develop - 3");
            fixture.MakeACommit("commit in develop - 4");
            fixture.MakeACommit("commit in develop - 5");
            fixture.AssertFullSemver("1.3.0-alpha.4");
            fixture.Checkout("release/1.2.0");
            fixture.MakeACommit("commit in release/1.2.0 - 1");
            fixture.MakeACommit("commit in release/1.2.0 - 2");
            fixture.MakeACommit("commit in release/1.2.0 - 3");
            fixture.AssertFullSemver("1.2.0-beta.1+3");
            fixture.Checkout("master");
            fixture.MergeNoFF("release/1.2.0");
            fixture.ApplyTag("1.2.0");
            fixture.Checkout("develop");
            fixture.MergeNoFF("release/1.2.0");
            fixture.MakeACommit("commit in develop - 6");
            fixture.AssertFullSemver("1.3.0-alpha.9");
            fixture.SequenceDiagram.Destroy("release/1.2.0");
            fixture.Repository.Branches.Remove("release/1.2.0");

            var expectedFullSemVer = "1.3.0-alpha.9";
            fixture.AssertFullSemver(expectedFullSemVer, config);
        }

        [Test]
        public void CommitsSinceVersionSourceShouldNotGoDownUponMergingFeatureOnlyToDevelop()
        {
            var config = new Config
            {
                VersioningMode = VersioningMode.ContinuousDeployment
            };

            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeACommit("commit in master - 1");
            fixture.ApplyTag("1.1.0");
            fixture.BranchTo("develop");
            fixture.MakeACommit("commit in develop - 1");
            fixture.AssertFullSemver("1.2.0-alpha.1");
            fixture.BranchTo("release/1.2.0");
            fixture.MakeACommit("commit in release - 1");
            fixture.MakeACommit("commit in release - 2");
            fixture.MakeACommit("commit in release - 3");
            fixture.AssertFullSemver("1.2.0-beta.1+3");
            fixture.ApplyTag("1.2.0");
            fixture.Checkout("develop");
            fixture.MakeACommit("commit in develop - 2");
            fixture.AssertFullSemver("1.3.0-alpha.1");
            fixture.MergeNoFF("release/1.2.0");
            fixture.AssertFullSemver("1.3.0-alpha.5");
            fixture.SequenceDiagram.Destroy("release/1.2.0");
            fixture.Repository.Branches.Remove("release/1.2.0");

            var expectedFullSemVer = "1.3.0-alpha.5";
            fixture.AssertFullSemver(expectedFullSemVer, config);
        }

        [Test]
        public void PreviousPreReleaseTagShouldBeRespectedWhenCountingCommits()
        {
            using var fixture = new EmptyRepositoryFixture();

            fixture.Repository.MakeACommit();

            fixture.BranchTo("develop");
            fixture.MakeATaggedCommit("1.0.0-alpha.3"); // manual bump version

            fixture.MakeACommit();
            fixture.MakeACommit();

            fixture.AssertFullSemver("1.0.0-alpha.5");
        }
    }
}
