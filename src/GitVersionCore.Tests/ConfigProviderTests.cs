using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Shouldly;
using YamlDotNet.Serialization;
using GitVersion.Configuration;
using GitVersion.VersioningModes;
using GitVersion.Extensions;
using GitVersion;
using GitVersion.Configuration.Init.Wizard;
using GitVersion.Logging;
using Microsoft.Extensions.Options;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class ConfigProviderTests : TestBase
    {
        private const string DefaultRepoPath = @"c:\MyGitRepo";

        private string repoPath;
        private IFileSystem fileSystem;
        private IConfigFileLocator configFileLocator;
        private IConfigProvider configProvider;
        private IConfigInitWizard configInitWizard;
        private IEnvironment environment;

        [SetUp]
        public void Setup()
        {
            fileSystem = new TestFileSystem();
            var log = new NullLog();
            environment = new TestEnvironment();

            var stepFactory = new ConfigInitStepFactory();
            configInitWizard = new ConfigInitWizard(new ConsoleAdapter(), stepFactory);
            configFileLocator = new DefaultConfigFileLocator(fileSystem, log);
            repoPath = DefaultRepoPath;

            var gitPreparer = new GitPreparer(log, environment, Options.Create(new Arguments { TargetPath = repoPath }));
            configProvider = new ConfigProvider(fileSystem, log, configFileLocator, gitPreparer, configInitWizard);

            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
        }

        [Test]
        public void CanReadOldDocument()
        {
            const string text = @"
assemblyVersioningScheme: MajorMinor
develop-branch-tag: alpha
release-branch-tag: rc
branches:
    master:
        mode: ContinuousDeployment
    dev(elop)?(ment)?$:
        mode: ContinuousDeployment
        tag: dev
    release[/-]:
       mode: continuousDeployment
       tag: rc
";
            SetupConfigFileContent(text);
            var error = Should.Throw<OldConfigurationException>(() => configProvider.Provide(repoPath));
            error.Message.ShouldContainWithoutWhitespace(@"GitVersion configuration file contains old configuration, please fix the following errors:
GitVersion branch configs no longer are keyed by regexes, update:
    dev(elop)?(ment)?$  -> develop
    release[/-]         -> release
assemblyVersioningScheme has been replaced by assembly-versioning-scheme
develop-branch-tag has been replaced by branch specific configuration.See http://gitversion.readthedocs.org/en/latest/configuration/#branch-configuration
release-branch-tag has been replaced by branch specific configuration.See http://gitversion.readthedocs.org/en/latest/configuration/#branch-configuration");
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
            var ex = Should.Throw<GitVersionConfigurationException>(() => configProvider.Provide(repoPath));
            ex.Message.ShouldBe("Branch configuration 'bug' is missing required configuration 'regex'\n\n" +
                                "See http://gitversion.readthedocs.io/en/latest/configuration/ for more info");
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
            var ex = Should.Throw<GitVersionConfigurationException>(() => configProvider.Provide(repoPath));
            ex.Message.ShouldBe("Branch configuration 'bug' is missing required configuration 'source-branches'\n\n" +
                                "See http://gitversion.readthedocs.io/en/latest/configuration/ for more info");
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
        [Category("NoMono")]
        [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
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

            var defaultConfigFileLocator = new DefaultConfigFileLocator(fileSystem, log);
            var gitPreparer = new GitPreparer(log, environment, Options.Create(new Arguments { TargetPath = repoPath }));

            configProvider = new ConfigProvider(fileSystem, log, defaultConfigFileLocator, gitPreparer, configInitWizard);

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
    }
}
