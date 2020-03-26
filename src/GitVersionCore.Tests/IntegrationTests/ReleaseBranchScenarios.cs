using GitTools.Testing;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using GitVersionCore.Tests.Helpers;
using LibGit2Sharp;
using NUnit.Framework;

namespace GitVersionCore.Tests.IntegrationTests
{
    [TestFixture]
    public class ReleaseBranchScenarios : TestBase
    {
        [Test]
        public void NoMergeBacksToDevelopInCaseThereAreNoChangesInReleaseBranch()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit();
            fixture.BranchTo("develop");
            fixture.Repository.MakeCommits(3);
            var releaseBranch = fixture.Repository.CreateBranch("release/1.0.0");
            fixture.Checkout("master");
            fixture.Repository.MergeNoFF("release/1.0.0");
            fixture.Repository.ApplyTag("1.0.0");
            fixture.Checkout("develop");

            fixture.Repository.Branches.Remove(releaseBranch);

            fixture.AssertFullSemver("1.1.0-alpha.0");
        }

        [Test]
        public void NoMergeBacksToDevelopInCaseThereAreChangesInReleaseBranch()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit();
            fixture.BranchTo("develop");
            fixture.Repository.MakeCommits(3);
            fixture.BranchTo("release/1.0.0");
            fixture.Repository.MakeACommit();

            // Merge to master
            fixture.Checkout("master");
            fixture.Repository.MergeNoFF("release/1.0.0");
            fixture.Repository.ApplyTag("1.0.0");

            // Merge to develop
            fixture.Checkout("develop");
            fixture.Repository.MergeNoFF("release/1.0.0");
            fixture.AssertFullSemver("1.1.0-alpha.2");

            fixture.Repository.MakeACommit();
            fixture.Repository.Branches.Remove("release/1.0.0");

