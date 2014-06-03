﻿namespace AcceptanceTests.GitFlow
{
    using System.Linq;
    using System.Threading;
    using Helpers;
    using LibGit2Sharp;
    using Xunit;

    public class PatchScenarios
    {
        [Fact]
        public void PatchLatestReleaseExample()
        {
            using (var fixture = new BaseGitFlowRepositoryFixture("1.2.0"))
            {
                // create hotfix
                fixture.Repository.CreateBranch("hotfix-1.2.1").Checkout();

                fixture.AssertFullSemver("1.2.1-beta.1+0");
                fixture.AssertNugetPackageVersion("1.2.1-beta0001+0");
                fixture.Repository.MakeACommit();
                fixture.AssertFullSemver("1.2.1-beta.1+1");
                fixture.AssertNugetPackageVersion("1.2.1-beta0001+1");
                fixture.Repository.ApplyTag("1.2.1-beta.1");
                fixture.AssertFullSemver("1.2.1-beta.2+1");
                fixture.AssertNugetPackageVersion("1.2.1-beta0002+1");

                // Merge hotfix branch to master
                fixture.Repository.Checkout("master");
                
                
                fixture.Repository.MergeNoFF("hotfix-1.2.1", Constants.SignatureNow());
                fixture.AssertFullSemver("1.2.1");
                fixture.AssertNugetPackageVersion("1.2.1");

                fixture.Repository.ApplyTag("1.2.1");
                fixture.AssertFullSemver("1.2.1");
                fixture.AssertNugetPackageVersion("1.2.1");

                // Verify develop version
                fixture.Repository.Checkout("develop");
                fixture.AssertFullSemver("1.3.0.0-unstable");
                fixture.AssertNugetPackageVersion("1.3.0-unstable0000");

                fixture.Repository.Commit("Test Commit", Constants.SignatureNow(), new CommitOptions { AllowEmptyCommit = true });
                fixture.AssertFullSemver("1.3.0.1-unstable");
                fixture.AssertNugetPackageVersion("1.3.0-unstable0001");

                fixture.Repository.Commit("Test Commit", Constants.SignatureNow(), Constants.SignatureNow(), new CommitOptions { AllowEmptyCommit = true });
                fixture.AssertFullSemver("1.3.0.2-unstable");
                fixture.AssertNugetPackageVersion("1.3.0-unstable0002");

                // Warning: Hack-ish hack
                //
                // Ensure the merge commit is done at a different time than the previous one
                // Otherwise, as they would have the same content and signature, the same sha would be generated.
                // Thus 'develop' and 'master' would point at the same exact commit and the Assert below would fail.
                Thread.Sleep(1000); 
                fixture.Repository.MergeNoFF("hotfix-1.2.1", Constants.SignatureNow());

                fixture.AssertFullSemver("1.3.0.3-unstable");
                fixture.AssertNugetPackageVersion("1.3.0-unstable0003");
            }
        }

        [Fact]
        public void PatchOlderReleaseExample()
        {
            using (var fixture = new BaseGitFlowRepositoryFixture(r =>
            {
                r.MakeATaggedCommit("1.0.0");
                r.MakeATaggedCommit("1.1.0");
                r.MakeATaggedCommit("1.2.0");
            }))
            {
                
                // create hotfix branch
                fixture.Repository.CreateBranch("hotfix-1.1.1", (Commit)fixture.Repository.Tags.Single(t => t.Name == "1.1.0").Target).Checkout();

                fixture.AssertFullSemver("1.1.1-beta.1+0");
                fixture.AssertNugetPackageVersion("1.1.1-beta0001+0");
                fixture.Repository.MakeACommit();
                fixture.AssertFullSemver("1.1.1-beta.1+1");
                fixture.AssertNugetPackageVersion("1.1.1-beta0001+1");

                // Merge hotfix branch to support
                fixture.Repository.CreateBranch("support-1.2", (Commit)fixture.Repository.Tags.Single(t => t.Name == "1.1.0").Target).Checkout();
                fixture.AssertFullSemver("1.1.0");
                fixture.AssertNugetPackageVersion("1.1.0");

                fixture.Repository.MergeNoFF("hotfix-1.1.1", Constants.SignatureNow());
                fixture.AssertFullSemver("1.1.1");
                fixture.AssertNugetPackageVersion("1.1.1");

                fixture.Repository.ApplyTag("1.1.1");
                fixture.AssertFullSemver("1.1.1");
                fixture.AssertNugetPackageVersion("1.1.1");

                // Verify develop version
                fixture.Repository.Checkout("develop");
                fixture.AssertFullSemver("1.3.0.0-unstable");
                fixture.AssertNugetPackageVersion("1.3.0-unstable0000");
            }
        }
    }
}
