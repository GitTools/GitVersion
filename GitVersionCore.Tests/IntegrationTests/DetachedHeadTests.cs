
namespace GitVersionCore.Tests.IntegrationTests
{
    using GitVersion;
    using GitVersionCore.Tests.Fixtures;
    using LibGit2Sharp;
    using NUnit.Framework;
    
    [TestFixture]
    public class DetachedHeadTests
    {


        [Test, ExpectedException(typeof(WarningException), ExpectedMessage = "It looks like the branch being examined is a detached Head pointing to commit '97985dd'. Without a proper branch name GitVersion cannot determine the build version.")]
        public void GivenARemoteGitRepositoryWithMergeCommitsAndMasterAndDevelopBranchesUsingExistingImplementation_ItShouldThrowWarningExcpetionNoBranch()
        {
            using (var fixture = new RemoteRepositoryFixture(new Config()))
            {
                // force old behaviour
                GitVersionContext.IsContextForTrackedBrancesOnly = false;

                fixture.Repository.Checkout(fixture.Repository.Head.Tip);

                // When
                fixture.AssertFullSemver("0.1.0");
            }
        }

        [Test]
        public void GivenARemoteRepositoryWithMergeCommitsAndMasterAndDevelopBranchesUsingNewImplementation_ItShouldReturnVersionNumber()
        {
            using (var fixture = new RemoteRepositoryFixture(new Config()))
            {
                fixture.Repository.Checkout(fixture.Repository.Head.Tip);

                // When
                fixture.AssertFullSemver("0.1.0");
            }
        }

    }
}
