using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using GitVersion;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using GitVersionCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using YamlDotNet.Serialization;
using Environment = System.Environment;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class ConfigProviderTests : TestBase
    {
        private const string DefaultRepoPath = @"c:\MyGitRepo";

        private string repoPath;
        private IConfigProvider configProvider;
        private IFileSystem fileSystem;

        [SetUp]
        public void Setup()
        {
            repoPath = DefaultRepoPath;
            var options = Options.Create(new GitVersionOptions { WorkingDirectory = repoPath });
            var sp = ConfigureServices(services =>
            {
                services.AddSingleton(options);
            });
            configProvider = sp.GetService<IConfigProvider>();
            fileSystem = sp.GetService<IFileSystem>();

            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
        }

        [Test]
        public void OverwritesDefaultsWithProvidedConfig()
        {
            var defaultConfig = configProvider.Provide(repoPath);
            const string text = @"
next-version: 2.0.0
branches:
    develop:
        mode: ContinuousDeployment
        tag: dev";
            SetupConfigFileContent(text);
            var config = configProvider.Provide(repoPath);

            config.NextVersion.ShouldBe("2.0.0");
            config.Branches["develop"].Increment.ShouldBe(defaultConfig.Branches["develop"].Increment);
            config.Branches["develop"].VersioningMode.ShouldBe(defaultConfig.Branches["develop"].VersioningMode);
            config.Branches["develop"].Tag.ShouldBe("dev");
        }

        [Test]
        public void AllBranchesModeWhenUsingMainline()
        {
            const string text = @"mode: Mainline";
            SetupConfigFileContent(text);
            var config = configProvider.Provide(repoPath);
            var branches = config.Branches.Select(x => x.Value);
            branches.All(branch => branch.VersioningMode == VersioningMode.Mainline).ShouldBe(true);
        }

        [Test]
        public void CanRemoveTag()
        {
            const string text = @"
next-version: 2.0.0
branches:
    release:
        tag: """"";
            SetupConfigFileContent(text);
            var config = configProvider.Provide(repoPath);

            config.NextVersion.ShouldBe("2.0.0");
            config.Branches["release"].Tag.ShouldBe(string.Empty);
        }

        [Test]
        public void RegexIsRequired()
        {
            const string text = @"
next-version: 2.0.0
branches:
    bug:
        tag: bugfix";
            SetupConfigFileContent(text);
            var ex = Should.Throw<ConfigurationException>(() => configProvider.Provide(repoPath));
            ex.Message.ShouldBe($"Branch configuration 'bug' is missing required configuration 'regex'{Environment.NewLine}" +
                                "See https://gitversion.net/docs/configuration/ for more info");
        }

        [Test]
        public void SourceBranchIsRequired()
        {
            const string text = @"
next-version: 2.0.0
branches:
    bug:
        regex: 'bug[/-]'
        tag: bugfix";
            SetupConfigFileContent(text);
            var ex = Should.Throw<ConfigurationException>(() => configProvider.Provide(repoPath));
            ex.Message.ShouldBe($"Branch configuration 'bug' is missing required configuration 'source-branches'{Environment.NewLine}" +
                                "See https://gitversion.net/docs/configuration/ for more info");
        }

        [Test(Description = "This test proves the configuration validation will fail early with a helpful message when a branch listed in source-branches has no configuration.")]
        public void SourceBranchesValidationShouldFailWhenMatchingBranchConfigurationIsMissing()
        {
            const string text = @"
branches:
    bug:
        regex: 'bug[/-]'
        tag: bugfix
        source-branches: [notconfigured]";
            SetupConfigFileContent(text);
            var ex = Should.Throw<ConfigurationException>(() => configProvider.Provide(repoPath));
            ex.Message.ShouldBe($"Branch configuration 'bug' defines these 'source-branches' that are not configured: '[notconfigured]'{Environment.NewLine}" +
                                "See https://gitversion.net/docs/configuration/ for more info");
        }

        [Test(Description = "Well-known branches may not be present in the configuration file. This test confirms the validation check succeeds when the source-branches configuration contain these well-known branches.")]
        [TestCase(Config.MasterBranchKey)]
        [TestCase(Config.DevelopBranchKey)]
        public void SourceBranchesValidationShouldSucceedForWellKnownBranches(string wellKnownBranchKey)
        {
            var text = $@"
branches:
    bug:
        regex: 'bug[/-]'
        tag: bugfix
        source-branches: [{wellKnownBranchKey}]";
            SetupConfigFileContent(text);
            var config = configProvider.Provide(repoPath);

            config.Branches["bug"].SourceBranches.ShouldBe(new List<string> { wellKnownBranchKey });
        }

        [Test]
        public void CanProvideConfigForNewBranch()
        {
            const string text = @"
next-version: 2.0.0
branches:
    bug:
        regex: 'bug[/-]'
        tag: bugfix
        source-branches: []";
            SetupConfigFileContent(text);
            var config = configProvider.Provide(repoPath);

            config.Branches["bug"].Regex.ShouldBe("bug[/-]");
            config.Branches["bug"].Tag.ShouldBe("bugfix");
        }

        [Test]
        public void NextVersionCanBeInteger()
        {
            const string text = "next-version: 2";
            SetupConfigFileContent(text);
            var config = configProvider.Provide(repoPath);

            config.NextVersion.ShouldBe("2.0");
        }

        [Test]
        public void NextVersionCanHaveEnormousMinorVersion()
        {
            const string text = "next-version: 2.118998723";
            SetupConfigFileContent(text);
            var config = configProvider.Provide(repoPath);

            config.NextVersion.ShouldBe("2.118998723");
        }

        [Test]
        public void NextVersionCanHavePatch()
        {
            const string text = "next-version: 2.12.654651698";
            SetupConfigFileContent(text);
            var config = configProvider.Provide(repoPath);

            config.NextVersion.ShouldBe("2.12.654651698");
        }

        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Category(NoMono)]
        [Description(NoMonoDescription)]
        public void CanWriteOutEffectiveConfiguration()
        {
            var config = configProvider.Provide(repoPath);

            config.ToString().ShouldMatchApproved();
        }

        [Test]
        public void CanUpdateAssemblyInformationalVersioningScheme()
        {
            const string text = @"
assembly-versioning-scheme: MajorMinor
assembly-file-versioning-scheme: MajorMinorPatch
assembly-informational-format: '{NugetVersion}'";

            SetupConfigFileContent(text);

            var config = configProvider.Provide(repoPath);
            config.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinor);
            config.AssemblyFileVersioningScheme.ShouldBe(AssemblyFileVersioningScheme.MajorMinorPatch);
            config.AssemblyInformationalFormat.ShouldBe("{NugetVersion}");
        }

        [Test]
        public void CanUpdateAssemblyInformationalVersioningSchemeWithMultipleVariables()
        {
            const string text = @"
assembly-versioning-scheme: MajorMinor
assembly-file-versioning-scheme: MajorMinorPatch
assembly-informational-format: '{Major}.{Minor}.{Patch}'";

            SetupConfigFileContent(text);

            var config = configProvider.Provide(repoPath);
            config.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinor);
            config.AssemblyFileVersioningScheme.ShouldBe(AssemblyFileVersioningScheme.MajorMinorPatch);
            config.AssemblyInformationalFormat.ShouldBe("{Major}.{Minor}.{Patch}");
        }


        [Test]
        public void CanUpdateAssemblyInformationalVersioningSchemeWithFullSemVer()
        {
            const string text = @"assembly-versioning-scheme: MajorMinorPatch
assembly-file-versioning-scheme: MajorMinorPatch
assembly-informational-format: '{FullSemVer}'
mode: ContinuousDelivery
next-version: 5.3.0
branches: {}";

            SetupConfigFileContent(text);

            var config = configProvider.Provide(repoPath);
            config.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinorPatch);
            config.AssemblyFileVersioningScheme.ShouldBe(AssemblyFileVersioningScheme.MajorMinorPatch);
            config.AssemblyInformationalFormat.ShouldBe("{FullSemVer}");
        }

        [Test]
        public void CanReadDefaultDocument()
        {
            const string text = "";
            SetupConfigFileContent(text);
            var config = configProvider.Provide(repoPath);
            config.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinorPatch);
            config.AssemblyFileVersioningScheme.ShouldBe(AssemblyFileVersioningScheme.MajorMinorPatch);
            config.AssemblyInformationalFormat.ShouldBe(null);
            config.Branches["develop"].Tag.ShouldBe("alpha");
            config.Branches["release"].Tag.ShouldBe("beta");
            config.TagPrefix.ShouldBe(Config.DefaultTagPrefix);
            config.NextVersion.ShouldBe(null);
        }

        [Test]
        public void VerifyAliases()
        {
            var config = typeof(Config);
            var propertiesMissingAlias = config.GetProperties()
                .Where(p => p.GetCustomAttribute<ObsoleteAttribute>() == null)
                .Where(p => p.GetCustomAttribute(typeof(YamlMemberAttribute)) == null)
                .Select(p => p.Name);

            propertiesMissingAlias.ShouldBeEmpty();
        }

        [Test]
        public void NoWarnOnGitVersionYmlFile()
        {
            SetupConfigFileContent(string.Empty);

            var stringLogger = string.Empty;
            void Action(string info) => stringLogger = info;

            var logAppender = new TestLogAppender(Action);
            var log = new Log(logAppender);

            var options = Options.Create(new GitVersionOptions { WorkingDirectory = repoPath });
            var sp = ConfigureServices(services =>
            {
                services.AddSingleton(options);
                services.AddSingleton<ILog>(log);
            });
            configProvider = sp.GetService<IConfigProvider>();

            configProvider.Provide(repoPath);

            stringLogger.Length.ShouldBe(0);
        }

        private string SetupConfigFileContent(string text, string fileName = DefaultConfigFileLocator.DefaultFileName)
        {
            return SetupConfigFileContent(text, fileName, repoPath);
        }

        private string SetupConfigFileContent(string text, string fileName, string path)
        {
            var fullPath = Path.Combine(path, fileName);
            fileSystem.WriteAllText(fullPath, text);

            return fullPath;
        }

        [Test]
        public void ShouldUseSpecifiedSourceBranchesForDevelop()
        {
            const string text = @"
next-version: 2.0.0
branches:
    develop:
        mode: ContinuousDeployment
        source-branches: ['develop']
        tag: dev";
            SetupConfigFileContent(text);
            var config = configProvider.Provide(repoPath);

            config.Branches["develop"].SourceBranches.ShouldBe(new List<string> { "develop" });
        }

        [Test]
        public void ShouldUseDefaultSourceBranchesWhenNotSpecifiedForDevelop()
        {
            const string text = @"
next-version: 2.0.0
branches:
    develop:
        mode: ContinuousDeployment
        tag: dev";
            SetupConfigFileContent(text);
            var config = configProvider.Provide(repoPath);

            config.Branches["develop"].SourceBranches.ShouldBe(new List<string>());
        }

        [Test]
        public void ShouldUseSpecifiedSourceBranchesForFeature()
        {
            const string text = @"
next-version: 2.0.0
branches:
    feature:
        mode: ContinuousDeployment
        source-branches: ['develop', 'release']
        tag: dev";
            SetupConfigFileContent(text);
            var config = configProvider.Provide(repoPath);

            config.Branches["feature"].SourceBranches.ShouldBe(new List<string> { "develop", "release" });
        }

        [Test]
        public void ShouldUseDefaultSourceBranchesWhenNotSpecifiedForFeature()
        {
            const string text = @"
next-version: 2.0.0
branches:
    feature:
        mode: ContinuousDeployment
        tag: dev";
            SetupConfigFileContent(text);
            var config = configProvider.Provide(repoPath);

            config.Branches["feature"].SourceBranches.ShouldBe(
                new List<string> { "develop", "master", "release", "feature", "support", "hotfix" });
        }

        [Test]
        public void ShouldNotOverrideAnythingWhenOverrideConfigIsEmpty()
        {
            const string text = @"
next-version: 1.2.3
tag-prefix: custom-tag-prefix-from-yml";
            SetupConfigFileContent(text);

            var expectedConfig = configProvider.Provide(repoPath, overrideConfig: null);
            var overridenConfig = configProvider.Provide(repoPath, overrideConfig: new Config());

            overridenConfig.AssemblyVersioningScheme.ShouldBe(expectedConfig.AssemblyVersioningScheme);
            overridenConfig.AssemblyFileVersioningScheme.ShouldBe(expectedConfig.AssemblyFileVersioningScheme);
            overridenConfig.AssemblyInformationalFormat.ShouldBe(expectedConfig.AssemblyInformationalFormat);
            overridenConfig.AssemblyVersioningFormat.ShouldBe(expectedConfig.AssemblyVersioningFormat);
            overridenConfig.AssemblyFileVersioningFormat.ShouldBe(expectedConfig.AssemblyFileVersioningFormat);
            overridenConfig.VersioningMode.ShouldBe(expectedConfig.VersioningMode);
            overridenConfig.TagPrefix.ShouldBe(expectedConfig.TagPrefix);
            overridenConfig.ContinuousDeploymentFallbackTag.ShouldBe(expectedConfig.ContinuousDeploymentFallbackTag);
            overridenConfig.NextVersion.ShouldBe(expectedConfig.NextVersion);
            overridenConfig.MajorVersionBumpMessage.ShouldBe(expectedConfig.MajorVersionBumpMessage);
            overridenConfig.MinorVersionBumpMessage.ShouldBe(expectedConfig.MinorVersionBumpMessage);
            overridenConfig.PatchVersionBumpMessage.ShouldBe(expectedConfig.PatchVersionBumpMessage);
            overridenConfig.NoBumpMessage.ShouldBe(expectedConfig.NoBumpMessage);
            overridenConfig.LegacySemVerPadding.ShouldBe(expectedConfig.LegacySemVerPadding);
            overridenConfig.BuildMetaDataPadding.ShouldBe(expectedConfig.BuildMetaDataPadding);
            overridenConfig.CommitsSinceVersionSourcePadding.ShouldBe(expectedConfig.CommitsSinceVersionSourcePadding);
            overridenConfig.TagPreReleaseWeight.ShouldBe(expectedConfig.TagPreReleaseWeight);
            overridenConfig.CommitMessageIncrementing.ShouldBe(expectedConfig.CommitMessageIncrementing);
            overridenConfig.Increment.ShouldBe(expectedConfig.Increment);
            overridenConfig.CommitDateFormat.ShouldBe(expectedConfig.CommitDateFormat);
            overridenConfig.MergeMessageFormats.ShouldBe(expectedConfig.MergeMessageFormats);
            overridenConfig.UpdateBuildNumber.ShouldBe(expectedConfig.UpdateBuildNumber);

            overridenConfig.Ignore.ShouldBeEquivalentTo(expectedConfig.Ignore);

            overridenConfig.Branches.Keys.ShouldBe(expectedConfig.Branches.Keys);

            foreach (var branch in overridenConfig.Branches.Keys)
            {
                overridenConfig.Branches[branch].ShouldBeEquivalentTo(expectedConfig.Branches[branch]);
            }
        }

        [Test]
        public void ShouldUseDefaultTagPrefixWhenNotSetInConfigFile()
        {
            const string text = "";
            SetupConfigFileContent(text);
            var config = configProvider.Provide(repoPath);

            config.TagPrefix.ShouldBe("[vV]");
        }

        [Test]
        public void ShouldUseTagPrefixFromConfigFileWhenProvided()
        {
            const string text = "tag-prefix: custom-tag-prefix-from-yml";
            SetupConfigFileContent(text);
            var config = configProvider.Provide(repoPath);

            config.TagPrefix.ShouldBe("custom-tag-prefix-from-yml");
        }

        [Test]
        public void ShouldOverrideTagPrefixWithOverrideConfigValue([Values] bool tagPrefixSetAtYmlFile)
        {
            var text = tagPrefixSetAtYmlFile ? "tag-prefix: custom-tag-prefix-from-yml" : "";
            SetupConfigFileContent(text);
            var config = configProvider.Provide(repoPath, overrideConfig: new Config { TagPrefix = "tag-prefix-from-override-config" });

            config.TagPrefix.ShouldBe("tag-prefix-from-override-config");
        }

        [Test]
        public void ShouldNotOverrideDefaultTagPrefixWhenNotSetInOverrideConfig()
        {
            const string text = "";
            SetupConfigFileContent(text);
            var config = configProvider.Provide(repoPath, overrideConfig: new Config { TagPrefix = null });

            config.TagPrefix.ShouldBe("[vV]");
        }

        [Test]
        public void ShouldNotOverrideTagPrefixFromConfigFileWhenNotSetInOverrideConfig()
        {
            const string text = "tag-prefix: custom-tag-prefix-from-yml";
            SetupConfigFileContent(text);
            var config = configProvider.Provide(repoPath, overrideConfig: new Config { TagPrefix = null });

            config.TagPrefix.ShouldBe("custom-tag-prefix-from-yml");
        }
    }
}
