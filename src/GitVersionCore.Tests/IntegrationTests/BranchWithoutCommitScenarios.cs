using GitTools.Testing;
using GitVersionCore.Tests.Helpers;
using LibGit2Sharp;
using NUnit.Framework;

namespace GitVersionCore.Tests.IntegrationTests
{
    [TestFixture]
    public class BranchWithoutCommitScenarios : TestBase
    {
        [Test]
        public void CanTakeVersionFromReleaseBranch()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.3");
            var commit = fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("release-4.0.123");
            fixture.Checkout(commit.Sha);

            fixture.AssertFullSemver("4.0.123-beta.1+0", null, fixture.Repository, commit.Sha, onlyTrackedBranches: false, targetBranch: "release-4.0.123");
        }

        [Test]
        public void BranchVersionHavePrecedenceOverTagVersionIfVersionGreaterThanTag()
        {
            using var fixture = new EmptyRepositoryFixture();

            fixture.Repository.MakeACommit();

            fixture.Repository.CreateBranch("develop");
            fixture.Checkout("develop");
            fixture.MakeATaggedCommit("0.1.0-alpha.1"); // simulate merge from feature branch

            fixture.Repository.CreateBranch("release/1.0");
            fixture.Checkout("release/1.0");

            fixture.AssertFullSemver("1.0.0-beta.1+0");
        }
		
        [Test]
        public void BetaBranchCreatedButStillOnTaggedAlphaCommitShouldCreateBetaVersion()
        {
            // Arrange
            var config = new Config
            {
                Branches =
                {
                    { "release", new BranchConfig { Tag = "beta" } },
                    { "develop", new BranchConfig { Tag = "alpha" } }
                }
            };

            // Act
            using var fixture = new BaseGitFlowRepositoryFixture("1.0.0");
            fixture.Checkout("develop");
            fixture.MakeATaggedCommit("1.1.0-alpha.1"); // assuming this has been build as 1.1.0-alpha.1
			
            fixture.BranchTo("release/1.1.0"); // about to be released, no additional empty commit in this scenario!
            fixture.Checkout("release/1.1.0"); // still on the same commit, but another branch, choosing to build same code as beta now
            
			// Assert
			fixture.AssertFullSemver("1.1.0-beta.1", config); //will be 1.1.0-alpha.1, should be 1.1.0-beta.1. Tag is an "alpha" tag from develop branch, only "beta" tags should count when on release branch. If no beta tag found, build new beta version on release branch.
			
            fixture.Checkout("develop"); // back to develop
            fixture.AssertFullSemver("1.1.0-alpha.1", config); //will be 1.1.0-alpha.1 based on tag (as before)

        }		
    }
}