            fixture.AssertFullSemver("1.1.0-alpha.3");
        }

        [Test]
        public void CanTakeVersionFromReleaseBranch()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Checkout("release-2.0.0");

            fixture.AssertFullSemver("2.0.0-beta.1+0");
            fixture.Repository.MakeCommits(2);
            fixture.AssertFullSemver("2.0.0-beta.1+2");
        }

        [Test]
        public void CanTakeVersionFromReleasesBranch()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("releases/2.0.0");
            fixture.Checkout("releases/2.0.0");

            fixture.AssertFullSemver("2.0.0-beta.1+0");
            fixture.Repository.MakeCommits(2);
            fixture.AssertFullSemver("2.0.0-beta.1+2");
        }

        [Test]
        public void ReleaseBranchWithNextVersionSetInConfig()
        {
            var config = new Config
            {
                NextVersion = "2.0.0"
            };
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeCommits(5);
            fixture.BranchTo("release-2.0.0");

            fixture.AssertFullSemver("2.0.0-beta.1+0", config);
            fixture.Repository.MakeCommits(2);
            fixture.AssertFullSemver("2.0.0-beta.1+2", config);
        }

        [Test]
        public void CanTakeVersionFromReleaseBranchWithTagOverridden()
        {
            var config = new Config
            {
                Branches =
                {
                    { "release", new BranchConfig { Tag = "rc" } }
                }
            };
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Checkout("release-2.0.0");

            fixture.AssertFullSemver("2.0.0-rc.1+0", config);
            fixture.Repository.MakeCommits(2);
            fixture.AssertFullSemver("2.0.0-rc.1+2", config);
        }

        [Test]
        public void CanHandleReleaseBranchWithStability()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("release-2.0.0-Final");
            fixture.Checkout("release-2.0.0-Final");

            fixture.AssertFullSemver("2.0.0-beta.1+0");
            fixture.Repository.MakeCommits(2);
            fixture.AssertFullSemver("2.0.0-beta.1+2");
        }

        [Test]
        public void WhenReleaseBranchOffDevelopIsMergedIntoMasterAndDevelopVersionIsTakenWithIt()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.MakeCommits(1);

            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

            fixture.AssertFullSemver("2.0.0+0");
            fixture.Repository.MakeCommits(2);
            fixture.AssertFullSemver("2.0.0+2");
        }

        [Test]
        public void WhenReleaseBranchOffMasterIsMergedIntoMasterVersionIsTakenWithIt()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(1);
            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

            fixture.AssertFullSemver("2.0.0+0");
        }

        [Test]
        public void MasterVersioningContinuousCorrectlyAfterMergingReleaseBranch()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(1);
            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

            fixture.AssertFullSemver("2.0.0+0");
            fixture.Repository.ApplyTag("2.0.0");
            fixture.Repository.MakeCommits(1);
            fixture.AssertFullSemver("2.0.1+1");
        }

        [Test]
        public void WhenReleaseBranchIsMergedIntoDevelopHighestVersionIsTakenWithIt()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.MakeCommits(1);

            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Checkout("develop");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

            fixture.Repository.CreateBranch("release-1.0.0");
            fixture.Checkout("release-1.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Checkout("develop");
            fixture.Repository.MergeNoFF("release-1.0.0", Generate.SignatureNow());

            fixture.AssertFullSemver("2.1.0-alpha.11");
        }

        [Test]
        public void WhenReleaseBranchIsMergedIntoMasterHighestVersionIsTakenWithIt()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(1);

            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

            fixture.Repository.CreateBranch("release-1.0.0");
            fixture.Checkout("release-1.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Checkout("master");
            fixture.Repository.MergeNoFF("release-1.0.0", Generate.SignatureNow());

            fixture.AssertFullSemver("2.0.0+5");
        }

        [Test]
        public void WhenReleaseBranchIsMergedIntoMasterHighestVersionIsTakenWithItEvenWithMoreThanTwoActiveBranches()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(1);

            fixture.Repository.CreateBranch("release-3.0.0");
            fixture.Checkout("release-3.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Checkout("master");
            fixture.Repository.MergeNoFF("release-3.0.0", Generate.SignatureNow());

            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

            fixture.Repository.CreateBranch("release-1.0.0");
            fixture.Checkout("release-1.0.0");
            fixture.Repository.MakeCommits(4);
            fixture.Checkout("master");
            fixture.Repository.MergeNoFF("release-1.0.0", Generate.SignatureNow());

            fixture.AssertFullSemver("3.0.0+10");
        }

        [Test]
        public void WhenMergingReleaseBackToDevShouldNotResetBetaVersion()
        {
            using var fixture = new EmptyRepositoryFixture();
            const string taggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(taggedVersion);
            fixture.Repository.CreateBranch("develop");
            fixture.Checkout("develop");

            fixture.Repository.MakeCommits(1);
            fixture.AssertFullSemver("1.1.0-alpha.1");

            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(1);

            fixture.AssertFullSemver("2.0.0-beta.1+1");

            //tag it to bump to beta 2
            fixture.Repository.ApplyTag("2.0.0-beta1");

            fixture.Repository.MakeCommits(1);

            fixture.AssertFullSemver("2.0.0-beta.2+2");

            //merge down to develop
            fixture.Checkout("develop");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

            //but keep working on the release
            fixture.Checkout("release-2.0.0");
            fixture.AssertFullSemver("2.0.0-beta.2+2");
            fixture.MakeACommit();
            fixture.AssertFullSemver("2.0.0-beta.2+3");
        }

        [Test]
        public void HotfixOffReleaseBranchShouldNotResetCount()
        {
            var config = new Config
            {
                VersioningMode = VersioningMode.ContinuousDeployment
            };
            using var fixture = new EmptyRepositoryFixture();
            const string taggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(taggedVersion);
            fixture.Repository.CreateBranch("develop");
            fixture.Checkout("develop");

            fixture.Repository.MakeCommits(1);

            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Checkout("release-2.0.0");
            fixture.Repository.MakeCommits(1);

            fixture.AssertFullSemver("2.0.0-beta.1", config);

            //tag it to bump to beta 2
            fixture.Repository.MakeCommits(4);

            fixture.AssertFullSemver("2.0.0-beta.5", config);

            //merge down to develop
            fixture.Repository.CreateBranch("hotfix-2.0.0");
            fixture.Repository.MakeCommits(2);

            //but keep working on the release
            fixture.Checkout("release-2.0.0");
            fixture.Repository.MergeNoFF("hotfix-2.0.0", Generate.SignatureNow());
            fixture.Repository.Branches.Remove(fixture.Repository.Branches["hotfix-2.0.0"]);
            fixture.AssertFullSemver("2.0.0-beta.7", config);
        }

        [Test]
        public void MergeOnReleaseBranchShouldNotResetCount()
        {
            var config = new Config
            {
                AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatchTag,
                VersioningMode = VersioningMode.ContinuousDeployment,
            };
            using var fixture = new EmptyRepositoryFixture();
            const string taggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(taggedVersion);
            fixture.Repository.CreateBranch("develop");
            fixture.Checkout("develop");
            fixture.Repository.MakeACommit();

            fixture.Repository.CreateBranch("release/2.0.0");

            fixture.Repository.CreateBranch("release/2.0.0-xxx");
            fixture.Checkout("release/2.0.0-xxx");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("2.0.0-beta.1", config);

            fixture.Checkout("release/2.0.0");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("2.0.0-beta.1", config);

            fixture.Repository.MergeNoFF("release/2.0.0-xxx");
            fixture.AssertFullSemver("2.0.0-beta.2", config);
        }

        [Test]
        public void CommitOnDevelopAfterReleaseBranchMergeToDevelopShouldNotResetCount()
        {
            var config = new Config
            {
                VersioningMode = VersioningMode.ContinuousDeployment
            };

            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeACommit("initial");
            fixture.BranchTo("develop");

            // Create release from develop
            fixture.BranchTo("release-2.0.0");
            fixture.AssertFullSemver("2.0.0-beta.0", config);

            // Make some commits on release
            fixture.MakeACommit("release 1");
            fixture.MakeACommit("release 2");
            fixture.AssertFullSemver("2.0.0-beta.2", config);

            // First forward merge release to develop
            fixture.Checkout("develop");
            fixture.MergeNoFF("release-2.0.0");

            // Make some new commit on release
            fixture.Checkout("release-2.0.0");
            fixture.Repository.MakeACommit("release 3 - after first merge");
            fixture.AssertFullSemver("2.0.0-beta.3", config);

            // Make new commit on develop
            fixture.Checkout("develop");
            // Checkout to release (no new commits)
            fixture.Checkout("release-2.0.0");
            fixture.AssertFullSemver("2.0.0-beta.3", config);
            fixture.Checkout("develop");
            fixture.Repository.MakeACommit("develop after merge");

            // Checkout to release (no new commits)
            fixture.Checkout("release-2.0.0");
            fixture.AssertFullSemver("2.0.0-beta.3", config);

            // Make some new commit on release
            fixture.Repository.MakeACommit("release 4");
            fixture.Repository.MakeACommit("release 5");
            fixture.AssertFullSemver("2.0.0-beta.5", config);

            // Second merge release to develop
            fixture.Checkout("develop");
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

            // Checkout to release (no new commits)
            fixture.Checkout("release-2.0.0");
            fixture.AssertFullSemver("2.0.0-beta.5", config);
        }

        [Test]
        public void CommitBeetweenMergeReleaseToDevelopShouldNotResetCount()
        {
            var config = new Config
            {
                VersioningMode = VersioningMode.ContinuousDeployment
            };

            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit("initial");
            fixture.Repository.CreateBranch("develop");
            Commands.Checkout(fixture.Repository, "develop");
            fixture.Repository.CreateBranch("release-2.0.0");
            Commands.Checkout(fixture.Repository, "release-2.0.0");
            fixture.AssertFullSemver("2.0.0-beta.0", config);

            // Make some commits on release
            var commit1 = fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("2.0.0-beta.2", config);

            // Merge release to develop - emulate commit beetween other person release commit push and this commit merge to develop
            Commands.Checkout(fixture.Repository, "develop");
            fixture.Repository.Merge(commit1, Generate.SignatureNow(), new MergeOptions { FastForwardStrategy = FastForwardStrategy.NoFastForward });
            fixture.Repository.MergeNoFF("release-2.0.0", Generate.SignatureNow());

            // Check version on release after merge to develop
            Commands.Checkout(fixture.Repository, "release-2.0.0");
            fixture.AssertFullSemver("2.0.0-beta.2", config);

            // Check version on release after making some new commits
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("2.0.0-beta.4", config);
        }

        public void ReleaseBranchShouldUseBranchNameVersionDespiteBumpInPreviousCommit()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0");
            fixture.Repository.MakeACommit("+semver:major");
            fixture.Repository.MakeACommit();

            fixture.BranchTo("release/2.0");

            fixture.AssertFullSemver("2.0.0-beta.1+2");
        }

        [Test]
        public void ReleaseBranchWithACommitShouldUseBranchNameVersionDespiteBumpInPreviousCommit()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0");
            fixture.Repository.MakeACommit("+semver:major");
            fixture.Repository.MakeACommit();

            fixture.BranchTo("release/2.0");

            fixture.Repository.MakeACommit();

            fixture.AssertFullSemver("2.0.0-beta.1+3");
        }

        [Test]
        public void ReleaseBranchedAtCommitWithSemverMessageShouldUseBranchNameVersion()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0");
            fixture.Repository.MakeACommit("+semver:major");

            fixture.BranchTo("release/2.0");

            fixture.AssertFullSemver("2.0.0-beta.1+1");
        }

        [Test]
        public void FeatureFromReleaseBranchShouldNotResetCount()
        {
            var config = new Config
            {
                VersioningMode = VersioningMode.ContinuousDeployment
            };

            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit("initial");
            fixture.Repository.CreateBranch("develop");
            Commands.Checkout(fixture.Repository, "develop");
            fixture.Repository.CreateBranch("release-2.0.0");
            Commands.Checkout(fixture.Repository, "release-2.0.0");
            fixture.AssertFullSemver("2.0.0-beta.0", config);

            // Make some commits on release
            fixture.Repository.MakeCommits(10);
            fixture.AssertFullSemver("2.0.0-beta.10", config);

            // Create feature from release
            fixture.BranchTo("feature/xxx");
            fixture.Repository.MakeACommit("feature 1");
            fixture.Repository.MakeACommit("feature 2");

            // Check version on release
            Commands.Checkout(fixture.Repository, "release-2.0.0");
            fixture.AssertFullSemver("2.0.0-beta.10", config);
            fixture.Repository.MakeACommit("release 11");
            fixture.AssertFullSemver("2.0.0-beta.11", config);

            // Make new commit on feature
            Commands.Checkout(fixture.Repository, "feature/xxx");
            fixture.Repository.MakeACommit("feature 3");

            // Checkout to release (no new commits)
            Commands.Checkout(fixture.Repository, "release-2.0.0");
            fixture.AssertFullSemver("2.0.0-beta.11", config);

            // Merge feature to release
            fixture.Repository.MergeNoFF("feature/xxx", Generate.SignatureNow());
            fixture.AssertFullSemver("2.0.0-beta.15", config);

            fixture.Repository.MakeACommit("release 13 - after feature merge");
            fixture.AssertFullSemver("2.0.0-beta.16", config);
        }

        [Test]
        public void AssemblySemFileVerShouldBeWeightedByPreReleaseWeight()
        {
            var config = new Config
            {
                AssemblyFileVersioningFormat = "{Major}.{Minor}.{Patch}.{WeightedPreReleaseNumber}",
                Branches =
                {
                    { "release", new BranchConfig
                        {
                            PreReleaseWeight = 1000
                        }
                    }
                }
            };
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Checkout("release-2.0.0");
            config.Reset();
            var variables = fixture.GetVersion(config);
            Assert.AreEqual(variables.AssemblySemFileVer, "2.0.0.1001");
        }

        [Test]
        public void AssemblySemFileVerShouldBeWeightedByDefaultPreReleaseWeight()
        {
            var config = new Config
            {
                AssemblyFileVersioningFormat = "{Major}.{Minor}.{Patch}.{WeightedPreReleaseNumber}",
            };
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.3");
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Checkout("release-2.0.0");
            config.Reset();
            var variables = fixture.GetVersion(config);
            Assert.AreEqual(variables.AssemblySemFileVer, "2.0.0.30001");
        }

        /// <summary>
        /// Create a feature branch from a release branch, and merge back, then delete it
        /// </summary>
        [Test]
        public void FeatureOnReleaseFeatureBranchDeleted()
        {
            var config = new Config
            {
                AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatchTag,
                VersioningMode = VersioningMode.ContinuousDeployment
            };

            using var fixture = new EmptyRepositoryFixture();
            var release450 = "release/4.5.0";
            var featureBranch = "feature/some-bug-fix";

            fixture.Repository.MakeACommit("initial");
            fixture.Repository.CreateBranch("develop");
            Commands.Checkout(fixture.Repository, "develop");

            // begin the release branch
            fixture.Repository.CreateBranch(release450);
            Commands.Checkout(fixture.Repository, release450);
            fixture.AssertFullSemver("4.5.0-beta.0", config);

            fixture.Repository.CreateBranch(featureBranch);
            Commands.Checkout(fixture.Repository, featureBranch);
            fixture.Repository.MakeACommit("blabla"); // commit 1
            Commands.Checkout(fixture.Repository, release450);
            fixture.Repository.MergeNoFF(featureBranch, Generate.SignatureNow()); // commit 2
            fixture.Repository.Branches.Remove(featureBranch);

            fixture.AssertFullSemver("4.5.0-beta.2", config);
        }

        /// <summary>
        /// Create a feature branch from a release branch, and merge back, but don't delete it
        /// </summary>
        [Test]
        public void FeatureOnReleaseFeatureBranchNotDeleted()
        {
            var config = new Config
            {
                AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatchTag,
                VersioningMode = VersioningMode.ContinuousDeployment
            };

            using var fixture = new EmptyRepositoryFixture();
            var release450 = "release/4.5.0";
            var featureBranch = "feature/some-bug-fix";

            fixture.Repository.MakeACommit("initial");
            fixture.Repository.CreateBranch("develop");
            Commands.Checkout(fixture.Repository, "develop");

            // begin the release branch
            fixture.Repository.CreateBranch(release450);
            Commands.Checkout(fixture.Repository, release450);
            fixture.AssertFullSemver("4.5.0-beta.0", config);

            fixture.Repository.CreateBranch(featureBranch);
            Commands.Checkout(fixture.Repository, featureBranch);
            fixture.Repository.MakeACommit("blabla"); // commit 1
            Commands.Checkout(fixture.Repository, release450);
            fixture.Repository.MergeNoFF(featureBranch, Generate.SignatureNow()); // commit 2

            fixture.AssertFullSemver("4.5.0-beta.2", config);
        }
    }
}
