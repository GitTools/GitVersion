using System.Runtime.CompilerServices;
using GitVersion.Configuration;
using GitVersion.Configuration.Tests.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion.Core.Tests;

[TestFixture]
public class ConfigurationProviderTests : TestBase
{
    private string repoPath;
    private ConfigurationProvider configurationProvider;
    private IFileSystem fileSystem;

    [SetUp]
    public void Setup()
    {
        this.repoPath = PathHelper.Combine(PathHelper.GetTempPath(), "MyGitRepo");
        var options = Options.Create(new GitVersionOptions { WorkingDirectory = repoPath });
        var sp = ConfigureServices(services => services.AddSingleton(options));
        this.configurationProvider = (ConfigurationProvider)sp.GetRequiredService<IConfigurationProvider>();
        this.fileSystem = sp.GetRequiredService<IFileSystem>();

        ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
    }

    [Test]
    public void OverwritesDefaultsWithProvidedConfig()
    {
        var defaultConfiguration = this.configurationProvider.ProvideForDirectory(this.repoPath);
        const string text = @"
next-version: 2.0.0
branches:
    develop:
        increment: Major
        mode: ContinuousDelivery
        label: dev";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.NextVersion.ShouldBe("2.0.0");
        configuration.Branches.ShouldNotBeNull();

        var developConfiguration = configuration.Branches["develop"];
        developConfiguration.Increment.ShouldBe(IncrementStrategy.Major);
        developConfiguration.Increment.ShouldNotBe(defaultConfiguration.Branches["develop"].Increment);
        developConfiguration.DeploymentMode.ShouldBe(DeploymentMode.ContinuousDelivery);
        developConfiguration.DeploymentMode.ShouldNotBe(defaultConfiguration.Branches["feature"].DeploymentMode);
        developConfiguration.Label.ShouldBe("dev");
    }

    [Test]
    public void CombineVersionStrategyConfigNextAndTaggedCommit()
    {
        // Arrange
        const string text = "strategies: [ConfiguredNextVersion, TaggedCommit]";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);

