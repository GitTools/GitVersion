namespace GitVersionCore.Tests.IntegrationTests
{
    using System.Linq;
    using GitVersion;
    using LibGit2Sharp;
    using NUnit.Framework;

    [TestFixture]
    public class OtherScenarios
    {
        // This is an attempt to automatically resolve the issue where you cannot build
        // when multiple branches point at the same commit
        // Current implementation favors master, then branches without - or / in their name

        [Test]
        public void DoNotBlowUpWhenMasterAndDevelopPointAtSameCommit()
        {
            using (var fixture = new RemoteRepositoryFixture(new Config()))
            {
                fixture.Repository.MakeACommit();
                fixture.Repository.MakeATaggedCommit("1.0.0");
                fixture.Repository.MakeACommit();
                fixture.Repository.CreateBranch("develop");

                fixture.LocalRepository.Network.Fetch(fixture.LocalRepository.Network.Remotes.First());
                fixture.LocalRepository.Checkout(fixture.Repository.Head.Tip);
                fixture.LocalRepository.Branches.Remove("master");
                fixture.InitialiseRepo();
                fixture.AssertFullSemver("1.0.1+1");
            }
        }

        [Test]
        public void DoNotBlowUpWhenDevelopAndFeatureBranchPointAtSameCommit()
        {
            using (var fixture = new RemoteRepositoryFixture(new Config()))
            {
                fixture.Repository.MakeACommit();
                fixture.Repository.CreateBranch("develop").Checkout();
                fixture.Repository.MakeACommit();
                fixture.Repository.MakeATaggedCommit("1.0.0");
                fixture.Repository.MakeACommit();
                fixture.Repository.CreateBranch("feature/someFeature");

                fixture.LocalRepository.Network.Fetch(fixture.LocalRepository.Network.Remotes.First());
                fixture.LocalRepository.Checkout(fixture.Repository.Head.Tip);
                fixture.LocalRepository.Branches.Remove("master");
                fixture.InitialiseRepo();
                fixture.AssertFullSemver("1.1.0-unstable.1");
            }
        }
    }
}