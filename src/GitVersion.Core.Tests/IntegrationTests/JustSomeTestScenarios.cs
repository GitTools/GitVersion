using GitTools.Testing;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using NUnit.Framework;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class JustSomeTestScenarios : TestBase
{
    [Test]
    public void __Just_A_Test_1__()
    {
        var configuration = new Config()
        {
            VersioningMode = VersioningMode.ContinuousDeployment
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.BranchTo("release/1.0.0");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.1", configuration);
    }

    [Test]
    public void __Just_A_Test_2__()
    {
        var configuration = new Config()
        {
            VersioningMode = VersioningMode.ContinuousDeployment
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.BranchTo("release/1.0.0");
        fixture.AssertFullSemver("1.0.0-beta.0", configuration);
        fixture.BranchTo("feature/just-a-test");
        fixture.AssertFullSemver("1.0.0-just-a-test.0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-just-a-test.1", configuration);
        fixture.Checkout("release/1.0.0");
        fixture.AssertFullSemver("1.0.0-beta.0", configuration);
        fixture.MergeNoFF("feature/just-a-test");
        fixture.AssertFullSemver("1.0.0-beta.2", configuration);
        fixture.Repository.Branches.Remove("feature/just-a-test");
        fixture.AssertFullSemver("1.0.0-beta.2", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.3", configuration);
    }

    [Test]
    public void __Just_A_Test_3__()
    {
        var configuration = new Config()
        {
            VersioningMode = VersioningMode.ContinuousDeployment
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.AssertFullSemver("0.0.0-ci.0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("0.0.1-ci.1", configuration);
    }

    [Test]
    public void __Just_A_Test_4__()
    {
        var configuration = new Config()
        {
            NextVersion = "1.0.0",
            VersioningMode = VersioningMode.ContinuousDeployment
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.AssertFullSemver("1.0.0-ci.0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-ci.1", configuration);
    }

    [Test]
    public void __Just_A_Test_5__()
    {
        var configuration = new Config()
        {
            VersioningMode = VersioningMode.ContinuousDeployment,
            Branches = new Dictionary<string, BranchConfig>()
            {
                {
                    "develop", new BranchConfig() {
                        TrackMergeTarget = true
                    }
                }
            }
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.AssertFullSemver("0.0.0-ci.0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("0.0.1-ci.1", configuration);
        fixture.BranchTo("develop");
        fixture.AssertFullSemver("0.1.0-alpha.1", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("0.1.0-alpha.2", configuration);
        fixture.BranchTo("release/1.0.0");
        fixture.AssertFullSemver("1.0.0-beta.0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.1", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.2", configuration);
        fixture.Checkout("develop");
        fixture.AssertFullSemver("1.1.0-alpha.0", configuration); // 1.1.0-alpha.0 expected
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);
        fixture.MergeNoFF("release/1.0.0");
        fixture.AssertFullSemver("1.1.0-alpha.4", configuration);
        fixture.Repository.Branches.Remove("release/1.0.0");
        fixture.AssertFullSemver("1.1.0-alpha.4", configuration); // okay because TrackMergeTarget=true ??
        configuration.Branches["develop"].TrackMergeTarget = false;
        fixture.AssertFullSemver("0.1.0-alpha.6", configuration); // 0.1.0 expected because TrackMergeTarget=false ??
    }

    [Test]
    public void __Just_A_Test_6__()
    {
        var configuration = new Config()
        {
            VersioningMode = VersioningMode.ContinuousDeployment,
            Branches = new Dictionary<string, BranchConfig>()
            {
                {
                    "develop", new BranchConfig() {
                        TrackMergeTarget = true
                    }
                }
            }
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.AssertFullSemver("0.0.1-ci.1", configuration);
        fixture.BranchTo("develop");
        fixture.AssertFullSemver("0.1.0-alpha.1", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("0.1.0-alpha.2", configuration);
        fixture.BranchTo("release/1.0.0");
        fixture.AssertFullSemver("1.0.0-beta.0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.1", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.2", configuration);
        fixture.Checkout("develop");
        fixture.AssertFullSemver("1.1.0-alpha.0", configuration); // 1.1.0-alpha.0 expected

        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);

        fixture.Checkout("main");
        fixture.MergeNoFF("release/1.0.0");
        fixture.AssertFullSemver("0.0.1-ci.5", configuration);
        fixture.ApplyTag("1.0.0");
        fixture.AssertFullSemver("1.0.0", configuration);

        fixture.Checkout("develop");
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);
        fixture.MergeNoFF("main");
        fixture.AssertFullSemver("1.1.0-alpha.6", configuration);

        fixture.Repository.Branches.Remove("release/1.0.0");
        fixture.AssertFullSemver("1.1.0-alpha.6", configuration); // okay because TrackMergeTarget=true ??
        configuration.Branches["develop"].TrackMergeTarget = false;
        fixture.AssertFullSemver("1.1.0-alpha.2", configuration); // okay because TrackMergeTarget=false ??
    }

    [Test]
    public void __Just_A_Test_7__()
    {
        var configuration = new Config()
        {
            VersioningMode = VersioningMode.ContinuousDeployment,
            Branches = new Dictionary<string, BranchConfig>()
            {
                {
                    "develop", new BranchConfig() {
                        TrackMergeTarget = true
                    }
                }
            }
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.AssertFullSemver("0.0.1-ci.1", configuration);
        fixture.BranchTo("develop");
        fixture.AssertFullSemver("0.1.0-alpha.1", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("0.1.0-alpha.2", configuration);
        fixture.BranchTo("release/1.0.0");
        fixture.AssertFullSemver("1.0.0-beta.0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.1", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.2", configuration);
        fixture.Checkout("develop");
        fixture.AssertFullSemver("1.1.0-alpha.0", configuration); // 1.1.0-alpha.0 expected

        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);

        fixture.Checkout("main");
        fixture.MergeNoFF("release/1.0.0");
        fixture.AssertFullSemver("0.0.1-ci.5", configuration);
        fixture.ApplyTag("1.0.0");
        fixture.Repository.Branches.Remove("release/1.0.0");

        fixture.AssertFullSemver("1.0.0", configuration);

        fixture.Checkout("develop");
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);
        fixture.MergeNoFF("main");
        fixture.AssertFullSemver("1.1.0-alpha.6", configuration);

        fixture.AssertFullSemver("1.1.0-alpha.6", configuration); // okay because TrackMergeTarget=true ??
        configuration.Branches["develop"].TrackMergeTarget = false;
        fixture.AssertFullSemver("1.1.0-alpha.2", configuration); // okay because TrackMergeTarget=false ??
    }
}
