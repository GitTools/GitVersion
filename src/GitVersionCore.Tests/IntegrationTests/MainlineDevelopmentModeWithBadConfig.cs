using System.Collections.Generic;
using GitTools.Testing;
using GitVersion.Configuration;
using GitVersion.VersioningModes;
using LibGit2Sharp;
using NUnit.Framework;

namespace GitVersionCore.Tests.IntegrationTests
{

    public class MainlineDevelopmentModeWithBadConfig : TestBase
    {
        private Config config = new Config
        {
            VersioningMode = VersioningMode.Mainline,
            Branches = new Dictionary<string, BranchConfig> {
                    {
                        "master", new BranchConfig()
                    },
                    {
                        "release", new BranchConfig()
                    }
                }
        };

        [Test]
        public void CorrectIsMainlineShouldGiveCorrectVersion()
        {
            config.Branches["master"].IsMainline = true;
            config.Branches["release"].IsMainline = false;

            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeACommit("1");
                var someFeature = fixture.Repository.CreateBranch("feature/some-feature");
                Commands.Checkout(fixture.Repository, "feature/some-feature");
                fixture.Repository.MakeCommits(1);

                Commands.Checkout(fixture.Repository, "master");
                fixture.AssertFullSemver("0.1.0+0");

                fixture.Repository.Merge(someFeature, Generate.SignatureNow());

                fixture.AssertFullSemver(config, "0.1.1");
            }
        }

        [Test]
        public void IncorrectIsMainlineShouldGiveRelevantErrorMessage()
        {
            config.Branches["master"].IsMainline = false;
            config.Branches["release"].IsMainline = true;

            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeACommit("1");
                var someFeature = fixture.Repository.CreateBranch("feature/some-feature");
                Commands.Checkout(fixture.Repository, "feature/some-feature");
                fixture.Repository.MakeCommits(1);

                Commands.Checkout(fixture.Repository, "master");
                fixture.AssertFullSemver("0.1.0+0");

                fixture.Repository.Merge(someFeature, Generate.SignatureNow());

                fixture.AssertFullSemver(config, "0.1.1");
            }
        }
    }
}
