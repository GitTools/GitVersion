using GitTools.Testing;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using LibGit2Sharp;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace GitVersion.Core.Tests.IntegrationTests
{
    public class XenoLibPackages : TestBase
    {
        private readonly Config config = new Config
        {
            VersioningMode = VersioningMode.Mainline,
            Branches = new Dictionary<string, BranchConfig>
            {
                {
                    "feature", new BranchConfig
                    {
                        Increment = IncrementStrategy.Minor
                    }
                },
                {
                    "pull-request", new BranchConfig
                    {
                        //Regex = @"^(pull|pull\-requests|pr)[/-]",
                        Increment = IncrementStrategy.Minor
                    }
                },
                {
                    "support", new BranchConfig
                    {
                        VersioningMode = VersioningMode.ContinuousDeployment,
                        Tag = "beta",
                        Increment = IncrementStrategy.Patch
                    }
                },

            }
        };

        [Test]
        public void IncrementFeatureByMinor()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeATaggedCommit("0.1.0");

            // feature workflow
            fixture.BranchTo("feature/foo", "foo");
            fixture.MakeACommit();
            fixture.AssertFullSemver("0.2.0-foo.1", config);
            fixture.MakeACommit();
            fixture.AssertFullSemver("0.2.0-foo.2", config);
            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("feature/foo");
            fixture.AssertFullSemver("0.2.0", config);
        }

        [Test]
        public void CanCalculatePullRequestChanges()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.MakeATaggedCommit("1.1.0");
            fixture.Repository.MakeATaggedCommit("2.0.0");

            // feature branch
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("feature/foo"));
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("2.1.0-foo.1", config);
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("2.1.0-foo.2", config);

            // pull request
            fixture.Repository.CreatePullRequestRef("feature/foo", MainBranch, normalise: true);
            fixture.AssertFullSemver("2.1.0-PullRequest0002.3", config);
            Commands.Checkout(fixture.Repository, MainBranch);
            fixture.Repository.MergeNoFF("feature/foo", Generate.SignatureNow());
            fixture.AssertFullSemver("2.1.0", config);

            // hotfix branch
            var tag = fixture.Repository.Tags.Single(t => t.FriendlyName == "1.0.0");
            var supportBranch = fixture.Repository.CreateBranch("support/1.0.0", (Commit)tag.Target);
            Commands.Checkout(fixture.Repository, supportBranch);
            fixture.AssertFullSemver("1.0.0", config);
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.0.1-beta.1", config);
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.0.1-beta.2", config);
            fixture.Repository.MakeATaggedCommit("1.0.1");

            // pull request
            fixture.Repository.CreatePullRequestRef("support/1.0.0", MainBranch, 3, normalise: true);
            fixture.Repository.DumpGraph();
            fixture.AssertFullSemver("2.1.1-PullRequest0003.6", config);
            Commands.Checkout(fixture.Repository, MainBranch);
            fixture.Repository.MergeNoFF("support/1.0.0", Generate.SignatureNow());
            fixture.AssertFullSemver("2.1.1", config);

        }
    }
}
