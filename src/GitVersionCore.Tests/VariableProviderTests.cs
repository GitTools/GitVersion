using System;
using GitVersion.Logging;
using NUnit.Framework;
using Shouldly;
using GitVersion.OutputFormatters;
using GitVersion.OutputVariables;
using GitVersion;
using GitVersion.VersionCalculation;
using GitVersion.VersioningModes;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class VariableProviderTests : TestBase
    {
        private IVariableProvider variableProvider;

        [SetUp]
        public void Setup()
        {
            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
            var log = new NullLog();
            var metaDataCalculator = new MetaDataCalculator();
            var baseVersionCalculator = new BaseVersionCalculator(log, null);
            var mainlineVersionCalculator = new MainlineVersionCalculator(log, metaDataCalculator);
            var nextVersionCalculator = new NextVersionCalculator(log, metaDataCalculator, baseVersionCalculator, mainlineVersionCalculator);
            variableProvider = new VariableProvider(nextVersionCalculator, new TestEnvironment());
        }

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

            semVer.BuildMetaData.VersionSourceSha = "versionSourceSha";
            semVer.BuildMetaData.Sha = "commitSha";
            semVer.BuildMetaData.ShortSha = "commitShortSha";
            semVer.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");


            var config = new TestEffectiveConfiguration();

            var vars = variableProvider.GetVariablesFor(semVer, config, false);

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

            semVer.BuildMetaData.VersionSourceSha = "versionSourceSha";
            semVer.BuildMetaData.Sha = "commitSha";
            semVer.BuildMetaData.ShortSha = "commitShortSha";
            semVer.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");


            var config = new TestEffectiveConfiguration(buildMetaDataPadding: 2, legacySemVerPadding: 5);

            var vars = variableProvider.GetVariablesFor(semVer, config, false);

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

            semVer.BuildMetaData.VersionSourceSha = "versionSourceSha";
            semVer.BuildMetaData.Sha = "commitSha";
            semVer.BuildMetaData.ShortSha = "commitShortSha";
            semVer.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");

            var config = new TestEffectiveConfiguration(versioningMode: VersioningMode.ContinuousDeployment);

            var vars = variableProvider.GetVariablesFor(semVer, config, false);

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

            semVer.BuildMetaData.VersionSourceSha = "versionSourceSha";
            semVer.BuildMetaData.Sha = "commitSha";
            semVer.BuildMetaData.ShortSha = "commitShortSha";
            semVer.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");

            var config = new TestEffectiveConfiguration();

            var vars = variableProvider.GetVariablesFor(semVer, config, false);

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

            semVer.BuildMetaData.VersionSourceSha = "versionSourceSha";
            semVer.BuildMetaData.Sha = "commitSha";
            semVer.BuildMetaData.ShortSha = "commitShortSha";
            semVer.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");

            var config = new TestEffectiveConfiguration(versioningMode: VersioningMode.ContinuousDeployment);

            var vars = variableProvider.GetVariablesFor(semVer, config, false);

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
                    VersionSourceSha = "versionSourceSha",
                    CommitsSinceTag = 5,
                    CommitsSinceVersionSource = 5,
                    Sha = "commitSha",
                    ShortSha = "commitShortSha",
                    CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
                }
            };

            var config = new TestEffectiveConfiguration(versioningMode: VersioningMode.ContinuousDeployment);

            var vars = variableProvider.GetVariablesFor(semVer, config, true);

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
            semVer.BuildMetaData.ShortSha = "commitShortSha";
            semVer.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");

            var config = new TestEffectiveConfiguration(versioningMode: VersioningMode.ContinuousDeployment, tagNumberPattern: @"[/-](?<number>\d+)[-/]");
            var vars = variableProvider.GetVariablesFor(semVer, config, false);

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
            semVer.BuildMetaData.ShortSha = "commitShortSha";
            semVer.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");

            var config = new TestEffectiveConfiguration(versioningMode: VersioningMode.ContinuousDeployment, tag: "useBranchName");
            var vars = variableProvider.GetVariablesFor(semVer, config, false);

            vars.FullSemVer.ShouldBe("1.2.3-feature.5");
        }

        [Test]
        [Category("NoMono")]
        [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
        public void ProvidesVariablesInContinuousDeliveryModeForFeatureBranch()
        {
            var semVer = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 3,
                BuildMetaData = "5.Branch.feature/123"
            };

            semVer.BuildMetaData.Branch = "feature/123";
            semVer.BuildMetaData.VersionSourceSha = "versionSourceSha";
            semVer.BuildMetaData.Sha = "commitSha";
            semVer.BuildMetaData.ShortSha = "commitShortSha";
            semVer.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");


            var config = new TestEffectiveConfiguration();

            var vars = variableProvider.GetVariablesFor(semVer, config, false);

            JsonOutputFormatter.ToJson(vars).ShouldMatchApproved(c => c.SubFolder("Approved"));
        }

        [Test]
        [Category("NoMono")]
        [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
        public void ProvidesVariablesInContinuousDeliveryModeForFeatureBranchWithCustomAssemblyInformationalFormat()
        {
            var semVer = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 3,
                BuildMetaData = "5.Branch.feature/123"
            };

            semVer.BuildMetaData.Branch = "feature/123";
            semVer.BuildMetaData.VersionSourceSha = "versionSourceSha";
            semVer.BuildMetaData.Sha = "commitSha";
            semVer.BuildMetaData.ShortSha = "commitShortSha";
            semVer.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");


            var config = new TestEffectiveConfiguration(assemblyInformationalFormat: "{Major}.{Minor}.{Patch}+{CommitsSinceVersionSource}.Branch.{BranchName}.Sha.{ShortSha}");

            var vars = variableProvider.GetVariablesFor(semVer, config, false);

            JsonOutputFormatter.ToJson(vars).ShouldMatchApproved(c => c.SubFolder("Approved"));
        }
    }
}
