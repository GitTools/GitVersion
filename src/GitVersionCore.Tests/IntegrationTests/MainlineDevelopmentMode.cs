using System;
using System.Diagnostics;
using GitTools;
using GitTools.Testing;
using GitVersion;
using GitVersionCore.Tests;
using NUnit.Framework;
using Shouldly;

public class MainlineDevelopmentMode
{
    private Config config = new Config
    {
        Branches =
        {
            {
                "master", new BranchConfig
                {
                    VersioningMode = VersioningMode.Mainline
                }
            }
        }
    };


    [Test]
    public void CannotSetMainlineDevelopmentAtAGlobalLevel()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            Should.Throw<NotSupportedException>(() =>
                fixture.AssertFullSemver(new Config
                {
                    VersioningMode = VersioningMode.Mainline
                }, ""));
        }
    }

    [Test]
    public void MergedFeatureBranchesToMasterImpliesRelease()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeACommit("1");
            fixture.MakeACommit();

            fixture.BranchTo("feature/foo");
            fixture.Repository.MakeACommit("2");
            fixture.Checkout("master");
            fixture.MergeNoFF("feature/foo");

            fixture.AssertFullSemver(config, "0.1.1+3");

            fixture.BranchTo("feature/foo2");
            fixture.Repository.MakeACommit("3 +semver: minor");
            fixture.Checkout("master");
            fixture.MergeNoFF("feature/foo2");
            fixture.AssertFullSemver(config, "0.2.0+5");

            fixture.BranchTo("feature/foo3");
            fixture.Repository.MakeACommit("4 +semver: minor");
            fixture.Checkout("master");
            fixture.MergeNoFF("feature/foo3");
            fixture.AssertFullSemver(config, "0.3.0+7");

            fixture.BranchTo("feature/foo4");
            fixture.Repository.MakeACommit("5 +semver: major");
            fixture.Checkout("master");
            fixture.MergeNoFF("feature/foo4");
            fixture.AssertFullSemver(config, "1.0.0+9");
        }
    }

    // Write test which has a forward merge into a feature branch
}
