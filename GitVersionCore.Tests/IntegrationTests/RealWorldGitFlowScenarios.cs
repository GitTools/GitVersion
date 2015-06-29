using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class RealWorldGitFlowScenarios
{
    [Test]
    public void SupportForPrereleasesAndResetUnstableCounter()
    {
        // Note: this test checks whether the functionality explained at https://github.com/GitTools/GitVersion/issues/452 works correctly.

        // This test should check the following contraints:
        // 1) Can we have unstable versions
        // 2) When we create a release branch, will that reset develop

        var config = new Config
        {
            VersioningMode = VersioningMode.ContinuousDeployment
        };

        using (var fixture = new EmptyRepositoryFixture(config))
        {
            fixture.Repository.MakeATaggedCommit("2.1.0");
            fixture.Repository.CreateBranch("develop").Checkout();
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("2.2.0-unstable.1");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("2.2.0-unstable.2");

            fixture.Repository.CreateBranch("release/2.2.0").Checkout();
            fixture.Repository.ApplyTag("2.2.0");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("2.2.0-beta.1");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("2.2.0-beta.2");

            fixture.Repository.Checkout("develop");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("2.3.0-unstable.1");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("2.3.0-unstable.2");

            fixture.Repository.Checkout("master");
            fixture.AssertFullSemver("2.1.0");
            fixture.Repository.MergeNoFF("release/2.2.0");
            fixture.Repository.Tags.Remove("2.2.0");
            fixture.Repository.ApplyTag("2.2.0");
            fixture.AssertFullSemver("2.2.0");

            fixture.Repository.Checkout("develop");
            fixture.Repository.MergeNoFF("release/2.2.0");
            fixture.AssertFullSemver("2.3.0-unstable.3");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("2.3.0-unstable.4");
            fixture.Repository.MakeACommit();

            fixture.Repository.DumpGraph();
        }
    }
}