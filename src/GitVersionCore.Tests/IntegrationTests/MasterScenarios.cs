using GitTools.Testing;
using LibGit2Sharp;
using NUnit.Framework;
using GitVersion.Configuration;
using GitVersion.VersioningModes;

namespace GitVersionCore.Tests.IntegrationTests
{
    [TestFixture]
    public class MasterScenarios : TestBase
    {
        [Test]
        public void CanHandleContinuousDelivery()
        {
            var config = new Config
            {
                Branches =
                {
                    {
                        "master", new BranchConfig
                        {
                            VersioningMode = VersioningMode.ContinuousDelivery
                        }
                    }
                }
            };
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.MakeCommits(2);
            fixture.AssertFullSemver(config, "1.0.1+2");
        }

        [Test]
        public void CanHandleContinuousDeployment()
        {
            var config = new Config
            {
                Branches =
                {
                    {
                        "master", new BranchConfig
                        {
                            VersioningMode = VersioningMode.ContinuousDeployment
                        }
                    }
                }
            };
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.MakeCommits(2);
            fixture.AssertFullSemver(config, "1.0.1-ci.2");
        }

        [Test]
        public void GivenARepositoryWithCommitsButNoTagsVersionShouldBe01()
        {
            using var fixture = new EmptyRepositoryFixture();
            // Given
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();

            // When
            fixture.AssertFullSemver("0.1.0+2");
        }

        [Test]
        public void GivenARepositoryWithCommitsButBadTagsVersionShouldBe01()
        {
            using var fixture = new EmptyRepositoryFixture();
            // Given
            fixture.Repository.MakeACommit();
            fixture.Repository.ApplyTag("BadTag");
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();

            // When
            fixture.AssertFullSemver("0.1.0+2");
        }

        [Test]
        public void GivenARepositoryWithCommitsButNoTagsWithDetachedHeadVersionShouldBe01()
        {
            using var fixture = new EmptyRepositoryFixture();
            // Given
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();

            var commit = fixture.Repository.Head.Tip;
            fixture.Repository.MakeACommit();
            Commands.Checkout(fixture.Repository, commit);

            // When
            fixture.AssertFullSemver("0.1.0+2");
        }

        [Test]
        public void GivenARepositoryWithTagAndNextVersionInConfigVersionShouldMatchVersionTxtFile()
        {
            const string expectedNextVersion = "1.1.0";
            var config = new Config { NextVersion = expectedNextVersion };
            using var fixture = new EmptyRepositoryFixture();
            const string taggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(taggedVersion);
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver(config, "1.1.0+5");
        }

        [Test]
        public void GivenARepositoryWithTagAndANextVersionTxtFileAndNoCommitsVersionShouldBeTag()
        {
            using var fixture = new EmptyRepositoryFixture();
            const string taggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(taggedVersion);

            fixture.AssertFullSemver(new Config { NextVersion = "1.1.0" }, "1.0.3");
        }

        [Test]
        public void GivenARepositoryWithTagAndNoNextVersionTxtFileVersionShouldBeTagWithBumpedPatch()
        {
            using var fixture = new EmptyRepositoryFixture();
            const string taggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(taggedVersion);
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver("1.0.4+5");
        }

        [Test]
        public void GivenARepositoryWithTagAndNoNextVersionTxtFileAndNoCommitsVersionShouldBeTag()
        {
            using var fixture = new EmptyRepositoryFixture();
            const string taggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(taggedVersion);

            fixture.AssertFullSemver("1.0.3");
        }

        [Test]
        public void GivenARepositoryWithTagAndOldNextVersionConfigVersionShouldBeTagWithBumpedPatch()
        {
            using var fixture = new EmptyRepositoryFixture();
            const string taggedVersion = "1.1.0";
            fixture.Repository.MakeATaggedCommit(taggedVersion);
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver(new Config { NextVersion = "1.0.0" }, "1.1.1+5");
        }

        [Test]
        public void GivenARepositoryWithTagAndOldNextVersionConfigAndNoCommitsVersionShouldBeTag()
        {
            using var fixture = new EmptyRepositoryFixture();
            const string taggedVersion = "1.1.0";
            fixture.Repository.MakeATaggedCommit(taggedVersion);

            fixture.AssertFullSemver(new Config { NextVersion = "1.0.0" }, "1.1.0");
        }

        [Test]
        public void CanSpecifyTagPrefixes()
        {
            using var fixture = new EmptyRepositoryFixture();
            const string taggedVersion = "version-1.0.3";
            fixture.Repository.MakeATaggedCommit(taggedVersion);
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver(new Config { TagPrefix = "version-" }, "1.0.4+5");
        }    

        [Test]
        public void CanSpecifyTagPrefixesAsRegex()
        {
            var config = new Config { TagPrefix = "version-|[vV]" };
            using var fixture = new EmptyRepositoryFixture();
            var taggedVersion = "v1.0.3";
            fixture.Repository.MakeATaggedCommit(taggedVersion);
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver(config, "1.0.4+5");

            taggedVersion = "version-1.0.5";
            fixture.Repository.MakeATaggedCommit(taggedVersion);
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver(config, "1.0.6+5");
        }

        [Test]
        public void AreTagsNotAdheringToTagPrefixIgnored()
        {
            var config = new Config { TagPrefix = "" };
            using var fixture = new EmptyRepositoryFixture();
            var taggedVersion = "version-1.0.3";
            fixture.Repository.MakeATaggedCommit(taggedVersion);
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver(config, "0.1.0+5");    //Fallback version + 5 commits since tag

            taggedVersion = "bad/1.0.3";
            fixture.Repository.MakeATaggedCommit(taggedVersion);

            fixture.AssertFullSemver(config, "0.1.0+6");   //Fallback version + 6 commits since tag
        }
    }
}