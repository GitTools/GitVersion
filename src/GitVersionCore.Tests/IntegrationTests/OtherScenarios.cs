using System.Collections.Generic;
using System.Linq;
using GitTools.Testing;
using GitVersion;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using GitVersionCore.Tests.Helpers;
using LibGit2Sharp;
using NUnit.Framework;

namespace GitVersionCore.Tests.IntegrationTests
{
    [TestFixture]
    public class OtherScenarios : TestBase
    {
        // This is an attempt to automatically resolve the issue where you cannot build
        // when multiple branches point at the same commit
        // Current implementation favors master, then branches without - or / in their name
        [Test]
        public void DoNotBlowUpWhenMasterAndDevelopPointAtSameCommit()
        {
            using var fixture = new RemoteRepositoryFixture();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("develop");

            Commands.Fetch((Repository)fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Network.Remotes.First().Name, new string[0], new FetchOptions(), null);
            Commands.Checkout(fixture.LocalRepositoryFixture.Repository, fixture.Repository.Head.Tip);
            fixture.LocalRepositoryFixture.Repository.Branches.Remove("master");
            fixture.InitializeRepo();
            fixture.AssertFullSemver("1.0.1+1");
        }

        [Test]
        public void AllowNotHavingMaster()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.MakeACommit();
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
            fixture.Repository.Branches.Remove(fixture.Repository.Branches["master"]);

            fixture.AssertFullSemver("1.1.0-alpha.1");
        }

        [Test]
        public void AllowHavingVariantsStartingWithMaster()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.MakeACommit();
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("masterfix"));

            fixture.AssertFullSemver("1.0.1-masterfix.1+1");
        }

        [Test]
        public void AllowHavingMainInsteadOfMaster()
        {
            var config = new Config();
            config.Branches.Add("master", new BranchConfig
            {
                Regex = "main",
                VersioningMode = VersioningMode.ContinuousDelivery,
                Tag = "useBranchName",
                Increment = IncrementStrategy.Patch,
                PreventIncrementOfMergedBranchVersion = true,
                TrackMergeTarget = false,
                SourceBranches = new List<string>()
            });

            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit();
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("main"));
            fixture.Repository.Branches.Remove(fixture.Repository.Branches["master"]);

            fixture.AssertFullSemver("0.1.0+0", config);
        }

        [Test]
        public void DoNotBlowUpWhenDevelopAndFeatureBranchPointAtSameCommit()
        {
            using var fixture = new RemoteRepositoryFixture();
            fixture.Repository.MakeACommit();
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("feature/someFeature");

            Commands.Fetch((Repository)fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Network.Remotes.First().Name, new string[0], new FetchOptions(), null);
            Commands.Checkout(fixture.LocalRepositoryFixture.Repository, fixture.Repository.Head.Tip);
            fixture.LocalRepositoryFixture.Repository.Branches.Remove("master");
            fixture.InitializeRepo();
            fixture.AssertFullSemver("1.1.0-alpha.1");
        }
    }
}
