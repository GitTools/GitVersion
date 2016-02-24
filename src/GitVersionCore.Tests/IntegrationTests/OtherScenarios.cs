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

                fixture.LocalRepositoryFixture.Repository.Network.Fetch(fixture.LocalRepositoryFixture.Repository.Network.Remotes.First());
                fixture.LocalRepositoryFixture.Repository.Checkout(fixture.Repository.Head.Tip);
                fixture.LocalRepositoryFixture.Repository.Branches.Remove("master");
                fixture.InitialiseRepo();
                fixture.AssertFullSemver("1.0.1+1");
            }
        }

        [Test]
        public void AllowNotHavingMaster()
        {
            using (var fixture = new EmptyRepositoryFixture(new Config()))
            {
                fixture.Repository.MakeACommit();
                fixture.Repository.MakeATaggedCommit("1.0.0");
                fixture.Repository.MakeACommit();
                fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
                fixture.Repository.Branches.Remove(fixture.Repository.Branches["master"]);

                fixture.AssertFullSemver("1.1.0-unstable.1");
            }
        }

        [Test]
        public void AllowHavingMainInsteadOfMaster()
        {
            var config = new Config();
            config.Branches.Add("main", new BranchConfig
                                            {
                                                VersioningMode = VersioningMode.ContinuousDelivery,
                                                Tag = "useBranchName",
                                                Increment = IncrementStrategy.Patch,
                                                PreventIncrementOfMergedBranchVersion = true,
                                                TrackMergeTarget = false
                                            });

            using (var fixture = new EmptyRepositoryFixture(config))
            {
                fixture.Repository.MakeACommit();
                fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
                fixture.Repository.Checkout(fixture.Repository.CreateBranch("main"));
                fixture.Repository.Branches.Remove(fixture.Repository.Branches["master"]);

                fixture.AssertFullSemver("0.1.0+0");
            }
        }

        [Test]
        public void DoNotBlowUpWhenDevelopAndFeatureBranchPointAtSameCommit()
        {
            using (var fixture = new RemoteRepositoryFixture(new Config()))
            {
                fixture.Repository.MakeACommit();
                fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
                fixture.Repository.MakeACommit();
                fixture.Repository.MakeATaggedCommit("1.0.0");
                fixture.Repository.MakeACommit();
                fixture.Repository.CreateBranch("feature/someFeature");

                fixture.LocalRepositoryFixture.Repository.Network.Fetch(fixture.LocalRepositoryFixture.Repository.Network.Remotes.First());
                fixture.LocalRepositoryFixture.Repository.Checkout(fixture.Repository.Head.Tip);
                fixture.LocalRepositoryFixture.Repository.Branches.Remove("master");
                fixture.InitialiseRepo();
                fixture.AssertFullSemver("1.1.0-unstable.1");
            }
        }
    }
}