using System;
using System.Collections.Generic;
using GitVersion;
using GitVersion.Logging;
using GitVersion.VersionCalculation;
using GitVersionCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class VariableProviderTests : TestBase
    {
        private IVariableProvider variableProvider;
        private List<string> logMessages;

        [SetUp]
        public void Setup()
        {
            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();

            logMessages = new List<string>();

            var sp = ConfigureServices(services =>
            {
                var log = new Log(new TestLogAppender(logMessages.Add));
                services.AddSingleton<ILog>(log);
            });

            variableProvider = sp.GetService<IVariableProvider>();
        }

        [Test]
        public void ShouldLogWarningWhenUsingDefaultInformationalVersionInCustomFormat()
        {
            var semVer = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 3,
            };

#pragma warning disable CS0618 // Type or member is obsolete
            var propertyName = nameof(SemanticVersionFormatValues.DefaultInformationalVersion);
#pragma warning restore CS0618 // Type or member is obsolete
            var config = new TestEffectiveConfiguration(assemblyInformationalFormat: $"{{{propertyName}}}");
            variableProvider.GetVariablesFor(semVer, config, false);
            logMessages.ShouldContain(message => message.Trim().StartsWith("WARN") && message.Contains(propertyName), 1, $"Expected a warning to be logged when using the variable {propertyName} in a configuration format template");
        }

        [Test]
        [Category(NoMono)]
        [Description(NoMonoDescription)]
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

            vars.ToString().ShouldMatchApproved(c => c.SubFolder("Approved"));
        }

        [Test]
        [Category(NoMono)]
        [Description(NoMonoDescription)]
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

            vars.ToString().ShouldMatchApproved(c => c.SubFolder("Approved"));
        }

        [Test]
        [Category(NoMono)]
        [Description(NoMonoDescription)]
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

            vars.ToString().ShouldMatchApproved(c => c.SubFolder("Approved"));
        }

        [Test]
        [Category(NoMono)]
        [Description(NoMonoDescription)]
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

            vars.ToString().ShouldMatchApproved(c => c.SubFolder("Approved"));
        }

        [Test]
        [Category(NoMono)]
        [Description(NoMonoDescription)]
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

            vars.ToString().ShouldMatchApproved(c => c.SubFolder("Approved"));
        }

        [Test]
        [Category(NoMono)]
        [Description(NoMonoDescription)]
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

            vars.ToString().ShouldMatchApproved(c => c.SubFolder("Approved"));
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
        [Category(NoMono)]
        [Description(NoMonoDescription)]
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

            vars.ToString().ShouldMatchApproved(c => c.SubFolder("Approved"));
        }

        [Test]
        [Category(NoMono)]
        [Description(NoMonoDescription)]
        public void ProvidesVariablesInContinuousDeliveryModeForFeatureBranchWithCustomAssemblyInfoFormat()
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

            vars.ToString().ShouldMatchApproved(c => c.SubFolder("Approved"));
        }
    }
}
