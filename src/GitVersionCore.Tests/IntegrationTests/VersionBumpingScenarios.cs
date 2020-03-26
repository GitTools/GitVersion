using System.Collections.Generic;
using GitTools.Testing;
using GitVersion.Model.Configuration;
using GitVersionCore.Tests.Helpers;
using NUnit.Framework;

namespace GitVersionCore.Tests.IntegrationTests
{
    [TestFixture]
    public class VersionBumpingScenarios : TestBase
    {
        [Test]
        public void AppliedPrereleaseTagCausesBump()
        {
            var configuration = new Config
            {
                Branches =
                {
                    {
                        "master", new BranchConfig
                        {
                            Tag = "pre",
                            SourceBranches = new List<string>()
                        }
                    }
                }
            };
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeATaggedCommit("1.0.0-pre.1");
            fixture.Repository.MakeACommit();

            fixture.AssertFullSemver("1.0.0-pre.2+1", configuration);
        }

        [Test]
        public void CanUseCommitMessagesToBumpVersion()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit();
            fixture.MakeATaggedCommit("1.0.0");
            fixture.Repository.MakeACommit("+semver:minor");

            fixture.AssertFullSemver("1.1.0+1");

            fixture.Repository.MakeACommit("+semver:major");

            fixture.AssertFullSemver("2.0.0+2");
        }

        [Test]
        public void CanUseCommitMessagesToBumpVersionBaseVersionTagIsAppliedToSameCommit()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit();
            fixture.MakeATaggedCommit("1.0.0");
            fixture.Repository.MakeACommit("+semver:minor");
            fixture.AssertFullSemver("1.1.0+1");

            fixture.ApplyTag("2.0.0");

            fixture.Repository.MakeACommit("Hello");

            // Default bump is patch

            fixture.AssertFullSemver("2.0.1+1");
        }
    }
}
