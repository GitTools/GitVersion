using System.Runtime.CompilerServices;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion.Core.Tests;

[TestFixture]
public class ConfigurationProviderTests : TestBase
{
    private const string DefaultRepoPath = @"c:\MyGitRepo";

    private string repoPath;
    private ConfigurationProvider configurationProvider;
    private IFileSystem fileSystem;

    [SetUp]
    public void Setup()
    {
        this.repoPath = DefaultRepoPath;
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
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.NextVersion.ShouldBe("2.0.0");
        configuration.Branches.ShouldNotBeNull();

        var developConfiguration = configuration.Branches["develop"];
        developConfiguration.Increment.ShouldBe(IncrementStrategy.Major);
        developConfiguration.Increment.ShouldNotBe(defaultConfiguration.Branches["develop"].Increment);
        developConfiguration.VersioningMode.ShouldBe(VersioningMode.ContinuousDelivery);
        developConfiguration.VersioningMode.ShouldNotBe(defaultConfiguration.Branches["develop"].VersioningMode);
        developConfiguration.Label.ShouldBe("dev");
    }

    [Test]
    public void CanRemoveLabel()
    {
        const string text = @"
next-version: 2.0.0
branches:
    release:
        label: """"";
        SetupConfigFileContent(text);
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
        SetupConfigFileContent(text);
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
        SetupConfigFileContent(text);
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
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.Branches["bug"].SourceBranches.ShouldBe(new List<string> { wellKnownBranchKey });
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
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.Branches["bug"].RegularExpression.ShouldBe("bug[/-]");
        configuration.Branches["bug"].Label.ShouldBe("bugfix");
    }

    [Test]
    public void NextVersionCanBeInteger()
    {
        const string text = "next-version: 2";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.NextVersion.ShouldBe("2.0");
    }

    [Test]
    public void NextVersionCanHaveEnormousMinorVersion()
    {
        const string text = "next-version: 2.118998723";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.NextVersion.ShouldBe("2.118998723");
    }

    [Test]
    public void NextVersionCanHavePatch()
    {
        const string text = "next-version: 2.12.654651698";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.NextVersion.ShouldBe("2.12.654651698");
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void CanWriteOutEffectiveConfiguration()
    {
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.ToJsonString().ShouldMatchApproved();
    }

    [Test]
    public void CanUpdateAssemblyInformationalVersioningScheme()
    {
        const string text = @"
assembly-versioning-scheme: MajorMinor
assembly-file-versioning-scheme: MajorMinorPatch
assembly-informational-format: '{NugetVersion}'";

        SetupConfigFileContent(text);

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

        SetupConfigFileContent(text);

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

        SetupConfigFileContent(text);

        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);
        configuration.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinorPatch);
        configuration.AssemblyFileVersioningScheme.ShouldBe(AssemblyFileVersioningScheme.MajorMinorPatch);
        configuration.AssemblyInformationalFormat.ShouldBe("{FullSemVer}");
    }

    [Test]
    public void CanReadDefaultDocument()
    {
        const string text = "";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);
        configuration.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinorPatch);
        configuration.AssemblyFileVersioningScheme.ShouldBe(AssemblyFileVersioningScheme.MajorMinorPatch);
        configuration.AssemblyInformationalFormat.ShouldBe(null);
        configuration.Branches["develop"].Label.ShouldBe("alpha");
        configuration.Branches["release"].Label.ShouldBe("beta");
        configuration.TagPrefix.ShouldBe(ConfigurationConstants.DefaultTagPrefix);
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
        this.configurationProvider = (ConfigurationProvider)sp.GetRequiredService<IConfigurationProvider>();

        this.configurationProvider.ProvideForDirectory(this.repoPath);

        stringLogger.Length.ShouldBe(0);
    }

    private void SetupConfigFileContent(string text, string fileName = ConfigurationFileLocator.DefaultFileName)
        => SetupConfigFileContent(text, fileName, this.repoPath);

    private void SetupConfigFileContent(string text, string fileName, string path)
    {
        var fullPath = PathHelper.Combine(path, fileName);
        this.fileSystem.WriteAllText(fullPath, text);
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
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.Branches["develop"].SourceBranches.ShouldBe(new List<string> { "develop" });
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
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.Branches["develop"].SourceBranches.ShouldBe(new List<string>());
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
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.Branches["feature"].SourceBranches.ShouldBe(new List<string> { "develop", "release" });
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
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.Branches["feature"].SourceBranches.ShouldBe(
            new List<string> { "develop", MainBranch, "release", "feature", "support", "hotfix" });
    }

    [Test]
    public void ShouldNotOverrideAnythingWhenOverrideConfigIsEmpty()
    {
        const string text = @"
next-version: 1.2.3
tag-prefix: custom-tag-prefix-from-yml";
        SetupConfigFileContent(text);

        var expectedConfig = GitFlowConfigurationBuilder.New
            .WithNextVersion("1.2.3")
            .WithTagPrefix("custom-tag-prefix-from-yml")
            .Build();
        var overridenConfig = this.configurationProvider.ProvideForDirectory(this.repoPath);

        overridenConfig.AssemblyVersioningScheme.ShouldBe(expectedConfig.AssemblyVersioningScheme);
        overridenConfig.AssemblyFileVersioningScheme.ShouldBe(expectedConfig.AssemblyFileVersioningScheme);
        overridenConfig.AssemblyInformationalFormat.ShouldBe(expectedConfig.AssemblyInformationalFormat);
        overridenConfig.AssemblyVersioningFormat.ShouldBe(expectedConfig.AssemblyVersioningFormat);
        overridenConfig.AssemblyFileVersioningFormat.ShouldBe(expectedConfig.AssemblyFileVersioningFormat);
        overridenConfig.TagPrefix.ShouldBe(expectedConfig.TagPrefix);
        overridenConfig.NextVersion.ShouldBe(expectedConfig.NextVersion);
        overridenConfig.MajorVersionBumpMessage.ShouldBe(expectedConfig.MajorVersionBumpMessage);
        overridenConfig.MinorVersionBumpMessage.ShouldBe(expectedConfig.MinorVersionBumpMessage);
        overridenConfig.PatchVersionBumpMessage.ShouldBe(expectedConfig.PatchVersionBumpMessage);
        overridenConfig.NoBumpMessage.ShouldBe(expectedConfig.NoBumpMessage);
        overridenConfig.TagPreReleaseWeight.ShouldBe(expectedConfig.TagPreReleaseWeight);
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
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.TagPrefix.ShouldBe(ConfigurationConstants.DefaultTagPrefix);
    }

    [Test]
    public void ShouldUseTagPrefixFromConfigFileWhenProvided()
    {
        const string text = "tag-prefix: custom-tag-prefix-from-yml";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath);

        configuration.TagPrefix.ShouldBe("custom-tag-prefix-from-yml");
    }

    [Test]
    public void ShouldOverrideTagPrefixWithOverrideConfigValue([Values] bool tagPrefixSetAtYmlFile)
    {
        var text = tagPrefixSetAtYmlFile ? "tag-prefix: custom-tag-prefix-from-yml" : "";
        SetupConfigFileContent(text);
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
        SetupConfigFileContent(text);
        var overrideConfiguration = new Dictionary<object, object?>
        {
            { "next-version", "1.0.0" }
        };

        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath, overrideConfiguration);

        configuration.TagPrefix.ShouldBe(ConfigurationConstants.DefaultTagPrefix);
    }

    [Test]
    public void ShouldNotOverrideTagPrefixFromConfigFileWhenNotSetInOverrideConfig()
    {
        const string text = "tag-prefix: custom-tag-prefix-from-yml";
        SetupConfigFileContent(text);
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
        SetupConfigFileContent(text);
        var overrideConfiguration = new Dictionary<object, object?>
        {
            { "tag-prefix", "custom-tag-prefix-from-console" }
        };
        var configuration = this.configurationProvider.ProvideForDirectory(this.repoPath, overrideConfiguration);

        configuration.TagPrefix.ShouldBe("custom-tag-prefix-from-console");
    }
}
