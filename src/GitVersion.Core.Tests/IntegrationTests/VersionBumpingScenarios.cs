using System.Collections.Generic;
using GitTools.Testing;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Model.Configuration;
using NUnit.Framework;

namespace GitVersion.Core.Tests.IntegrationTests
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
                        MainBranch, new BranchConfig
                        {
                            Tag = "pre",
                            SourceBranches = new HashSet<string>()
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
            var configuration = new Config
            {
                VersioningMode = GitVersion.VersionCalculation.VersioningMode.Mainline
            };
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit();
            fixture.MakeATaggedCommit("1.0.0");
            fixture.Repository.MakeACommit("+semver:minor");

            fixture.AssertFullSemver("1.1.0+1");

            fixture.Repository.MakeACommit("+semver:major");

            fixture.AssertFullSemver("2.0.0+2");

            fixture.Repository.MakeACommit("+semver:patch");

            fixture.AssertFullSemver("2.0.1", configuration);

            fixture.Repository.MakeACommit("+semver:minor");

            fixture.AssertFullSemver("2.1.0", configuration);
        }

        [Test]
        public void CanUseConventionalCommitsToBumpVersion()
        {
            var configuration = new Config
            {
                VersioningMode = GitVersion.VersionCalculation.VersioningMode.Mainline,

                // For future debugging of this regex: https://regex101.com/r/UfzIwS/1
                MajorVersionBumpMessage = "(build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test)(\\([\\w\\s]*\\))?(!:|:.*\\n\\n.*\\n\\n.*BREAKING.*).*",

                // For future debugging of this regex: https://regex101.com/r/9ccNam/1
                MinorVersionBumpMessage = "(feat)(\\([\\w\\s]*\\))?:",

                // For future debugging of this regex: https://regex101.com/r/ALKccf/1
                PatchVersionBumpMessage = "(build|chore|ci|docs|fix|perf|refactor|revert|style|test)(\\([\\w\\s]*\\))?:(.*\\n\\n.*\\n\\n.*BREAKING.*){0}"
            };
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit();
            fixture.MakeATaggedCommit("1.0.0");

            fixture.Repository.MakeACommit("feat(Api): Added some new endpoints");
            fixture.AssertFullSemver("1.1.0", configuration);

            // This tests if adding an exclamation mark after the type (breaking change) bumps the major version
            fixture.Repository.MakeACommit("feat(Api)!: Changed existing API models");
            fixture.AssertFullSemver("2.0.0", configuration);

            // This tests if writing BREAKING CHANGE in the footer bumps the major version
            fixture.Repository.MakeACommit("feat: Changed existing API models\n\nSome more descriptive text\n\nBREAKING CHANGE");
            fixture.AssertFullSemver("3.0.0", configuration);

            fixture.Repository.MakeACommit("chore: Cleaned up various things");
            fixture.AssertFullSemver("3.0.1", configuration);

            fixture.Repository.MakeACommit("chore: Cleaned up more various things");
            fixture.AssertFullSemver("3.0.2", configuration);

            fixture.Repository.MakeACommit("feat: Added some new functionality");
            fixture.AssertFullSemver("3.1.0", configuration);

            fixture.Repository.MakeACommit("feat: Added even more new functionality");
            fixture.AssertFullSemver("3.2.0", configuration);
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
