using System;
using GitVersion;
using GitVersionCore.Tests;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class VariableProviderTests
{
    [Test]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ProvidesVariablesInContinuousDeliveryModeForPreRelease()
    {
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = "unstable.4",
            BuildMetaData = "5.Branch.develop"
        };

        semVer.BuildMetaData.Sha = "commitSha";
        semVer.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");


        var config = new TestEffectiveConfiguration();

        var vars = VariableProvider.GetVariablesFor(semVer, config, false);

        JsonOutputFormatter.ToJson(vars).ShouldMatchApproved(c => c.SubFolder("Approved"));
    }

    [Test]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ProvidesVariablesInContinuousDeliveryModeForPreReleaseWithPadding()
    {
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = "unstable.4",
            BuildMetaData = "5.Branch.develop"
        };

        semVer.BuildMetaData.Sha = "commitSha";
        semVer.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");


        var config = new TestEffectiveConfiguration(buildMetaDataPadding: 2, legacySemVerPadding: 5);

        var vars = VariableProvider.GetVariablesFor(semVer, config, false);

        JsonOutputFormatter.ToJson(vars).ShouldMatchApproved(c => c.SubFolder("Approved"));
    }

    [Test]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ProvidesVariablesInContinuousDeploymentModeForPreRelease()
    {
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = "unstable.4",
            BuildMetaData = "5.Branch.develop"
        };

        semVer.BuildMetaData.Sha = "commitSha";
        semVer.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");

        var config = new TestEffectiveConfiguration(versioningMode: VersioningMode.ContinuousDeployment);

        var vars = VariableProvider.GetVariablesFor(semVer, config, false);

        JsonOutputFormatter.ToJson(vars).ShouldMatchApproved(c => c.SubFolder("Approved"));
    }

    [Test]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ProvidesVariablesInContinuousDeliveryModeForStable()
    {
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            BuildMetaData = "5.Branch.develop"
        };

        semVer.BuildMetaData.Sha = "commitSha";
        semVer.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");

        var config = new TestEffectiveConfiguration();

        var vars = VariableProvider.GetVariablesFor(semVer, config, false);

        JsonOutputFormatter.ToJson(vars).ShouldMatchApproved(c => c.SubFolder("Approved"));
    }

    [Test]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ProvidesVariablesInContinuousDeploymentModeForStable()
    {
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            BuildMetaData = "5.Branch.develop"
        };

        semVer.BuildMetaData.Sha = "commitSha";
        semVer.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");

        var config = new TestEffectiveConfiguration(versioningMode: VersioningMode.ContinuousDeployment);

        var vars = VariableProvider.GetVariablesFor(semVer, config, false);

        JsonOutputFormatter.ToJson(vars).ShouldMatchApproved(c => c.SubFolder("Approved"));
    }

    [Test]
    [Category("NoMono")]
    [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    public void ProvidesVariablesInContinuousDeploymentModeForStableWhenCurrentCommitIsTagged()
    {
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            BuildMetaData =
            {
                CommitsSinceTag = 5,
                CommitsSinceVersionSource = 5,
                Sha = "commitSha",
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            }
        };

        var config = new TestEffectiveConfiguration(versioningMode: VersioningMode.ContinuousDeployment);

        var vars = VariableProvider.GetVariablesFor(semVer, config, true);

        JsonOutputFormatter.ToJson(vars).ShouldMatchApproved(c => c.SubFolder("Approved"));
    }

    [Test]
    public void ProvidesVariablesInContinuousDeploymentModeWithTagNamePattern()
    {
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = "PullRequest",
            BuildMetaData = "5.Branch.develop"
        };

        semVer.BuildMetaData.Branch = "pull/2/merge";
        semVer.BuildMetaData.Sha = "commitSha";
        semVer.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");

        var config = new TestEffectiveConfiguration(versioningMode: VersioningMode.ContinuousDeployment, tagNumberPattern: @"[/-](?<number>\d+)[-/]");
        var vars = VariableProvider.GetVariablesFor(semVer, config, false);

        vars.FullSemVer.ShouldBe("1.2.3-PullRequest0002.5");
    }

    [Test]
    public void ProvidesVariablesInContinuousDeploymentModeWithTagSetToUseBranchName()
    {
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            BuildMetaData = "5.Branch.develop"
        };

        semVer.BuildMetaData.Branch = "feature";
        semVer.BuildMetaData.Sha = "commitSha";
        semVer.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");

        var config = new TestEffectiveConfiguration(versioningMode: VersioningMode.ContinuousDeployment, tag: "useBranchName");
        var vars = VariableProvider.GetVariablesFor(semVer, config, false);

        vars.FullSemVer.ShouldBe("1.2.3-feature.5");
    }
}