        // Act
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        // Assert
        configuration.VersionStrategy.ShouldBe(VersionStrategies.ConfiguredNextVersion | VersionStrategies.TaggedCommit);
    }

    [Test]
    public void CanRemoveLabel()
    {
        const string text = @"
next-version: 2.0.0
branches:
    release:
        label: """"";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.NextVersion.ShouldBe("2.0.0");
        configuration.Branches["release"].Label.ShouldBe(string.Empty);
    }

    [Test]
    public void RegexIsRequired()
    {
        const string text = @"
next-version: 2.0.0
branches:
    bug:
        label: bugfix";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);
        var ex = Should.Throw<ConfigurationException>(() => this.configurationProvider.ProvideForDirectory(this.repoPath));
        ex.Message.ShouldBe($"Branch configuration 'bug' is missing required configuration 'regex'{PathHelper.NewLine}" +
                            "See https://gitversion.net/docs/reference/configuration for more info");
    }

    [Test(Description = "This test proves the configuration validation will fail early with a helpful message when a branch listed in source-branches has no configuration.")]
    public void SourceBranchesValidationShouldFailWhenMatchingBranchConfigurationIsMissing()
    {
        const string text = @"
branches:
    bug:
        regex: 'bug[/-]'
        label: bugfix
        source-branches: [notconfigured]";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);
        var ex = Should.Throw<ConfigurationException>(() => this.configurationProvider.ProvideForDirectory(this.repoPath));
        ex.Message.ShouldBe($"Branch configuration 'bug' defines these 'source-branches' that are not configured: '[notconfigured]'{PathHelper.NewLine}" +
                            "See https://gitversion.net/docs/reference/configuration for more info");
    }

    [Test(Description = "Well-known branches may not be present in the configuration file. This test confirms the validation check succeeds when the source-branches configuration contain these well-known branches.")]
    [TestCase(ConfigurationConstants.MainBranchKey)]
    [TestCase(ConfigurationConstants.DevelopBranchKey)]
    public void SourceBranchesValidationShouldSucceedForWellKnownBranches(string wellKnownBranchKey)
    {
        var text = $@"
branches:
    bug:
        regex: 'bug[/-]'
        label: bugfix
        source-branches: [{wellKnownBranchKey}]";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.Branches["bug"].SourceBranches.ShouldBe([wellKnownBranchKey]);
    }

    [Test]
    public void CanProvideConfigForNewBranch()
    {
        const string text = @"
next-version: 2.0.0
branches:
    bug:
        regex: 'bug[/-]'
        label: bugfix
        source-branches: []";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.Branches["bug"].RegularExpression.ShouldBe("bug[/-]");
        configuration.Branches["bug"].Label.ShouldBe("bugfix");
    }

    [Test]
    public void NextVersionCanBeInteger()
    {
        const string text = "next-version: 2";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.NextVersion.ShouldBe("2.0");
    }

    [Test]
    public void NextVersionCanHaveEnormousMinorVersion()
    {
        const string text = "next-version: 2.118998723";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.NextVersion.ShouldBe("2.118998723");
    }

    [Test]
    public void NextVersionCanHavePatch()
    {
        const string text = "next-version: 2.12.654651698";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.NextVersion.ShouldBe("2.12.654651698");
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void CanWriteOutEffectiveConfiguration()
    {
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        new ConfigurationSerializer().Serialize(configuration).ShouldMatchApproved();
    }

    [Test]
    public void CanUpdateAssemblyInformationalVersioningScheme()
    {
        const string text = @"
assembly-versioning-scheme: MajorMinor
assembly-file-versioning-scheme: MajorMinorPatch
assembly-informational-format: '{NugetVersion}'";

        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);

        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);
        configuration.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinor);
        configuration.AssemblyFileVersioningScheme.ShouldBe(AssemblyFileVersioningScheme.MajorMinorPatch);
        configuration.AssemblyInformationalFormat.ShouldBe("{NugetVersion}");
    }

    [Test]
    public void CanUpdateAssemblyInformationalVersioningSchemeWithMultipleVariables()
    {
        const string text = @"
assembly-versioning-scheme: MajorMinor
assembly-file-versioning-scheme: MajorMinorPatch
assembly-informational-format: '{Major}.{Minor}.{Patch}'";

        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);

        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);
        configuration.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinor);
        configuration.AssemblyFileVersioningScheme.ShouldBe(AssemblyFileVersioningScheme.MajorMinorPatch);
        configuration.AssemblyInformationalFormat.ShouldBe("{Major}.{Minor}.{Patch}");
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

        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);

        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);
        configuration.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinorPatch);
        configuration.AssemblyFileVersioningScheme.ShouldBe(AssemblyFileVersioningScheme.MajorMinorPatch);
        configuration.AssemblyInformationalFormat.ShouldBe("{FullSemVer}");
    }

    [Test]
    public void CanReadDefaultDocument()
    {
        const string text = "";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);
        configuration.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinorPatch);
        configuration.AssemblyFileVersioningScheme.ShouldBe(AssemblyFileVersioningScheme.MajorMinorPatch);
        configuration.AssemblyInformationalFormat.ShouldBe(null);
        configuration.Branches["develop"].Label.ShouldBe("alpha");
        configuration.Branches["release"].Label.ShouldBe("beta");
        configuration.TagPrefix.ShouldBe(RegexPatterns.Configuration.DefaultTagPrefixPattern);
        configuration.NextVersion.ShouldBe(null);
    }

    [Test]
    public void VerifyAliases()
    {
        var configuration = typeof(GitVersionConfiguration);
        var propertiesMissingAlias = configuration.GetProperties()
            .Where(p => p.GetCustomAttribute<ObsoleteAttribute>() == null
                && p.GetCustomAttribute<JsonIgnoreAttribute>() == null
                && p.GetCustomAttribute<JsonPropertyNameAttribute>() == null)
            .Select(p => p.Name);

        propertiesMissingAlias.ShouldBeEmpty();
    }

    [Test]
    public void NoWarnOnGitVersionYmlFile()
    {
        const string text = "";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);

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
        this.configurationProvider = (ConfigurationProvider)sp.GetRequiredService<IConfigurationProvider>();

        this.configurationProvider.ProvideForDirectory(this.repoPath);

        stringLogger.Length.ShouldBe(0);
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
        label: dev";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.Branches["develop"].SourceBranches.ShouldBe(["develop"]);
    }

    [Test]
    public void ShouldUseDefaultSourceBranchesWhenNotSpecifiedForDevelop()
    {
        const string text = @"
next-version: 2.0.0
branches:
    develop:
        mode: ContinuousDeployment
        label: dev";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.Branches["develop"].SourceBranches.ShouldBe(["main"]);
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
        label: dev";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.Branches["feature"].SourceBranches.ShouldBe(["develop", "release"]);
    }

    [Test]
    public void ShouldUseDefaultSourceBranchesWhenNotSpecifiedForFeature()
    {
        const string text = @"
next-version: 2.0.0
branches:
    feature:
        mode: ContinuousDeployment
        label: dev";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.Branches["feature"].SourceBranches.ShouldBe(
            ["develop", MainBranch, "release", "support", "hotfix"]);
    }

    [Test]
    public void ShouldNotOverrideAnythingWhenOverrideConfigIsEmpty()
    {
        const string text = @"
next-version: 1.2.3
tag-prefix: custom-tag-prefix-from-yml";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);

        var expectedConfig = GitFlowConfigurationBuilder.New
            .WithNextVersion("1.2.3")
            .WithTagPrefix("custom-tag-prefix-from-yml")
            .Build();
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.AssemblyVersioningScheme.ShouldBe(expectedConfig.AssemblyVersioningScheme);
        configuration.AssemblyFileVersioningScheme.ShouldBe(expectedConfig.AssemblyFileVersioningScheme);
        configuration.AssemblyInformationalFormat.ShouldBe(expectedConfig.AssemblyInformationalFormat);
        configuration.AssemblyVersioningFormat.ShouldBe(expectedConfig.AssemblyVersioningFormat);
        configuration.AssemblyFileVersioningFormat.ShouldBe(expectedConfig.AssemblyFileVersioningFormat);
        configuration.TagPrefix.ShouldBe(expectedConfig.TagPrefix);
        configuration.NextVersion.ShouldBe(expectedConfig.NextVersion);
        configuration.MajorVersionBumpMessage.ShouldBe(expectedConfig.MajorVersionBumpMessage);
        configuration.MinorVersionBumpMessage.ShouldBe(expectedConfig.MinorVersionBumpMessage);
        configuration.PatchVersionBumpMessage.ShouldBe(expectedConfig.PatchVersionBumpMessage);
        configuration.NoBumpMessage.ShouldBe(expectedConfig.NoBumpMessage);
        configuration.TagPreReleaseWeight.ShouldBe(expectedConfig.TagPreReleaseWeight);
        configuration.CommitDateFormat.ShouldBe(expectedConfig.CommitDateFormat);
        configuration.MergeMessageFormats.ShouldBe(expectedConfig.MergeMessageFormats);
        configuration.UpdateBuildNumber.ShouldBe(expectedConfig.UpdateBuildNumber);

        configuration.Ignore.ShouldBeEquivalentTo(expectedConfig.Ignore);

        configuration.Branches.Keys.ShouldBe(expectedConfig.Branches.Keys);

        foreach (var branch in configuration.Branches.Keys)
        {
            configuration.Branches[branch].ShouldBeEquivalentTo(expectedConfig.Branches[branch]);
        }
    }

    [Test]
    public void ShouldUseDefaultTagPrefixWhenNotSetInConfigFile()
    {
        const string text = "";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.TagPrefix.ShouldBe(RegexPatterns.Configuration.DefaultTagPrefixPattern);
    }

    [Test]
    public void ShouldUseTagPrefixFromConfigFileWhenProvided()
    {
        const string text = "tag-prefix: custom-tag-prefix-from-yml";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.TagPrefix.ShouldBe("custom-tag-prefix-from-yml");
    }

    [Test]
    public void ShouldOverrideTagPrefixWithOverrideConfigValue([Values] bool tagPrefixSetAtYmlFile)
    {
        var text = tagPrefixSetAtYmlFile ? "tag-prefix: custom-tag-prefix-from-yml" : "";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);
        var overrideConfiguration = new Dictionary<object, object?>
        {
            { "tag-prefix", "tag-prefix-from-override-configuration" }
        };
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath, overrideConfiguration);

        configuration.TagPrefix.ShouldBe("tag-prefix-from-override-configuration");
    }

    [Test]
    public void ShouldNotOverrideDefaultTagPrefixWhenNotSetInOverrideConfig()
    {
        const string text = "";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);
        var overrideConfiguration = new Dictionary<object, object?>
        {
            { "next-version", "1.0.0" }
        };

        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath, overrideConfiguration);

        configuration.TagPrefix.ShouldBe(RegexPatterns.Configuration.DefaultTagPrefixPattern);
    }

    [Test]
    public void ShouldNotOverrideTagPrefixFromConfigFileWhenNotSetInOverrideConfig()
    {
        const string text = "tag-prefix: custom-tag-prefix-from-yml";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);
        var overrideConfiguration = new Dictionary<object, object?>
        {
            { "next-version", "1.0.0" }
        };
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath, overrideConfiguration);

        configuration.TagPrefix.ShouldBe("custom-tag-prefix-from-yml");
    }

    [Test]
    public void ShouldOverrideTagPrefixFromConfigFileWhenSetInOverrideConfig()
    {
        const string text = "tag-prefix: custom-tag-prefix-from-yml";
        using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, text: text);
        var overrideConfiguration = new Dictionary<object, object?>
        {
            { "tag-prefix", "custom-tag-prefix-from-console" }
        };
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath, overrideConfiguration);

        configuration.TagPrefix.ShouldBe("custom-tag-prefix-from-console");
    }
}
